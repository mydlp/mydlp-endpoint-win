//    Copyright (C) 2011 Huseyin Ozgur Batur <ozgur@medra.com.tr>
//
//--------------------------------------------------------------------------
//    This file is part of MyDLP.
//
//    MyDLP is free software: you can redistribute it and/or modify
//    it under the terms of the GNU General Public License as published by
//    the Free Software Foundation, either version 3 of the License, or
//    (at your option) any later version.
//
//    MyDLP is distributed in the hope that it will be useful,
//    but WITHOUT ANY WARRANTY; without even the implied warranty of
//    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//    GNU General Public License for more details.
//
//    You should have received a copy of the GNU General Public License
//    along with MyDLP.  If not, see <http://www.gnu.org/licenses/>.
//--------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.Win32;
using System.Diagnostics;
using System.ComponentModel;
using System.Management;
using System.Timers;
using System.Runtime.InteropServices;

namespace MyDLP.EndPoint.Core
{
    public class Configuration
    {
        //app conf
        static String appPath;
        static String seapServer;
        static int seapPort;
        static String minifilterPath;
        static String pyBackendPath;
        static String erlangPath;
        static String pythonBinPaths;
        static String erlangBinPaths;
        static String pythonPath;
        static String mydlpConfPath;
        static int erlPid = 0;
        static int pythonPid = 0;
        static DateTime startTime;
        static String userName = "";
        static Timer userNameTimer;

        //user conf
        static Logger.LogLevel logLevel = Logger.LogLevel.DEBUG;
        static String managementServer;
        static long logLimit;
        static long maximumObjectSize;
        static bool archiveInbound;
        static bool usbSerialAccessControl;
        static bool printerMonitor;
        static bool newFilterConfiguration;

        public static void setNewFilterConfiguration(bool newConf)
        {
            newFilterConfiguration = newConf;
        }

        //This is a special case logger should be initialized before configuration class    
        public static String GetLogPath()
        {
            if (System.Environment.UserInteractive)
            {
                return @"C:\workspace\mydlp-development-env\logs\mydlpepwin.log";
            }
            else
            {
                try
                {
                    RegistryKey mydlpKey = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("MyDLP");

                    //Get path
                    try
                    {
                        return mydlpKey.GetValue("AppPath").ToString() + @"\logs\mydlpepwin.log";
                    }
                    catch (Exception e)
                    {
                        return @"C:\mydlpepwin.log";
                    }
                }
                catch (Exception e)
                {
                    return @"C:\mydlpepwin.log";
                }
            }
        }

        //This is a special case logger should be initialized before configuration class 
        public static void InitLogLevel()
        {
            try
            {
                RegistryKey mydlpKey = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("MyDLP", true);

                //Get loglevel
                try
                {
                    logLevel = (Logger.LogLevel)mydlpKey.GetValue("log_level");
                    if (logLevel > Logger.LogLevel.DEBUG) logLevel = Logger.LogLevel.DEBUG;
                }
                catch (Exception e)
                {
                    mydlpKey.SetValue("log_level", 1, RegistryValueKind.DWord);
                    logLevel = Logger.LogLevel.INFO;
                }

            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Unable to get registry key HKLM/Software/MyDLP "
                     + e.Message + " " + e.StackTrace);
                logLevel = Logger.LogLevel.DEBUG;
            }
        }

        public static void InitUserNameCacheTimer()
        {
            userNameTimer = new Timer(20000);
            userNameTimer.Elapsed += new ElapsedEventHandler(OnTimeUserNameCacheEvent);
            userNameTimer.Enabled = true;

        }

        private static void OnTimeUserNameCacheEvent(object source, ElapsedEventArgs e)
        {
            userName = "";
        }

        public static int GetErlPid()
        {
            //get erl.exe pid
            Process[] processes = Process.GetProcessesByName("erl");
            if (processes.Length == 0)
            {
                //ger werl.exe pid
                processes = Process.GetProcessesByName("werl");
                if (processes.Length == 0)
                {
                    //erlang service not working get current pid toprovide a valid process
                    processes = new Process[] { Process.GetCurrentProcess() };
                }
            }
            return processes[0].Id;
        }

        public static Boolean SetErlConf()
        {
            try
            {
                StreamReader reader = new StreamReader(mydlpConfPath);
                string content = reader.ReadToEnd();
                reader.Close();

                if (content.Contains("management_server_address"))
                {
                    content = Regex.Replace(content, "^[#\t\\ ]*management_server_address\\s+([0-9\\.]+)?[\t\\ ]*(\r)?$", "management_server_address\t" + managementServer + @"$2", RegexOptions.Multiline);
                }
                else
                {
                    content = content + "\nmanagement_server_address\t" + managementServer;
                }

                StreamWriter writer = new StreamWriter(mydlpConfPath);
                writer.Write(content);
                writer.Close();
                return true;
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("SetErlConf " + e.Message + "\n" + e.StackTrace);
                return false;
            }
        }

