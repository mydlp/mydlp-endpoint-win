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
using System.Collections.Generic;
using System.Text;
using System.Printing;
using Microsoft.Win32;
using System.Threading;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;
using System.ComponentModel;
using MyDLP.EndPoint.Core;
using System.Management;

namespace MyDLP.EndPoint.Service
{
    public class PrinterController
    {
        //Imports for native functions
        [DllImport("Advapi32.dll")]
        static extern bool GetUserName(StringBuilder lpBuffer, ref int nSize);
        [DllImport("printui.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern void PrintUIEntryW(IntPtr hwnd,
            IntPtr hinst, string lpszCmdLine, int nCmdShow);
        [DllImport("kernel32.dll")]

        static extern void SetLastError(uint dwErrCode);
        //Singleton instance
        static PrinterController instance = null;
        //SID of current user to get/listen shared printer connections        
        static String currentSid;
        //Status of PrintController isntance 
        static bool started = false;
        //Windows OS permissions <queue.Name, permissionACLstring>
        Dictionary<string, string> printerPermissions;
        //Shared Printer Connections items: "name,server"
        ArrayList printerConnections;
        //Spooled printers list to be reverted back(only for Windows XP)
        static ArrayList spooledNativePrinters;
        bool handlingConnectionChange = false;
        bool changingLocalPrinters = false;
        //Resgistry watcher for shared printers
        ManagementEventWatcher watcher;

        //Prefix for secure printers
        static String PrinterPrefix = "(MyDLP)";
        //MyDLP driver name - fixed 
        const String MyDLPDriver = "MyDLP XPS Printer Driver";
        const String SystemPrinterSecurityDescriptor =
            "O:SYG:SYD:(A;;LCSWSDRCWDWO;;;SY)(A;OIIO;RPWPSDRCWDWO;;;SY)";
        const String BuiltinAdminsPrinterSecurityDescriptor =
            "O:SYG:SYD:(A;;LCSWSDRCWDWO;;;SY)(A;OIIO;RPWPSDRCWDWO;;;SY)(A;;LCSWSDRCWDWO;;;BA)(A;OIIO;RPWPSDRCWDWO;;;BA)";

        /*const String sysEntry1 = "(A;OIIO;RPWPSDRCWDWO;;;SY)";
        const String sysEntry2 = "(A;;LCSWSDRCWDWO;;;SY)";
        const String admEntry1 = "(A;OIIO;RPWPSDRCWDWO;;;BA)";
        const String admEntry2 = "(A;;LCSWSDRCWDWO;;;BA)";*/
        const String authUserPrint = "(A;OI;SWRC;;;AU)";

        public static PrinterController getInstance()
        {
            if (instance == null)
                instance = new PrinterController();
            return instance;
        }

        public void Start()
        {
            if (started)
            {
                return;
            }
            PrinterPrefix = Configuration.PrinterPrefix;
            printerPermissions = new Dictionary<string, string>();
            spooledNativePrinters = new ArrayList();

            if (CheckAndInstallPortMonitor())
            {
                if (CheckAndInstallXPSDriver())
                {
                    //Correct incase of an improper shutdown
                    RemoveLocalSecurePrinters();
                    if (TempSpooler.Start())
                    {
                        Thread changeListeningThread = new Thread(new ThreadStart(MyDLPEP.PrinterUtils.StartBlockingLocalChangeListener));
                        changeListeningThread.Start();
                        started = true;
                        Configuration.GetLoggedOnUser();
                        HandlePrinterConnectionChange();
                        InstallLocalSecurePrinters();
                    }
                    else
                    {
                        MyDLPEP.PrinterUtils.listenChanges = false;
                        RemoveLocalSecurePrinters();
                    }
                }
            }

            if (!started)
            {
                SvcController.StartService("Spooler", 5000);
            }
        }

        public void Stop()
        {
            if (!started)
            {
                return;
            }
            TempSpooler.Stop();
            //listen changes should be changed before removing secure printers
            MyDLPEP.PrinterUtils.listenChanges = false;
            RemoveLocalSecurePrinters();
            started = false;
        }

        public void LocalPrinterRemoveHandler()
        {
            Logger.GetInstance().Debug("LocalPrinterRemoveHandler started");
            LocalPrintServer pServer = new LocalPrintServer();
            PrintQueueCollection queueCollection = pServer.GetPrintQueues();

            String fallbackDefaultSecurePrinterName = "";

            foreach (PrintQueue mQueue in queueCollection)
            {
                if (mQueue.QueueDriver.Name == MyDLPDriver ||
                    mQueue.QueuePort.Name == "MyDLP")
                {
                    bool exists = false;

                    //Check if it is a shared printer
                    foreach (PrinterConnection connection in printerConnections)
                    {
                        if (connection.secureName == mQueue.Name)
                        {
                            //found a secure printer for non-local printer skipping;
                            exists = true;
                            fallbackDefaultSecurePrinterName = mQueue.Name;
                        }
                    }

                    //Check if there is a non-secure printer for for this printer
                    foreach (PrintQueue queue in queueCollection)
                    {
                        if (queue.QueueDriver.Name != MyDLPDriver ||
                            queue.QueuePort.Name != "MyDLP")
                        {
                            if (mQueue.Name == GetSecurePrinterName(queue.Name))
                            {
                                exists = true;
                                fallbackDefaultSecurePrinterName = mQueue.Name;
                            }
                        }
                    }
                    if (!exists)
                    {
                        MyDLPEP.PrinterUtils.RemovePrinter(mQueue.Name);

                        MyDLPEP.SessionUtils.ImpersonateActiveUser();
                        String defaultPrinterName = MyDLPEP.PrinterUtils.GetDefaultSystemPrinter();
                        MyDLPEP.SessionUtils.StopImpersonation();
                        if (mQueue.Name == defaultPrinterName)
                        {
                            //Real default printer removed do something
                            if (fallbackDefaultSecurePrinterName != "")
                            {
                                MyDLPEP.SessionUtils.ImpersonateActiveUser();
                                MyDLPEP.PrinterUtils.SetDefaultSystemPrinter(fallbackDefaultSecurePrinterName);
                                MyDLPEP.SessionUtils.StopImpersonation();
                            }
                            else
                            {
                                //there is no fall back printer
                                MyDLPEP.PrinterUtils.SetDefaultSystemPrinter("");
                            }
                        }
                    }
                }
            }
        }

        public void LocalPrinterAddHandler()
        {
            Logger.GetInstance().Debug("LocalPrinterAddHandler started");
            InstallLocalSecurePrinters();
        }

        public bool IsPrinterConnection(string secureName)
        {
            foreach (PrinterConnection conneciton in printerConnections)
            {
                if (conneciton.secureName == secureName)
                    return true;
            }

            return false;
        }

        public PrinterConnection GetPrinterConnection(string secureName)
        {
            foreach (PrinterConnection conneciton in printerConnections)
            {
                if (conneciton.secureName == secureName)
                    return conneciton;
            }

            return null;
        }

        public static String GetSecurePrinterName(String qName)
        {

            qName = PrinterPrefix + NormalizePrinterName(qName);
            return qName;
        }

        public static String NormalizePrinterName(String qName)
        {
            return qName.Replace(":", "_")
                .Replace("\\", "_")
                .Replace("/", "_")
                .Replace("|", "_")
                .Replace("<", "_")
                .Replace(">", "_")
                .Replace("*", "_")
                .Replace(".", "_");

        }

        public void ListenPrinterConnections(String sid)
        {
            if (!started || currentSid == sid)
            {
                return;
            }
            currentSid = sid;
            printerConnections.Clear();
            HandlePrinterConnectionChange();

            try
            {
                if (watcher != null)
                {
                    watcher.Stop();
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("Unable to stop shared printer connections registry watcher:" + ex.Message + ex.StackTrace);
            }

            try
            {
                Logger.GetInstance().Debug(sid + @"\\Printers\\Connections");
                WqlEventQuery query = new WqlEventQuery(
                           "SELECT * FROM RegistryKeyChangeEvent WHERE " +
                           "Hive = 'HKEY_USERS'" +
                           @" AND KeyPath = '" + sid + @"\\Printers\\Connections'");
                watcher = new ManagementEventWatcher(new ManagementScope("\\\\localhost\\root\\default"), query);
                watcher.EventArrived += new EventArrivedEventHandler(HandleEvent);
                watcher.Start();
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("Unable to start shared printer connections registry watcher:" + ex.Message + ex.StackTrace);
            }
        }

        public void HandlePrinterConnectionChange()
        {
            bool change = false;
            lock (this)
            {
                if (handlingConnectionChange)
                {
                    return;
                }
                else handlingConnectionChange = true;
            }
            try
            {
                Logger.GetInstance().Debug("HandlePrinterConnectionChange started");
                RegistryKey connectionsKey = Registry.Users.OpenSubKey(currentSid + "\\Printers\\Connections");
                ArrayList newConnectionList = new ArrayList();

                foreach (String connection in connectionsKey.GetSubKeyNames())
                {
                    try
                    {
                        string[] list = connection.Split(',');
                        PrinterConnection newConnection = new PrinterConnection(list[3], list[2]);

                        //Keep track of recent connections
                        newConnectionList.Add(newConnection);
                        //Add only if there is not already such secure printer
                        if (!printerConnections.Contains(newConnection))
                        {
                            AddSecurePrinterForConnection(newConnection);
                            change = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.GetInstance().Error("ListenPrinterConnections:" + ex.Message + " " + ex.StackTrace);
                        continue;
                    }
                }

                //Remove old outdated connections
                for (int i = 0; i < printerConnections.Count; i++)
                {
                    PrinterConnection connection = (PrinterConnection)printerConnections[i];
                    if (!newConnectionList.Contains(connection))
                    {
                        Logger.GetInstance().Debug("Removing old connection" + connection);
                        RemoveSecurePrinterForConnection(connection);
                        printerConnections.Remove(connection);
                        i--;
                        change = true;
                    }
                }
                Logger.GetInstance().Debug("HandlePrinterConnectionChange ended");

                if (change)
                {
                    //Give windows time to update default printer
                    Thread.Sleep(2000);

                    UpdateDefaultPrinter();
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("ListenPrinterConnections:" + ex.Message + " " + ex.StackTrace);
            }

            lock (this)
            {
                handlingConnectionChange = false;
            }
        }

        private void HandleEvent(object sender, EventArrivedEventArgs e)
        {
            HandlePrinterConnectionChange();
        }

        private void AddSecurePrinterForConnection(PrinterConnection connection)
        {
            try
            {
                LocalPrintServer pServer = new LocalPrintServer();

                bool exists = false;

                foreach (PrintQueue queue in pServer.GetPrintQueues())
                {
                    if (queue.Name == connection.secureName)
                    {
                        exists = true;
                    }
                }

                if (!exists)
                {
                    pServer.InstallPrintQueue(connection.secureName,
                    MyDLPDriver,
                    new String[] { "MyDLP" },
                   "winprint",
                    PrintQueueAttributes.Direct);
                }

                printerConnections.Add(connection);
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("AddSecurePrinterForConnection: " + connection + " " + ex.Message + " " + ex.StackTrace);
            }

        }

        private void RemoveSecurePrinterForConnection(PrinterConnection connection)
        {
            try
            {
                LocalPrintServer pServer = new LocalPrintServer();
                MyDLPEP.PrinterUtils.RemovePrinter(connection.secureName);
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("RemoveSecurePrinterForConnection: " + connection + " " + ex.Message + " " + ex.StackTrace);
            }
        }

        private void InstallLocalSecurePrinters()
        {
            bool change = false;
            lock (this)
            {
                if (changingLocalPrinters)
                {
                    return;
                }
                else changingLocalPrinters = true;
            }
            try
            {
                Logger.GetInstance().Debug("InstallSecurePrinters started");

                LocalPrintServer pServer = new LocalPrintServer();
                PrintQueue mydlpQueue;
                PrintQueueCollection queueCollection = pServer.GetPrintQueues();
                String securityDesc;

                foreach (PrintQueue queue in queueCollection)
                {
                    //Logger.GetInstance().Debug("Process printer queue: " + queue.Name
                    //    + " driver: " + queue.QueueDriver.Name + " port: " + queue.QueuePort.Name);


                    if (queue.QueueDriver.Name != MyDLPDriver ||
                        queue.QueuePort.Name != "MyDLP")
                    {

                        //check if secure printer already exists
                        bool exists = false;
                        PrintQueueCollection collection = pServer.GetPrintQueues();
                        foreach (PrintQueue q in queueCollection)
                        {
                            if (q.Name == GetSecurePrinterName(queue.Name))
                            {
                                if (q.QueueDriver.Name == MyDLPDriver && q.QueuePort.Name == "MyDLP")
                                {
                                    exists = true;
                                }
                                else
                                {
                                    //fix any incorrect ports
                                    Logger.GetInstance().Debug(q.QueueDriver.Name + q.QueuePort.Name);
                                    MyDLPEP.PrinterUtils.RemovePrinter(q.Name);
                                }
                            }
                        }

                        if (!exists)
                        {
                            Logger.GetInstance().Debug(
                                "Not a secure printer installing installing secure version:" + queue.Name + PrinterPrefix);
                            try
                            {
                                String mydlpQueueName = GetSecurePrinterName(queue.Name);
                                mydlpQueue = pServer.InstallPrintQueue(mydlpQueueName,
                                    MyDLPDriver,
                                    new String[] { "MyDLP" },
                                    "winprint",
                                    PrintQueueAttributes.Direct);
                                change = true;
                                MyDLPEP.PrinterUtils.TakePrinterOwnership(queue.Name);
                                securityDesc = MyDLPEP.PrinterUtils.GetPrinterSecurityDescriptor(queue.Name);

                                //save original permissions
                                if (!printerPermissions.ContainsKey(mydlpQueue.Name))
                                {
                                    printerPermissions.Add(mydlpQueue.Name, securityDesc);
                                }

                                if (Environment.UserInteractive)
                                {
                                    MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(queue.Name, BuiltinAdminsPrinterSecurityDescriptor);
                                    MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(mydlpQueue.Name, BuiltinAdminsPrinterSecurityDescriptor + authUserPrint);
                                }
                                else
                                {
                                    MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(queue.Name, SystemPrinterSecurityDescriptor);
                                    MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(mydlpQueue.Name, SystemPrinterSecurityDescriptor + authUserPrint);
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
                                    + " error:" + e);
                            }
                        }
                    }
                }

                //On any change set default printer to MyDLP counterpart
                if (change)
                {
                    //Give windows time to update default printer
                    Thread.Sleep(2000);
                    UpdateDefaultPrinter();
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("InstallSecurePrinters failed: " + e);
            }
            finally
            {
                Logger.GetInstance().Debug("InstallSecurePrinters ended");
                lock (this)
                {
                    changingLocalPrinters = false;
                }
            }
        }

        private void RemoveLocalSecurePrinters()
        {
            lock (this)
            {
                if (changingLocalPrinters)
                {
                    return;
                }
                else changingLocalPrinters = true;
            }
            bool defaultPrinterReverted = false;
            try
            {
                Logger.GetInstance().Debug("RemoveSecurePrinters started");

                MyDLPEP.SessionUtils.ImpersonateActiveUser();
                String defaultPrinterName = MyDLPEP.PrinterUtils.GetDefaultSystemPrinter();
                MyDLPEP.SessionUtils.StopImpersonation();

                Logger.GetInstance().Info("Default printer :" + defaultPrinterName);

                LocalPrintServer pServer = new LocalPrintServer();
                PrintQueueCollection queueCollection = pServer.GetPrintQueues();
                foreach (PrintQueue queue in queueCollection)
                {
                    //Logger.GetInstance().Debug("Process printer queue: " + queue.Name
                    //   + " driver: " + queue.QueueDriver.Name + " port: " + queue.QueuePort.Name);

                    if (queue.QueueDriver.Name == MyDLPDriver ||
                        queue.QueuePort.Name == "MyDLP")
                    {
                        Logger.GetInstance().Debug("A secure printer found removing " + queue.Name);

                        if (queue.Name.StartsWith(PrinterPrefix))
                        {
                            PrintQueueCollection qCollection = pServer.GetPrintQueues();
                            foreach (PrintQueue q in qCollection)
                            {
                                //Find mathing non secure printer
                                if (GetSecurePrinterName(q.Name) == queue.Name)
                                {
                                    if (printerPermissions.ContainsKey(queue.Name))
                                    {
                                        MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(
                                           q.Name,
                                           printerPermissions[queue.Name]);
                                    }
                                    else
                                    {
                                        //fallback make printer usable
                                        MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(
                                            q.Name,
                                            BuiltinAdminsPrinterSecurityDescriptor + authUserPrint);
                                    }

                                    if (queue.Name == defaultPrinterName)
                                    {
                                        //Revert default printer                    
                                        MyDLPEP.SessionUtils.ImpersonateActiveUser();
                                        if (MyDLPEP.PrinterUtils.SetDefaultSystemPrinter(q.Name))
                                        {
                                            defaultPrinterReverted = true;
                                            MyDLPEP.SessionUtils.StopImpersonation();
                                            Logger.GetInstance().Debug("Set default printer as:" + q.Name + "successfully");
                                        }
                                        else
                                        {
                                            MyDLPEP.SessionUtils.StopImpersonation();
                                            Logger.GetInstance().Error("Failed to set default printer as:" + q.Name);
                                        }
                                    }

                                }
                            }
                        }

                        MyDLPEP.PrinterUtils.TakePrinterOwnership(queue.Name);
                        if (Environment.UserInteractive)
                        {
                            MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(queue.Name, BuiltinAdminsPrinterSecurityDescriptor);
                        }
                        else
                        {
                            MyDLPEP.PrinterUtils.SetPrinterSecurityDescriptor(queue.Name, SystemPrinterSecurityDescriptor);
                        }
                        MyDLPEP.PrinterUtils.RemovePrinter(queue.Name);

                    }
                    else
                    {
                        //Logger.GetInstance().Debug("A non-secure printer found " + queue.Name);

                        if (spooledNativePrinters.Contains(queue.Name))
                        {
                            Logger.GetInstance().Debug("Reenabling spooling " + queue.Name);
                            MyDLPEP.PrinterUtils.SetPrinterSpoolMode(queue.Name, true);
                            spooledNativePrinters.Remove(queue.Name);
                        }
                    }
                }

                if (!defaultPrinterReverted)
                {
                    //Error occured set a valid default printer from available printers
                    MyDLPEP.SessionUtils.ImpersonateActiveUser();

                    MyDLPEP.PrinterUtils.SetDefaultSystemPrinter("");
                    MyDLPEP.SessionUtils.StopImpersonation();
                }

            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Remove secure printers failed: " + e);
            }
            finally
            {
                Logger.GetInstance().Debug("RemoveSecurePrinters ended");
                lock (this)
                {
                    changingLocalPrinters = false;
                }
            }
        }

        private bool CheckAndInstallPortMonitor()
        {
            if (MyDLPEP.PrinterUtils.CheckIfPrinterPortExists("MyDLP"))
            {
                return true;
            }
            else
            {
                try
                {
                    Logger.GetInstance().Debug("No MyDLP Port found, installing");
                    SvcController.StopService("Spooler", 10000);
                    //this is ugly but necessary
                    Thread.Sleep(5000);

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

                    SvcController.StartService("Spooler", 5000);
                    Thread.Sleep(1000);
                    Logger.GetInstance().Debug("MyDLP Port installation complete");
                    return true;

                }
                catch (Exception e)
                {
                    Logger.GetInstance().Error("Error in install port monitor:" + e);
                    return false;
                }
            }
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
                Logger.GetInstance().Error("Error install printer driver:" + e);
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

        private void UpdateDefaultPrinter()
        {
            MyDLPEP.SessionUtils.ImpersonateActiveUser();
            String defaultPrinterName = MyDLPEP.PrinterUtils.GetDefaultSystemPrinter();

            if (!defaultPrinterName.StartsWith(PrinterPrefix))
            {
                String newDefaultPrinterName = GetSecurePrinterName(defaultPrinterName);

                if (MyDLPEP.PrinterUtils.SetDefaultSystemPrinter(newDefaultPrinterName))
                {
                    MyDLPEP.SessionUtils.StopImpersonation();
                    Logger.GetInstance().Debug("Change default printer from:" + defaultPrinterName + " to:" + newDefaultPrinterName + "successfully");
                }
                else
                {
                    MyDLPEP.SessionUtils.StopImpersonation();
                    Logger.GetInstance().Error("Failed to change default printer from:" + defaultPrinterName + " to:" + newDefaultPrinterName);
                }
            }
            else
            {
                MyDLPEP.SessionUtils.StopImpersonation();
                Logger.GetInstance().Debug("Default printer is already a secure printer:" + defaultPrinterName);
            }
        }

        private PrinterController()
        {
            spooledNativePrinters = new ArrayList();
            printerConnections = new ArrayList();
            watcher = null;
            MyDLPEP.PrinterUtils.LocalPrinterRemoveHandler = new MyDLPEP.PrinterUtils.LocalPrinterRemoveHandlerDeleagate(LocalPrinterRemoveHandler);
            MyDLPEP.PrinterUtils.LocalPrinterAddHandler = new MyDLPEP.PrinterUtils.LocalPrinterAddHandlerDeleagate(LocalPrinterAddHandler);
        }

        public class PrinterConnection
        {

            public PrinterConnection(String name, String server)
            {
                this.name = name;
                this.server = server;
                this.secureName = GetSecurePrinterName(name + "_on_" + server);
            }

            public override bool Equals(object obj)
            {
                if (!(obj is PrinterConnection)) return false;

                PrinterConnection pObj = (PrinterConnection)obj;

                return (this.name == pObj.name) && (this.server == pObj.server);
            }

            public override int GetHashCode()
            {
                return (this.name + this.server).GetHashCode();
            }

            public override string ToString()
            {
                return this.name + this.server;
            }

            public String name;
            public String server;
            public String secureName;
        }
    }
}
