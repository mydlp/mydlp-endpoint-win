using System;
using System.Collections;
using System.Text;
using System.Printing;
using Microsoft.Win32;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using MyDLP.EndPoint.Core;

namespace MyDLP.EndPoint.Service
{
    public class PrinterController
    {
        static PrinterController instance = null;

        ArrayList spooledNativePrinters;

        public const String PrinterPrefix = "(MyDLP)";
        public const String MyDLPDriver = "MyDLP XPS Printer Driver";

        public const String SystemPrinterSecurityDescriptor =
            "O:SYG:SYD:(A;;LCSWSDRCWDWO;;;SY)(A;OIIO;RPWPSDRCWDWO;;;SY)";
        
        public const String BuiltinAdminsPrinterSecurityDescriptor =
            "O:SYG:SYD:(A;;LCSWSDRCWDWO;;;SY)(A;OIIO;RPWPSDRCWDWO;;;SY)(A;;LCSWSDRCWDWO;;;BA)(A;OIIO;RPWPSDRCWDWO;;;BA)";

        public void Start()
        {
            SvcController.StopService("Spooler", 5000);
            //this is ugly but necessary
            Thread.Sleep(1000);
            //if (CheckAndInstallPortMonitor() && SetSystemPrinterRegistry())
            if (CheckAndInstallPortMonitor())
            {
                SvcController.StartService("Spooler", 5000);
                Thread.Sleep(1000);
                
                if (CheckAndInstallXPSDriver())
                {
                    InstallSecurePrinters();
                    TempSpooler.Start();
                }

            }
            else
            {
                SvcController.StartService("Spooler", 5000);
                TempSpooler.Stop();
            }
        }

        public void Stop()
        {
            RemoveSecurePrinters();
        }

