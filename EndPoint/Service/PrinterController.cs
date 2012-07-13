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
using System.Collections;
using System.Text;
using System.Printing;
using Microsoft.Win32;
using System.Threading;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Runtime.InteropServices;
using MyDLP.EndPoint.Core;
using System.ComponentModel;
namespace MyDLP.EndPoint.Service
{
    public class PrinterController
    {
        [DllImport("Advapi32.dll")]
        static extern bool GetUserName(StringBuilder lpBuffer, ref int nSize);

        [DllImport("printui.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern void PrintUIEntryW(IntPtr hwnd,
            IntPtr hinst, string lpszCmdLine, int nCmdShow);
        [DllImport("kernel32.dll")]
        static extern void SetLastError(uint dwErrCode);

        static PrinterController instance = null;

        ArrayList spooledNativePrinters;

        const String PrinterPrefix = "(MyDLP)";
        const String MyDLPDriver = "MyDLP XPS Printer Driver";

        const String SystemPrinterSecurityDescriptor =
            "O:SYG:SYD:(A;;LCSWSDRCWDWO;;;SY)(A;OIIO;RPWPSDRCWDWO;;;SY)";

        const String BuiltinAdminsPrinterSecurityDescriptor =
            "O:SYG:SYD:(A;;LCSWSDRCWDWO;;;SY)(A;OIIO;RPWPSDRCWDWO;;;SY)(A;;LCSWSDRCWDWO;;;BA)(A;OIIO;RPWPSDRCWDWO;;;BA)";

        const String sysEntry1 = "(A;OIIO;RPWPSDRCWDWO;;;SY)";
        const String sysEntry2 = "(A;;LCSWSDRCWDWO;;;SY)";
        const String admEntry1 = "(A;OIIO;RPWPSDRCWDWO;;;BA)";
        const String admEntry2 = "(A;;LCSWSDRCWDWO;;;BA)";

        public void Start()
        {
            SvcController.StopService("Spooler", 5000);
            //this is ugly but necessary
            Thread.Sleep(1000);

            if (CheckAndInstallPortMonitor())
            {
                SvcController.StartService("Spooler", 5000);
                Thread.Sleep(1000);

                if (CheckAndInstallXPSDriver())
                {   
                    //Correct incase of an improper shutdown
                    RemoveSecurePrinters();
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

        public static PrinterController getInstance()
        {
            if (instance == null)
                instance = new PrinterController();
            return instance;
        }

        private void InstallSecurePrinters()
        {
            try
            {
                Logger.GetInstance().Debug("InstallSecurePrinters started");

                LocalPrintServer pServer = new LocalPrintServer();
                PrintQueueCollection queueCollection = pServer.GetPrintQueues();
                String securityDesc;

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
                            MyDLPEP.PrinterUtils.TakePrinterOwnership(queue.Name);
                            securityDesc = MyDLPEP.PrinterUtils.GetPrinterSecurityDescriptor(queue.Name);
                            if (securityDesc != "")
                            {
                                if (!securityDesc.Contains(admEntry1))
                                    securityDesc += admEntry1;
                                if (!securityDesc.Contains(admEntry2))
                                    securityDesc += admEntry2;
                                if (!securityDesc.Contains(sysEntry1))
                                    securityDesc += sysEntry1;
                                if (!securityDesc.Contains(sysEntry2))
                                    securityDesc += sysEntry2;

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

                            //This is netiher required nor working in windows 7
                            if (!queue.IsDirect && Configuration.GetOs() == Configuration.OsVersion.XP)
                            {
                                Logger.GetInstance().Debug("Found spooling native printer " + queue.Name);
                                MyDLPEP.PrinterUtils.SetPrinterSpoolMode(queue.Name, false);
                                spooledNativePrinters.Add(queue.Name);
                            }
                        }
                        catch (Exception e)
                        {
                            Logger.GetInstance().Debug("Unable to process non-secure printer " + queue.Name
                                + " error:" + e.Message + " " + e.StackTrace);
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

        private void RemoveSecurePrinters()
        {
            try
            {
                Logger.GetInstance().Debug("RemoveSecurePrinters started");

                LocalPrintServer pServer = new LocalPrintServer();
                PrintQueueCollection queueCollection = pServer.GetPrintQueues();
                foreach (PrintQueue queue in queueCollection)
                {
                    Logger.GetInstance().Debug("Process printer queue: " + queue.Name
                       + " driver: " + queue.QueueDriver.Name + " port: " + queue.QueuePort.Name);

                    if (queue.QueueDriver.Name == MyDLPDriver ||
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

        private bool CheckAndInstallPortMonitor()
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

        private bool CheckAndInstallXPSDriver()
        {
            try
            {
                if (MyDLPEP.PrinterUtils.CheckIfPrinterDriverExists(MyDLPDriver))
                {
                    Logger.GetInstance().Debug("MyDLP XPS Driver exists");
                    return true;
                }
                //PrintUI.dll does not work in a windows service on Windows XP use manual
                else if (Configuration.GetOs() == Configuration.OsVersion.Win7_32
                    || Configuration.GetOs() == Configuration.OsVersion.Win7_64)
                {
                    Logger.GetInstance().Debug("Installing MyDLP XPS Driver automatically");
                    X509Store store = new X509Store(StoreName.TrustedPublisher, StoreLocation.LocalMachine);
                    X509Certificate2 mydlpPubCert = new X509Certificate2(Configuration.PrintingDirPath + "mydlppub.cer");
                    store.Open(OpenFlags.ReadWrite);
                    store.Add(mydlpPubCert);

                    SetLastError(0);
                    PrintUIEntryW(IntPtr.Zero, IntPtr.Zero, "/ia /m \"MyDLP XPS Printer Driver\" /q /f \"" + Configuration.PrintingDirPath + "MyDLPXPSDrv.inf\"", 0);
                    int lastError = Marshal.GetLastWin32Error();
                    Logger.GetInstance().Debug("PrintUIEntryW last error no:" + lastError + " message:" + (new Win32Exception(lastError)).Message);
                    if (lastError != 0) throw new Win32Exception(lastError);
                    return true;
                }
                else
                {
                    Logger.GetInstance().Error("MyDLP XPS Driver not found on XP machine, run installdriverXP.bat manually");
                    return false;
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Error install printer driver:" + e.Message);
                return false;
            }
        }

        private bool HasSubKey(RegistryKey key, String subKeyName)
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

        private PrinterController()
        {
            spooledNativePrinters = new ArrayList();
        }
    }
}
