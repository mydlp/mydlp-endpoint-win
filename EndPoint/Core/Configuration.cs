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

        public enum OsVersion { XP, Win7_32, Win7_64, Unknown };
        //Resolving cicular dependency with Platform.dll
        public delegate string GetLoggedOnUserDeleagate();
        public static GetLoggedOnUserDeleagate GetLoggedOnUser;

        //app conf
        static String appPath;
        static String seapServer;
        static int seapPort;
        static String minifilterPath;
        static String kbfilterPath;
        static String javaBackendPath;
        static String erlangPath;
        static String javaBinPaths;
        static String erlangHome;
        static String javaPath;
        static String mydlpConfPath;
        static int erlPid = 0;
        static int javaPid = 0;
        static DateTime startTime;
        static String userName = "";
        //static Timer userNameTimer;
        static String printingDirPath;
        static String printSpoolPath;
        static String version = "";
        static String printerPrefix;

        //user conf
        static Logger.LogLevel logLevel = Logger.LogLevel.DEBUG;
        static String managementServer;
        static long logLimit;
        static long maximumObjectSize;
        static bool archiveInbound;
        static bool usbSerialAccessControl;
        static bool printerMonitor;
        static bool newFilterConfiguration;
        static string screentShotProcesses = "";
        static bool blockScreenShot;
        static bool remStorEncryption;
        static bool hasEncryptionKey = false;

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
                if (Environment.UserInteractive)
                {
                    logLevel = Logger.LogLevel.DEBUG;
                }
                else
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
                        mydlpKey.SetValue("log_level", Logger.LogLevel.DEBUG, RegistryValueKind.DWord);
                        logLevel = Logger.LogLevel.INFO;
                    }
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Unable to get registry key HKLM/Software/MyDLP "
                     + e);
                logLevel = Logger.LogLevel.DEBUG;
            }
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
                Logger.GetInstance().Error("SetErlConf " + e);
                return false;
            }
        }

        //this will run after erl and java started
        public static void SetPids()
        {
            int tryLimit = 30;
            int tryCount = 0;
            String path = appPath + @"\run\mydlp.pid";
            try
            {
                //DateTime dt = File.GetLastWriteTime(path);
                //while (dt <= Configuration.StartTime && tryCount < tryLimit)
                while (!File.Exists(path) && tryCount < tryLimit)
                {
                    System.Threading.Thread.Sleep(3000);
                    // dt = File.GetLastWriteTime(path);                    
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
            path = appPath + @"\run\backend.pid";
            try
            {
                //DateTime dt = File.GetLastWriteTime(path);
                //while (dt <= Configuration.StartTime && tryCount < tryLimit)
                while (!File.Exists(path) && tryCount < tryLimit)
                {
                    System.Threading.Thread.Sleep(3000);
                    tryCount++;
                }
                string text = System.IO.File.ReadAllText(path);
                javaPid = Int32.Parse(text.Trim());
            }
            catch
            {
                javaPid = 0;
            }

            Logger.GetInstance().Info("Configuration.JavaPid = " + Configuration.JavaPid);
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
                    kbfilterPath = "C:\\workspace\\mydlp-endpoint-win\\EndPoint\\KbFilter\\src\\objchk_win7_amd64\\amd64\\MyDLPKBF.sys";
                }
                else
                {
                    Logger.GetInstance().Info("32 bit platform, using MyDLPMF.sys");
                    minifilterPath = "C:\\workspace\\mydlp-endpoint-win\\EndPoint\\MiniFilter\\src\\objchk_wxp_x86\\i386\\MyDLPMF.sys";
                    kbfilterPath = "C:\\workspace\\mydlp-endpoint-win\\EndPoint\\KbFilter\\src\\objchk_wxp_x86\\i386\\MyDLPKBF.sys";
                }

                printingDirPath = "C:\\workspace\\mydlp-endpoint-win\\EndPoint\\Service\\printing\\";
                javaBackendPath = @"C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\backend\";
                javaPath = @"C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\backend\target\";
                erlangPath = @"C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\mydlp\";
                erlangHome = @"C:\workspace\mydlp-development-env\erl5.8.5";
                javaBinPaths = @"C:\workspace\mydlp-development-env\jre7\bin";
                appPath = @"C:\workspace\mydlp-development-env";
                mydlpConfPath = Configuration.ErlangPath + "mydlp-ep.conf";
                printSpoolPath = @"C:\windows\temp\mydlp\spool";
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
                            Logger.GetInstance().Info("64 bit platform, using MyDLPMF_64.sys and MyDLPKBF_64.sys");
                            minifilterPath = appPath + "MyDLPMF_64.sys";
                            kbfilterPath = appPath + "MyDLPKBF_64.sys";
                        }
                        else
                        {
                            Logger.GetInstance().Info("32 bit platform, using MyDLPMF.sys and MyDLPKBF.sys");
                            minifilterPath = appPath + "MyDLPMF.sys";
                            kbfilterPath = appPath + "MyDLPKBF.sys";
                        }
                        printingDirPath = appPath + "printing\\";
                        javaBackendPath = appPath + "engine\\java\\";
                        erlangPath = appPath + "engine\\erl\\";
                        erlangHome = appPath + @"erl5.8.5";
                        javaPath = appPath + "engine\\java\\";
                        javaBinPaths = appPath + "jre7\\bin\\";
                        mydlpConfPath = Configuration.AppPath + @"\mydlp.conf";
                        printSpoolPath = Path.Combine(Path.GetTempPath(), "mydlp\\spool");
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Error("Unable to get registry value  HKLM/Software/MyDLP:AppPath "
                            + e);
                        return false;
                    }
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Error("Unable to open registry key HKLM/Software/MyDLP "
                        + e);
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
                RegistryKey mydlpKey = Registry.LocalMachine.OpenSubKey("Software", true).CreateSubKey("MyDLP");
                //Get screenShotConfiguration
                if ((int)(getRegistryConfSafe(mydlpKey, "prtscr_block", 0, RegistryValueKind.DWord)) == 0)
                {
                    blockScreenShot = false;
                }
                else
                {
                    blockScreenShot = true;
                    screentShotProcesses = (String)getRegistryConfSafe(mydlpKey, "prtscr_processes", 0, RegistryValueKind.String);
                }
                //Get archiveInbound
                if ((int)(getRegistryConfSafe(mydlpKey, "archive_inbound", 0, RegistryValueKind.DWord)) == 0)
                {
                    archiveInbound = false;
                }
                else
                {
                    archiveInbound = true;
                }
                //Get usbSerialAccessControl
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
                //Get printerPrefix
                printerPrefix = (String)getRegistryConfSafe(mydlpKey, "printer_prefix", "(MyDLP)", RegistryValueKind.String);

                //Get remStorEncryption
                if ((int)(getRegistryConfSafe(mydlpKey, "usbstor_encryption", 0, RegistryValueKind.DWord)) == 0)
                {
                    remStorEncryption = false;
                }
                else
                {
                    remStorEncryption = true;
                }

                //Get seapServer
                seapServer = (String)getRegistryConfSafe(mydlpKey, "seap_server", "127.0.0.1", RegistryValueKind.String);
                //Get managementServer
                managementServer = (String)getRegistryConfSafe(mydlpKey, "management_server", "127.0.0.1", RegistryValueKind.String);
                //Get seapPort
                seapPort = (int)getRegistryConfSafe(mydlpKey, "seap_port", 9099, RegistryValueKind.DWord);

                //Try to use old management server if local host found for management server
                if (managementServer == "127.0.0.1")
                {
                    managementServer = (String)getRegistryConfSafe(mydlpKey, "ManagementServer", "127.0.0.1", RegistryValueKind.String, false);
                    //set new key
                    mydlpKey.SetValue("management_server", managementServer, RegistryValueKind.String);
                }
                //try to delete old key anyway
                mydlpKey.DeleteValue("ManagementServer", false);
                //Get logLimit
                logLimit = (int)getRegistryConfSafe(mydlpKey, "log_limit", 10485760, RegistryValueKind.DWord);
                //Get maximumObjectSize
                maximumObjectSize = (int)getRegistryConfSafe(mydlpKey, "maximum_object_size", 10485760, RegistryValueKind.DWord);
                if (!Environment.UserInteractive)
                {
                    //Get loglevel
                    logLevel = (Logger.LogLevel)getRegistryConfSafe(mydlpKey, "log_level", 1, RegistryValueKind.DWord);
                    if (logLevel > Logger.LogLevel.DEBUG) logLevel = Logger.LogLevel.DEBUG;
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Unable to open registry key HKLM/Software/MyDLP "
                    + e);
                return false;
            }
            Logger.GetInstance().Info("MyDLP LogLevel: " + logLevel.ToString());
            Logger.GetInstance().Info("MyDLP ManagementServer: " + managementServer);
            Logger.GetInstance().Info("MyDLP ArchiveInbound: " + archiveInbound);
            Logger.GetInstance().Info("MyDLP USBSerialAccessControl: " + usbSerialAccessControl);
            Logger.GetInstance().Info("MyDLP PrinterMonitor: " + printerMonitor);
            Logger.GetInstance().Info("MyDLP LogLimit: " + logLimit);
            Logger.GetInstance().Info("MyDLP MaximumObjectSize: " + maximumObjectSize);
            Logger.GetInstance().Info("MyDLP PrinterPrefix: " + printerPrefix);
            return true;
        }

        public static object getRegistryConfSafe(RegistryKey key, String valueName, Object defaultValue, RegistryValueKind kind)
        {
            return getRegistryConfSafe(key, valueName, defaultValue, kind, true);
        }

        public static object getRegistryConfSafe(RegistryKey key, String valueName, Object defaultValue, RegistryValueKind kind, bool create)
        {
            object retVal;
            try
            {
                retVal = key.GetValue(valueName);
            }
            catch (Exception e)
            {
                try
                {
                    if (create)
                    {
                        Logger.GetInstance().Error("Unable to get registry value: " + key.ToString() + " " + valueName + " creating with default value:" + defaultValue);
                        key.SetValue(valueName, defaultValue, kind);
                    }
                }
                catch
                {
                    Logger.GetInstance().Error("Unable to create registry value: " + key.ToString() + " " + valueName + " with default value:" + defaultValue);
                }
                retVal = defaultValue;
            }

            if (retVal == null)
            {
                retVal = defaultValue;
                try
                {
                    if (create)
                    {
                        Logger.GetInstance().Error("Null registry value: " + key.ToString() + " " + valueName + " creating with default value:" + defaultValue);
                        key.SetValue(valueName, defaultValue, kind);
                    }
                }
                catch
                {
                    Logger.GetInstance().Error("Unable to create null registry value: " + key.ToString() + " " + valueName + " with default value:" + defaultValue);
                }
            }
            return retVal;
        }

        public static void setRegistryConfSafe(RegistryKey key, String valueName, Object newValue, RegistryValueKind kind)
        {
            try
            {
                key.SetValue(valueName, newValue, kind);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Unable to set registry value: " + key.ToString() + " " + valueName + " to:" + newValue);
            }
        }

        [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public extern static IntPtr LoadLibrary(string libraryName);

        [DllImport("kernel32", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        public extern static IntPtr GetProcAddress(IntPtr hwnd, string procedureName);

        public static OsVersion GetOs()
        {
            OperatingSystem os = Environment.OSVersion;
            Version vs = os.Version;

            if (os.Platform == PlatformID.Win32NT)
            {
                if (vs.Major == 5 && vs.Minor != 0)
                {
                    return OsVersion.XP;
                }
                else if (vs.Major == 6 && vs.Minor != 0)
                {
                    if (IsOS64Bit())
                    {
                        return OsVersion.Win7_64;
                    }
                    else
                    {
                        return OsVersion.Win7_32;
                    }
                }
            }

            return OsVersion.Unknown;
        }
        public static String GetMyDLPVersion()
        {
            if (version != "")
            {
                return version;
            }
            try
            {
                RegistryKey uninstallKey = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey(@"Microsoft\Windows\CurrentVersion\Uninstall");

                foreach (String keyName in uninstallKey.GetSubKeyNames())
                {
                    try
                    {
                        RegistryKey subKey = uninstallKey.OpenSubKey(keyName);

                        if (((String)getRegistryConfSafe(subKey, "DisplayName", "", RegistryValueKind.String, false)) == "MyDLP")
                        {

                            version = (String)getRegistryConfSafe(subKey, "DisplayVersion", "", RegistryValueKind.String, false);
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }
            catch (Exception e)
            {
                version = "N\\A";
            }
            return version;
        }

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

        public static bool RemovableStorageEncryption
        {
            get
            {
                return remStorEncryption;
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

        public static String KbFilterPath
        {
            get
            {
                return kbfilterPath;
            }
        }

        public static String JavaBackendPath
        {
            get
            {
                return javaBackendPath;
            }
        }

        public static String ErlangPath
        {
            get
            {
                return erlangPath;
            }
        }

        public static String ErlangHome
        {
            get
            {
                return erlangHome;
            }
        }

        public static String JavaBinPaths
        {
            get
            {
                return javaBinPaths;
            }
        }

        public static String JavaPath
        {
            get
            {
                return javaPath;
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

        public static int JavaPid
        {
            get
            {
                return javaPid;
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

        public static String PrintingDirPath
        {
            get
            {
                return printingDirPath;
            }
        }

        public static String PrintSpoolPath
        {
            get
            {
                return printSpoolPath;
            }
        }

        public static bool BlockScreenShot
        {
            get
            {
                return blockScreenShot;
            }
        }

        public static String ScreentShotProcesses
        {
            get
            {
                return screentShotProcesses;
            }
        }

        public static bool HasEncryptionKey
        {
            get
            {
                return hasEncryptionKey;
            }
            set
            {
                hasEncryptionKey = value;
            }
        }

        public static String PrinterPrefix
        {
            get
            {
                String safePrefix = NormalizePrefix(printerPrefix);
                return safePrefix;
            }
        }

        //Prefix will be a part of device path name and spool pathname so it should be normalized
        public static String NormalizePrefix(String prefix)
        {
            return prefix.Replace(":", "_")
                .Replace("\\", "_")
                .Replace("/", "_")
                .Replace("|", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("*", "_")
                .Replace(".", "_");
        }
    }
}

