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
using Microsoft.Win32;

namespace MyDLP.EndPoint.Core
{
    public class Configuration
    {
        static String path;
        static String seapServer;
        static int seapPort;
        static Logger.LogLevel logLevel;
        static String minifilterPath; 

        public static String Path
        {
            get
            {
                return path;
            }
        }

        public static String SeapServer
        {
            get
            {
                return seapServer;
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


        public static bool GetRegistryConf()
        {
            if (System.Environment.UserInteractive)
            {
                //Use development conf
                minifilterPath = "C:\\workspace\\mydlp-endpoint-win\\EndPoint\\MiniFilter\\src\\objchk_wxp_x86\\i386\\MyDLPMF.sys";
            

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
                        path = mydlpKey.GetValue("Path").ToString();
                        minifilterPath = path; 
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Error("Unable to get registry value  HKLM/Software/MyDLP:Path "
                            + e.Message + " " + e.StackTrace);
                        return false;
                    }

                    //Get loglevel
                    try
                    {
                        logLevel = (Logger.LogLevel)mydlpKey.GetValue("LogLevel");
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

                    Logger.GetInstance().Info("MyDLP Path: " + path);
                    Logger.GetInstance().Info("MyDLP LogLevel: " + logLevel.ToString());
                    Logger.GetInstance().Info("MyDLP SeapServer: " + seapServer + ":" + seapPort);

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
    }
}
