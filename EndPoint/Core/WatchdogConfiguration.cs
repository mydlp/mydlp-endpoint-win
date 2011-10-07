using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Win32;
using System.Diagnostics;

namespace MyDLP.EndPoint.Core
{
    public class WatchdogConfiguration
    {
        public static String GetLogPath()
        {
            if (System.Environment.UserInteractive)
            {
                return @"C:\workspace\mydlp-development-env\logs\mydlpepwd.log";
            }
            else
            {
                try
                {
                    RegistryKey mydlpKey = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("MyDLP");

                    //Get path
                    try
                    {
                        return mydlpKey.GetValue("AppPath").ToString() + @"logs\mydlpepwd.log";
                    }
                    catch (Exception e)
                    {
                        return @"C:\mydlpepwd.log";
                    }
                }
                catch (Exception e)
                {
                    return @"C:\mydlpepwd.log";
                }
            }
        }

    }
}
