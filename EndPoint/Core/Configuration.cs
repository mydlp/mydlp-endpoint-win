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

namespace MyDLP.EndPoint.Core
{
    public class Configuration
    {
        static String appPath;
        static String seapServer;
        static String managementServer;
        static int seapPort;
        static Logger.LogLevel logLevel;
        static String minifilterPath;
        static String pyBackendPath;
        static String erlangPath;
        static String pythonBinPaths;
        static String erlangBinPaths;
        static String pythonPath;
        static String mydlpConfPath;
        static bool archiveInbound;

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

                content = Regex.Replace(content, @"^[#\t\ ]*management_server_address\s+[0-9\.]+(\r)?$", "management_server_address\t" + managementServer + @"$1", RegexOptions.Multiline);
                content = Regex.Replace(content, @"^[#\t\ ]*archive_inbound\s+(true|false)[\t\ ]*(\r)?$", "archive_inbound\t" + archiveInbound.ToString().ToLower() + @"$2", RegexOptions.Multiline);

                StreamWriter writer = new StreamWriter(mydlpConfPath);
                writer.Write(content);
                writer.Close();
                return true;
            }
            catch (Exception e)
            {
                Logger.GetInstance().Debug("SetErlConf " + e.Message + "\n" + e.StackTrace);
                return false;
            }
        }

        public static bool GetRegistryConf()
        {
            if (System.Environment.UserInteractive)
            {
                //Use development conf
                minifilterPath = "C:\\workspace\\mydlp-endpoint-win\\EndPoint\\MiniFilter\\src\\objchk_wxp_x86\\i386\\MyDLPMF.sys";
                pyBackendPath = @"C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\backend\py\";
                erlangPath = @"C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\mydlp\";
                erlangBinPaths = @"C:\workspace\mydlp-deployment-env\erl5.7.4\bin;C:\workspace\mydlp-deployment-env\erl5.7.4\erts-5.7.4\bin";
                pythonBinPaths = @"C:\workspace\mydlp-deployment-env\Python26";
                pythonPath = @"C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\thrift\gen-py";
                appPath = @"C:\workspace\mydlp-development-env";
                seapServer = "127.0.0.1";
                managementServer = "127.0.0.1";
                archiveInbound = false;
                seapPort = 9099;
                mydlpConfPath = Configuration.ErlangPath + "mydlp-ep.conf";
                return true;
            }
            else
            {
                //Use normal conf
                try
                {
                    RegistryKey mydlpKey = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("MyDLP");

                    //Get path
                    try
                    {
                        appPath = mydlpKey.GetValue("AppPath").ToString();
                        minifilterPath = appPath + "MyDLPMF.sys";
                        pyBackendPath = appPath + "engine\\py\\";
                        erlangPath = appPath + "engine\\erl\\";
                        erlangBinPaths = appPath + @"erl5.7.4\bin;" + appPath + @"erl5.7.4\erts-5.7.4\bin";
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

                    //Get archiveInbound
                    try
                    {
                        if ((int)mydlpKey.GetValue("ArchiveInbound") == 0)
                        {
                            archiveInbound = false;
                        }
                        else
                        {
                            archiveInbound = true;
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Error("Unable to get registry value  HKLM/Software/MyDLP:ArchiveInbound "
                             + e.Message + " " + e.StackTrace);
                        return false;
                    }

                    //Get loglevel
                    try
                    {
                        logLevel = (Logger.LogLevel)mydlpKey.GetValue("LogLevel");
                        if (logLevel > Logger.LogLevel.DEBUG) logLevel = Logger.LogLevel.DEBUG;
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Error("Unable to get registry value  HKLM/Software/MyDLP:LogLevel "
                             + e.Message + " " + e.StackTrace);
                        return false;
                    }

                    //Get seapServer
                    try
                    {
                        seapServer = mydlpKey.GetValue("SeapServer").ToString();
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Error("Unable to get registry value  HKLM/Software/MyDLP:SeapServer "
                             + e.Message + " " + e.StackTrace);
                        return false;
                    }

                    //Get managementServer
                    try
                    {
                        managementServer = mydlpKey.GetValue("ManagementServer").ToString();
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Error("Unable to get registry value  HKLM/Software/MyDLP:ManagementServer "
                             + e.Message + " " + e.StackTrace);
                        return false;
                    }


                    //Get seapPort
                    try
                    {
                        seapPort = (int)mydlpKey.GetValue("SeapPort");
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Error("Unable to get registry value  HKLM/Software/MyDLP:SeapPort "
                             + e.Message + " " + e.StackTrace);
                        return false;
                    }

                    Logger.GetInstance().Info("MyDLP Path: " + appPath);
                    Logger.GetInstance().Info("MyDLP LogLevel: " + logLevel.ToString());
                    Logger.GetInstance().Info("MyDLP SeapServer: " + seapServer + ":" + seapPort);
                    Logger.GetInstance().Info("MyDLP ManagementServer: " + managementServer);
                    Logger.GetInstance().Info("MyDLP ArchiveInbound: " + archiveInbound);
                    Logger.GetInstance().Info("MyDLP AppPath: " + appPath);
                    Logger.GetInstance().Info("MyDLP ConfPath: " + mydlpConfPath);

                    return true;
                }
                catch (Exception e)
                {
                    Logger.GetInstance().Error("Unable to open registry key HKLM/Software/MyDLP "
                        + e.Message + " " + e.StackTrace);
                    return false;
                }
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
    }
}