        public void InstallSecurePrinters()
        {
            Configuration.OsVersion version = Configuration.GetOs();
            try
            {
                Logger.GetInstance().Debug("InstallSecurePrinters started");

                LocalPrintServer pServer = new LocalPrintServer();
                PrintQueueCollection queueCollection = pServer.GetPrintQueues();
                foreach (PrintQueue queue in queueCollection)
                {
                    Logger.GetInstance().Debug("Process printer queue: " + queue.Name
                        + " driver: " + queue.QueueDriver.Name + " port: " + queue.QueuePort.Name);

                    if (queue.QueueDriver.Name != MyDLPDriver ||
                        queue.QueuePort.Name != "MyDLP")
                    {
                        Logger.GetInstance().Debug(
                            "Not a secure printer installing installing secure version:" + queue.Name + "(MyDLP)");
                        try
                        {
                            pServer.InstallPrintQueue(PrinterPrefix + queue.Name,
                                MyDLPDriver,
                                new String[] { "MyDLP" },
                                "winprint",
                                PrintQueueAttributes.Direct);

                            String securityDesc = MyDLPEP.PrinterUtils.GetPrinterSecurityDescriptor(queue.Name);
                            if (securityDesc != "")
                            {
                                MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(PrinterPrefix + queue.Name, securityDesc);
                            }

                            if (Environment.UserInteractive)
                            {
                                MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(queue.Name, BuiltinAdminsPrinterSecurityDescriptor);
                            }
                            else
                            {
                                MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(queue.Name, SystemPrinterSecurityDescriptor);
                            }

                            if (!queue.IsDirect)
                            {
                                Logger.GetInstance().Debug("Found spooling native printer " + queue.Name);
                                MyDLPEP.PrinterUtils.SetPrinterSpoolMode(queue.Name, false);
                                spooledNativePrinters.Add(queue.Name);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Debug("Unable to install printer " + queue.Name + " error:" + e.Message);
                        }

                    }
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("InstallSecurePrinters failed: " + e.Message);
            }
            finally
            {
                Logger.GetInstance().Debug("InstallSecurePrinters ended");
            }
        }

        public void RemoveSecurePrinters()
        {
            try
            {
                Logger.GetInstance().Debug("InstallSecurePrinters started");
                Logger.GetInstance().Debug("RemoveSecurePrinters started");

                LocalPrintServer pServer = new LocalPrintServer();
                PrintQueueCollection queueCollection = pServer.GetPrintQueues();
                foreach (PrintQueue queue in queueCollection)
                {
                    Logger.GetInstance().Debug("Process printer queue: " + queue.Name
                       + " driver: " + queue.QueueDriver.Name + " port: " + queue.QueuePort.Name);

                    if (queue.QueueDriver.Name == MyDLPDriver||
                        queue.QueuePort.Name == "MyDLP")
                    {
                        Logger.GetInstance().Debug("A secure printer found removing " + queue.Name);

                        if (queue.Name.StartsWith(PrinterPrefix))
                        {
                            String securityDesc = MyDLPEP.PrinterUtils.GetPrinterSecurityDescriptor(queue.Name);
                            if (securityDesc != "")
                            {
                                MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(
                                    queue.Name.Substring(PrinterPrefix.Length),
                                    securityDesc);
                            }
                        }
                        MyDLPEP.PrinterUtils.RemovePrinter(queue.Name);

                    }
                    else
                    {
                        Logger.GetInstance().Debug("A non-secure printer found " + queue.Name);

                        if (spooledNativePrinters.Contains(queue.Name))
                        {
                            Logger.GetInstance().Debug("Reenabling spooling " + queue.Name);
                            MyDLPEP.PrinterUtils.SetPrinterSpoolMode(queue.Name, true);
                            spooledNativePrinters.Remove(queue.Name);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Remove secure pritners failed: " + e.Message);
            }
            finally
            {
                Logger.GetInstance().Debug("RemoveSecurePrinters ended");
            }
        }

        public bool CheckAndInstallPortMonitor()
        {
            try
            {
                String system32Path = Environment.GetEnvironmentVariable("windir") + @"\\System32";

                if (Configuration.GetOs() == Configuration.OsVersion.Win7_64)
                {
                    system32Path = Environment.GetEnvironmentVariable("windir") + @"\\Sysnative";
                }

                String sourceFile;
                String sourceUIFile;
                String destinationFile;
                String destinationUIFile;

                Configuration.OsVersion version = Configuration.GetOs();

                if (version == Configuration.OsVersion.Win7_32)
                {
                    sourceFile = System.IO.Path.Combine(Configuration.PrintingDirPath, "mydlpportmon_win7_x86.dll");
                    sourceUIFile = System.IO.Path.Combine(Configuration.PrintingDirPath, "mydlpportui_win7_x86.dll");
                }
                else if (version == Configuration.OsVersion.Win7_64)
                {
                    sourceFile = System.IO.Path.Combine(Configuration.PrintingDirPath, "mydlpportmon_win7_x64.dll");
                    sourceUIFile = System.IO.Path.Combine(Configuration.PrintingDirPath, "mydlpportui_win7_x64.dll");
                }
                else if (version == Configuration.OsVersion.XP)
                {
                    sourceFile = System.IO.Path.Combine(Configuration.PrintingDirPath, "mydlpportmon_xp_x86.dll");
                    sourceUIFile = System.IO.Path.Combine(Configuration.PrintingDirPath, "mydlpportui_xp_x86.dll");
                }
                else
                {
                    Logger.GetInstance().Error("Unknown incompatible windows version");
                    return false;
                }

                destinationFile = System.IO.Path.Combine(system32Path, "mydlpportmon.dll");
                destinationUIFile = System.IO.Path.Combine(system32Path, "mydlpportui.dll");

                //Copy files
                System.IO.File.Copy(sourceFile, destinationFile, true);
                System.IO.File.Copy(sourceUIFile, destinationUIFile, true);

                //Set registry for spooler service
                RegistryKey monitorsKey = Registry.LocalMachine.OpenSubKey(@"System\CurrentControlSet\Control\Print\Monitors", true);
                RegistryKey mydlpMonitorKey;
                RegistryKey mydlpPortsKey;

                if (HasSubKey(monitorsKey, "MyDLP Port Monitor"))
                {
                    mydlpMonitorKey = monitorsKey.OpenSubKey("MyDLP Port Monitor", true);
                }
                else
                {
                    mydlpMonitorKey = monitorsKey.CreateSubKey("MyDLP Port Monitor", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }

                mydlpMonitorKey.SetValue("Driver", "mydlpportmon.dll", RegistryValueKind.String);


                if (HasSubKey(mydlpMonitorKey, "Ports"))
                {
                    mydlpPortsKey = mydlpMonitorKey.OpenSubKey("Ports", true);
                }
                else
                {
                    mydlpPortsKey = mydlpMonitorKey.CreateSubKey("Ports", RegistryKeyPermissionCheck.ReadWriteSubTree);
                }

                mydlpPortsKey.SetValue("MyDLP", "", RegistryValueKind.String);

            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Error in install port monitor:" + e.Message);
                return false;
            }

            return true;
        }

        public bool CheckAndInstallXPSDriver()
        {
            try
            {
                ProcessStartInfo procStartInfo;

                //PrintUI.dll does not work in a windows service on Windows XP
                if (Configuration.GetOs() == Configuration.OsVersion.Win7_32
                    || Configuration.GetOs() == Configuration.OsVersion.Win7_64)
                {
                    X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                    X509Certificate2 mydlpPubCert = new X509Certificate2(Configuration.PrintingDirPath + "mydlppub.cer");
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(mydlpPubCert);

                    procStartInfo = new ProcessStartInfo("rundll32", " printui.dll,PrintUIEntry /ia /m \"MyDLP XPS Printer Driver\" /f \"" + Configuration.PrintingDirPath + "MyDLPXPSDrv.inf\"");


                    procStartInfo.RedirectStandardOutput = true;
                    procStartInfo.UseShellExecute = false;
                    procStartInfo.CreateNoWindow = true;

                    Process proc = new Process();
                    proc.StartInfo = procStartInfo;
                    Logger.GetInstance().Debug("Starting process:" + procStartInfo.Arguments);
                    proc.Start();
                    string result = proc.StandardOutput.ReadToEnd();
                    Logger.GetInstance().Debug(result);
                }
                //Check only if driver is preinstalled on Windows XP
                else if (Configuration.GetOs() == Configuration.OsVersion.XP)
                {
                    return MyDLPEP.PrinterUtils.CheckIfPrinterDriverExists(MyDLPDriver);              
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Error install printer driver:" + e.Message);
                return false;
            }
        }

        //this is for printing from local system account
        /*public bool SetSystemPrinterRegistry()
        {
            Logger.GetInstance().Debug("SetSystemPrinterRegistry started");
            try
            {
                RegistryKey currentUserKey = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows NT\CurrentVersion", true);
                RegistryKey currentUserDevicesKey = currentUserKey.OpenSubKey("Devices", true);
                RegistryKey currentUserPortsKey = currentUserKey.OpenSubKey("PrinterPorts", true);

                RegistryKey defaultUserKey = Registry.Users.OpenSubKey(@".DEFAULT\Software\Microsoft\Windows NT\CurrentVersion", true);
                RegistryKey defaultUserDevicesKey;
                RegistryKey defaultUserPortsKey;

                if (HasSubKey(defaultUserKey, "Devices"))
                {
                    defaultUserDevicesKey = defaultUserKey.OpenSubKey("Devices", true);
                }
                else
                {
                    defaultUserDevicesKey = defaultUserKey.CreateSubKey("Devices",
                        RegistryKeyPermissionCheck.ReadWriteSubTree);
                }

                if (HasSubKey(defaultUserKey, "PrinterPorts"))
                {
                    defaultUserPortsKey = defaultUserKey.OpenSubKey("PrinterPorts", true);
                }
                else
                {
                    defaultUserPortsKey = defaultUserKey.CreateSubKey("PrinterPorts", 
                        RegistryKeyPermissionCheck.ReadWriteSubTree);
                }

                String[] currentUserDeviceValueNames = currentUserDevicesKey.GetValueNames();

                foreach (String currentValueName in currentUserDeviceValueNames)
                {
                    String value = (String)currentUserDevicesKey.GetValue(currentValueName, "");
                    defaultUserDevicesKey.SetValue(currentValueName, value, RegistryValueKind.String);
                }

                String[] currentUserPortValueNames = currentUserPortsKey.GetValueNames();

                foreach (String currentValueName in currentUserPortValueNames)
                {
                    String value = (String)currentUserPortsKey.GetValue(currentValueName, "");
                    defaultUserPortsKey.SetValue(currentValueName, value, RegistryValueKind.String);
                }

                return true;
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("SetSystemPrinterRegistry error: " + 
                    e.Message + " " + e.StackTrace);
                
                return false;
            } 
        }*/

        public bool HasSubKey(RegistryKey key, String subKeyName)
        {
            bool hasKey = false;

            foreach (String name in key.GetSubKeyNames())
            {
                if (name == subKeyName)
                {
                    hasKey = true;
                    break;
                }
            }
            return hasKey;
        }

        public static PrinterController getInstance()
        {
            if (instance == null)
                instance = new PrinterController(); 
            return instance;
        }

        private PrinterController()
        {
            spooledNativePrinters = new ArrayList();
        }
    }
}
