using System;
using System.Collections.Generic;
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
        public const String PrinterPrefix = "(MyDLP)";
        public const String SystemPrinterSecurityDescriptor =
            "O:SYG:SYD:(A;;LCSWSDRCWDWO;;;SY)(A;OIIO;RPWPSDRCWDWO;;;SY)";
        public const String BuiltinAdminsPrinterSecurityDescriptor =
            "O:SYG:SYD:(A;;LCSWSDRCWDWO;;;SY)(A;OIIO;RPWPSDRCWDWO;;;SY)(A;;LCSWSDRCWDWO;;;BA)(A;OIIO;RPWPSDRCWDWO;;;BA)";

        public static void Start()
        {
            SvcController.StopService("Spooler", 5000);
            //this is ugly but necessary
            Thread.Sleep(1000);
            if (CheckAndInstallPortMonitor())
            {
                SvcController.StartService("Spooler", 5000);
                Thread.Sleep(1000);
                //this is ugly but necessary
                CheckAndInstallXPSDriver();
                InstallSecurePrinters();
                TempSpooler.Start();
                
            }
            else
            {
                SvcController.StartService("Spooler", 5000);
            }
        }

        public static void Stop()
        {
            RemoveSecurePrinters();
        }

        public static void InstallSecurePrinters()
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

                    if (queue.QueueDriver.Name != "MyDLP XPS Printer Driver" ||
                        queue.QueuePort.Name != "MyDLP")
                    {
                        Logger.GetInstance().Debug(
                            "Not a secure printer installing installing secure version:" + queue.Name + "(MyDLP)");
                        try
                        {
                            pServer.InstallPrintQueue(PrinterPrefix + queue.Name,
                                "MyDLP XPS Printer Driver",
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

        public static void RemoveSecurePrinters()
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

                    if (queue.QueueDriver.Name == "MyDLP XPS Printer Driver" ||
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
                        Logger.GetInstance().Debug("A non-secure printer found revealing " + queue.Name);
                        //MyDLPEP.PrinterUtils.RevealPrinter(queue.Name);
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

        public static bool CheckAndInstallPortMonitor()
        {
            try
            {
                String system32Path = Environment.GetEnvironmentVariable("windir") + @"\\System32";

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

        public static bool CheckAndInstallXPSDriver()
        {
            try
            {
                X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                X509Certificate2 mydlpPubCert = new X509Certificate2(Configuration.PrintingDirPath + "mydlppub.cer");
                store.Open(OpenFlags.ReadWrite);
                store.Add(mydlpPubCert);


                ProcessStartInfo procStartInfo =
                    new ProcessStartInfo("rundll32", " printui.dll,PrintUIEntry /ia /m \"MyDLP XPS Printer Driver\" /f \"" + Configuration.PrintingDirPath + "MyDLPXPSDrv.inf\"");
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;

                Process proc = new Process();
                proc.StartInfo = procStartInfo;
                Logger.GetInstance().Debug("Starting process:" + procStartInfo.Arguments);
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                Logger.GetInstance().Debug(result);

                return true;
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Error install printer driver:" + e.Message);
                return false;
            }
        }

        public static bool HasSubKey(RegistryKey key, String subKeyName)
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
    }
}