        //this will run after erl and python started
        public static void setPids()
        {
            int tryLimit = 30;
            int tryCount = 0;

            String path = appPath + @"\run\mydlp.pid";
            try
            {
                DateTime dt = File.GetLastWriteTime(path);
                while (dt <= Configuration.StartTime && tryCount < tryLimit)
                {
                    System.Threading.Thread.Sleep(3000);
                    dt = File.GetLastWriteTime(path);
                    tryCount++;
                }
                string text = System.IO.File.ReadAllText(path);
                erlPid = Int32.Parse(text.Trim());
            }
            catch
            {
                erlPid = GetErlPid();
            }

            tryCount = 0;
            path = appPath + @"\run\backend-py.pid";
            try
            {
                DateTime dt = File.GetLastWriteTime(path);
                while (dt <= Configuration.StartTime && tryCount < tryLimit)
                {
                    System.Threading.Thread.Sleep(3000);
                    dt = File.GetLastWriteTime(path);
                }
                string text = System.IO.File.ReadAllText(path);
                pythonPid = Int32.Parse(text.Trim());
            }
            catch
            {
                pythonPid = 0;
            }

            Logger.GetInstance().Info("Configuration.PythonPid = " + Configuration.PythonPid);
            Logger.GetInstance().Info("Configuration.ErlPid = " + Configuration.ErlPid);
        }

        public static bool GetAppConf()
        {
            Logger.GetInstance().Debug("GetAppConf called");

            if (System.Environment.UserInteractive)
            {
                //Use development conf
                if (IsOS64Bit())
                {
                    Logger.GetInstance().Info("64 bit platform, using MyDLPMF_64.sys");
                    minifilterPath = "C:\\workspace\\mydlp-endpoint-win\\EndPoint\\MiniFilter\\src\\objchk_win7_amd64\\amd64\\MyDLPMF.sys";
                }
                else
                {
                    Logger.GetInstance().Info("32 bit platform, using MyDLPMF.sys");
                    minifilterPath = "C:\\workspace\\mydlp-endpoint-win\\EndPoint\\MiniFilter\\src\\objchk_wxp_x86\\i386\\MyDLPMF.sys";
                }
                pyBackendPath = @"C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\backend\py\";
                erlangPath = @"C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\mydlp\";
                erlangBinPaths = @"C:\workspace\mydlp-deployment-env\erl5.8.5\bin;C:\workspace\mydlp-deployment-env\erl5.8.5\erts-5.8.5\bin";
                pythonBinPaths = @"C:\workspace\mydlp-deployment-env\Python26";
                pythonPath = @"C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\thrift\gen-py";
                appPath = @"C:\workspace\mydlp-development-env";
                seapServer = "127.0.0.1";
                seapPort = 9099;
                mydlpConfPath = Configuration.ErlangPath + "mydlp-ep.conf";

            }
            else
            {
                //Use normal conf
                try
                {
                    RegistryKey mydlpKey = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("MyDLP", true);

                    //Get path
                    try
                    {
                        appPath = mydlpKey.GetValue("AppPath").ToString();
                        if (IsOS64Bit())
                        {
                            Logger.GetInstance().Info("64 bit platform, using MyDLPMF_64.sys");
                            minifilterPath = appPath + "MyDLPMF_64.sys";
                        }
                        else
                        {
                            Logger.GetInstance().Info("32 bit platform, using MyDLPMF.sys");
                            minifilterPath = appPath + "MyDLPMF.sys";
                        }
                        pyBackendPath = appPath + "engine\\py\\";
                        erlangPath = appPath + "engine\\erl\\";
                        erlangBinPaths = appPath + @"erl5.8.5\bin;" + appPath + @"erl5.8.5\erts-5.8.5\bin";
                        pythonPath = appPath + "engine\\py\\";
                        pythonBinPaths = appPath + "Python26";
                        mydlpConfPath = Configuration.AppPath + @"\mydlp.conf";
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Error("Unable to get registry value  HKLM/Software/MyDLP:AppPath "
                            + e.Message + " " + e.StackTrace);
                        return false;
                    }

                    //Get seapServer
                    seapServer = (String)getRegistryConfSafe(mydlpKey, "seap_server", "127.0.0.1", RegistryValueKind.String);

                    //Get managementServer
                    managementServer = (String)getRegistryConfSafe(mydlpKey, "management_server", "127.0.0.1", RegistryValueKind.String);


                    //Get seapPort
                    seapPort = (int)getRegistryConfSafe(mydlpKey, "seap_port", 9099, RegistryValueKind.DWord);

                }
                catch (Exception e)
                {
                    Logger.GetInstance().Error("Unable to open registry key HKLM/Software/MyDLP "
                        + e.Message + " " + e.StackTrace);
                    return false;
                }
            }

            Logger.GetInstance().Info("MyDLP Path: " + appPath);
            Logger.GetInstance().Info("MyDLP SeapServer: " + seapServer + ":" + seapPort);
            Logger.GetInstance().Info("MyDLP AppPath: " + appPath);
            Logger.GetInstance().Info("MyDLP ConfPath: " + mydlpConfPath);

            return true;
        }

