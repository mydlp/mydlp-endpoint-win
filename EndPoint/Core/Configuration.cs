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

        public static bool GetRegistryConf()
        {
            if (System.Environment.UserInteractive)
            {
                //Use development conf

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