        public static bool GetUserConf()
        {
            Logger.GetInstance().Debug("GetUserConf called");

            try
            {
                RegistryKey mydlpKey = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("MyDLP", true);

                //Get archiveInbound
                if ((int)(getRegistryConfSafe(mydlpKey, "archive_inbound", 0, RegistryValueKind.DWord)) == 0)
                {
                    archiveInbound = false;
                }
                else
                {
                    archiveInbound = true;
                }

                //Get archiveInbound
                if ((int)(getRegistryConfSafe(mydlpKey, "usb_serial_access_control", 0, RegistryValueKind.DWord)) == 0)
                {
                    usbSerialAccessControl = false;
                }
                else
                {
                    usbSerialAccessControl = true;
                }

                //Get printMonitor
                if ((int)(getRegistryConfSafe(mydlpKey, "print_monitor", 0, RegistryValueKind.DWord)) == 0)
                {
                    printerMonitor = false;
                }
                else
                {
                    printerMonitor = true;
                }


                //Get managementServer
                managementServer = (String)getRegistryConfSafe(mydlpKey, "management_server", "127.0.0.1", RegistryValueKind.String);

                //Try to use old management server if local host found for management server
                if (managementServer == "127.0.0.1")
                    managementServer = (String)getRegistryConfSafe(mydlpKey, "ManagementServer", "127.0.0.1", RegistryValueKind.String);

                //Get logLimit
                logLimit = (int)getRegistryConfSafe(mydlpKey, "log_limit", 10485760, RegistryValueKind.DWord);

                //Get maximumObjectSize
                maximumObjectSize = (int)getRegistryConfSafe(mydlpKey, "maximum_object_size", 10485760, RegistryValueKind.DWord);

                //Get loglevel
                logLevel = (Logger.LogLevel)getRegistryConfSafe(mydlpKey, "log_level", 1, RegistryValueKind.DWord);
                if (logLevel > Logger.LogLevel.DEBUG) logLevel = Logger.LogLevel.DEBUG;

            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Unable to open registry key HKLM/Software/MyDLP "
                    + e.Message + " " + e.StackTrace);
                return false;
            }


            Logger.GetInstance().Info("MyDLP LogLevel: " + logLevel.ToString());
            Logger.GetInstance().Info("MyDLP ManagementServer: " + managementServer);
            Logger.GetInstance().Info("MyDLP ArchiveInbound: " + archiveInbound);
            Logger.GetInstance().Info("MyDLP USBSerialAccessControl: " + usbSerialAccessControl);
            Logger.GetInstance().Info("MyDLP PrinterMonitor: " + printerMonitor);
            Logger.GetInstance().Info("MyDLP LogLimit: " + logLimit);
            Logger.GetInstance().Info("MyDLP MaximumObjectSize: " + maximumObjectSize);

            return true;
        }

        public static string GetLoggedOnUser()
        {
            if (userName != "")
                return userName;
            else
            {

                String processName = "explorer.exe";
                string query = "Select * from Win32_Process Where Name = \"" + processName + "\"";
                ManagementObjectSearcher searcher = new ManagementObjectSearcher(query);
                ManagementObjectCollection processList = searcher.Get();

                foreach (ManagementObject obj in processList)
                {
                    string[] argList = new string[] { string.Empty, string.Empty };
                    int returnVal = Convert.ToInt32(obj.InvokeMethod("GetOwner", argList));
                    if (returnVal == 0)
                    {
                        // return DOMAIN\user
                        string owner = argList[0] + "@" + argList[1];
                        userName = owner;
                        return userName;
                    }
                }

                userName = "NO OWNER";
            }
            return userName;
        }

        public static object getRegistryConfSafe(RegistryKey key, String valueName, Object defaultValue, RegistryValueKind kind)
        {
            object retVal;
            try
            {
                retVal = key.GetValue(valueName);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Unable to get registry value: " + key.ToString() + " " + valueName + " creating with default value:" + defaultValue);
                try
                {
                    key.SetValue(valueName, defaultValue, kind);
                }
                catch
                {
                    Logger.GetInstance().Error("Unable to create registry value: " + key.ToString() + " " + valueName + " with default value:" + defaultValue);
                }
                retVal = defaultValue;
            }

            if (retVal == null)
            {
                Logger.GetInstance().Error("Null registry value: " + key.ToString() + " " + valueName + " creating with default value:" + defaultValue);
                retVal = defaultValue;
                try
                {
                    key.SetValue(valueName, defaultValue, kind);
                }
                catch
                {
                    Logger.GetInstance().Error("Unable to create null registry value: " + key.ToString() + " " + valueName + " with default value:" + defaultValue);
                }
            }
            return retVal;
        }

        [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public extern static IntPtr LoadLibrary(string libraryName);

        [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public extern static IntPtr GetProcAddress(IntPtr hwnd, string procedureName);

        private delegate bool IsWow64ProcessDelegate([In] IntPtr handle, [Out] out bool isWow64Process);

        private static IsWow64ProcessDelegate GetIsWow64ProcessDelegate()
        {
            IntPtr handle = LoadLibrary("kernel32");

            if (handle != IntPtr.Zero)
            {
                IntPtr fnPtr = GetProcAddress(handle, "IsWow64Process");

                if (fnPtr != IntPtr.Zero)
                {
                    return (IsWow64ProcessDelegate)Marshal.GetDelegateForFunctionPointer((IntPtr)fnPtr, typeof(IsWow64ProcessDelegate));
                }
            }

            return null;
        }

        public static bool IsOS64Bit()
        {
            if (IntPtr.Size == 8 || (IntPtr.Size == 4 && Is32BitProcessOn64BitProcessor()))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool Is32BitProcessOn64BitProcessor()
        {
            IsWow64ProcessDelegate fnDelegate = GetIsWow64ProcessDelegate();

            if (fnDelegate == null)
            {
                return false;
            }

            bool isWow64;
            bool retVal = fnDelegate.Invoke(Process.GetCurrentProcess().Handle, out isWow64);

            if (retVal == false)
            {
                return false;
            }

            return isWow64;
        }

        public static DateTime StartTime
        {
            get
            {
                return startTime;
            }

            set
            {
                startTime = value;
            }
        }

        //Property get
        public static String AppPath
        {
            get
            {
                return appPath;
            }
        }

        public static String SeapServer
        {
            get
            {
                return seapServer;
            }
        }

        public static String ManagementServer
        {
            get
            {
                return managementServer;
            }
        }

        public static bool ArchiveInbound
        {
            get
            {
                return archiveInbound;
            }
        }

        public static bool UsbSerialAccessControl
        {
            get
            {
                return usbSerialAccessControl;
            }
        }

        public static bool PrinterMonitor
        {
            get
            {
                return printerMonitor;
            }
        }

        public static int SeapPort
        {
            get
            {
                return seapPort;
            }
        }

        public static Logger.LogLevel LogLevel
        {
            get
            {
                return logLevel;
            }
        }

        public static String MinifilterPath
        {
            get
            {
                return minifilterPath;
            }
        }

        public static String PyBackendPath
        {
            get
            {
                return pyBackendPath;
            }
        }

        public static String ErlangPath
        {
            get
            {
                return erlangPath;
            }
        }

        public static String ErlangBinPaths
        {
            get
            {
                return erlangBinPaths;
            }
        }

        public static String PythonBinPaths
        {
            get
            {
                return pythonBinPaths;
            }
        }

        public static String PythonPath
        {
            get
            {
                return pythonPath;
            }
        }

        public static String MydlpConfPath
        {
            get
            {
                return mydlpConfPath;
            }
        }

        public static int ErlPid
        {
            get
            {
                return erlPid;
            }
        }

        public static int PythonPid
        {
            get
            {
                return pythonPid;
            }
        }

        public static long LogLimit
        {
            get
            {
                return logLimit;
            }
        }

        public static long MaximumObjectSize
        {
            get
            {
                return maximumObjectSize;
            }
        }

        public static bool NewFilterConfiguration
        {
            get
            {
                return newFilterConfiguration;
            }
        }
    }
}

