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
using System.Linq;
using System.Text;
using System.IO;
using MyDLP.EndPoint.Core;
using System.Threading;
using System.Printing;
using System.Management;
using System.Collections;
using System.Security.Principal;
using System.Net;

namespace MyDLP.EndPoint.Service
{
    public class TempSpooler
    {
        static string localJobId;
        static string sharedJobId;
        static string localXpsPath;
        static string sharedXpsPath;
        static string metaPath;
        static string localPrinterName;
        static Thread remotePrintingListenerThread;
        static string sharedPrinterName;
        static ArrayList sharedLocalPrinters;
        static bool started = false;
        static bool listeningForRemotePrinting = false;
        static bool hasSharedPrinter = false;
        static volatile bool checkingPrinters = false;
        //Used by engine for remote printing
        static int IECPPort = 9100;

        static ManagementEventWatcher shareWatcher = null;
        static FileSystemWatcher spoolWatcher;
        //static String shareSpoolPerm = "O:BAG:DUD:PAI(A;OICI;0x100116;;;AU)(A;OICI;FA;;;SY)";
        //static String shareSpoolPermInteractive = "O:BAG:DUD:PAI(A;OICI;FA;;;BA)(A;OICI;0x100116;;;AU)(A;OICI;FA;;;SY)";

        public static bool Start()
        {
            spoolWatcher = new FileSystemWatcher();
            //clean spool files create empty directory
            if (System.IO.Directory.Exists(Configuration.PrintSpoolPath))
            {
                try
                {
                    System.IO.Directory.Delete(Configuration.PrintSpoolPath, true);
                }

                catch (System.IO.IOException e)
                {
                    Logger.GetInstance().Error(e);
                }
            }
            try
            {
                System.IO.Directory.CreateDirectory(Configuration.PrintSpoolPath);
            }
            catch (System.IO.IOException e)
            {
                Logger.GetInstance().Error(e);
            }
            try
            {
                spoolWatcher.Path = Configuration.PrintSpoolPath;
                spoolWatcher.IncludeSubdirectories = true;
                spoolWatcher.Filter = "*.meta";
                //spoolWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                // | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                spoolWatcher.Created += new FileSystemEventHandler(OnCreate);
                //spoolWatcher.Changed += new FileSystemEventHandler(OnCreate);
                spoolWatcher.EnableRaisingEvents = true;
                spoolWatcher.Error += new ErrorEventHandler(SpoolWatcherError);
                Logger.GetInstance().Debug("TempSpooler watch started on path:" + spoolWatcher.Path);
                StartShareEventListener();
                started = true;
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("Unable to start TempSpooler:" + ex);
                StopShareEventListener();
                started = false;
                return false;
            }
        }

        public static bool Stop()
        {
            try
            {
                if (started)
                {
                    StopShareEventListener();
                    StopSharedPrinterServerListener();
                    spoolWatcher.EnableRaisingEvents = false;
                    started = false;
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("TempSpooler watch cannot be stopped on path:" + spoolWatcher.Path + " " + ex);
            }
            return true;
        }

        private static void StartShareEventListener()
        {
            Logger.GetInstance().Debug("StartShareEventListener");
            CheckForSharedPrinters();
            WqlEventQuery q = new WqlEventQuery();
            q.EventClassName = "__InstanceOperationEvent";
            q.WithinInterval = new TimeSpan(0, 0, 60); // query interval
            q.Condition = @"TargetInstance ISA 'Win32_Share'";
            shareWatcher = new ManagementEventWatcher(q);
            shareWatcher.EventArrived += new EventArrivedEventHandler(HandleEvent);
            shareWatcher.Start();
            Logger.GetInstance().Debug("Start StartShareEventListener Finished");
        }

        private static void HandleEvent(object sender, EventArrivedEventArgs e)
        {
            CheckForSharedPrinters();
        }

        private static void CheckForSharedPrinters()
        {
            if (checkingPrinters)
            {
                return;
            }
            else
            {
                try
                {
                    checkingPrinters = true;
                    Logger.GetInstance().Debug("CheckForSharedPrinters");
                    PrintServer pServer = new LocalPrintServer();
                    sharedLocalPrinters = new ArrayList();

                    foreach (PrintQueue queue in pServer.GetPrintQueues())
                    {
                        if (queue.IsShared)
                        {
                            sharedLocalPrinters.Add(queue.Name);
                        }
                    }
                    if (sharedLocalPrinters.Count > 0)
                    {
                        if (!hasSharedPrinter)
                        {
                            //has shared printer
                            hasSharedPrinter = true;
                            StartSharedPrinterServerListener();
                        }
                    }
                    else
                    {
                        if (hasSharedPrinter)
                        {
                            //has no shared printer
                            hasSharedPrinter = false;
                            StopSharedPrinterServerListener();
                        }
                    }
                    checkingPrinters = false;
                }
                catch (Exception ex)
                {
                    checkingPrinters = false;
                    Logger.GetInstance().Error("CheckForSharedPrinters exception:" + ex);
                }
            }
        }

        private static string WinXPIECPFirewallEnable =
            "netsh firewall add portopening TCP " + IECPPort + " \"MyDLP IECP\"";

        private static string WinXPIECPFirewallDisable =
            "netsh firewall delete portopening protocol=TCP port=" + IECPPort;

        private static string Win7IECPFirewallEnable = 
            "netsh advfirewall firewall add rule name=\"MyDLP IECP\" " + 
            "dir=in action=allow protocol=TCP localport=" + IECPPort;

        private static string Win7IECPFirewallDisable = 
            "netsh advfirewall firewall delete rule name=\"MyDLP IECP\" "+
            "protocol=TCP localport=" + IECPPort;

        private static void StartSharedPrinterServerListener()
        {
            if (listeningForRemotePrinting == true)
                return;

            if (Configuration.OsVersion.XP == Configuration.GetOs())
            {
                ProcessControl.ExecuteCommandAsync(WinXPIECPFirewallEnable, "Firewall");
            }
            else 
            {
                ProcessControl.ExecuteCommandAsync(Win7IECPFirewallEnable, "Firewall");
            }
               
            listeningForRemotePrinting = true;
            remotePrintingListenerThread = new Thread(SharedPrinterListenerWorker);
            remotePrintingListenerThread.Start();
        }

        private static void SharedPrinterListenerWorker()
        {
            while (listeningForRemotePrinting)
            {
                SeapClient.ListenRemotePrintBlocking(OnRemotePrintingRequest);
            }
        }

        private static void StopSharedPrinterServerListener()
        {
            listeningForRemotePrinting = false;
            if (Configuration.OsVersion.XP == Configuration.GetOs())
            {
                ProcessControl.ExecuteCommandAsync(WinXPIECPFirewallDisable, "Firewall");
            }
            else
            {
                ProcessControl.ExecuteCommandAsync(Win7IECPFirewallDisable, "Firewall");
            }
        }

        private static void StopShareEventListener()
        {
            Logger.GetInstance().Debug("StopShareEventListener");
            if (shareWatcher != null)
                shareWatcher.Stop();
            shareWatcher = null;
            //remove share if any
            //has no shared printer;

        }


        private static void OnRemotePrintingRequest(String jobId, String printJobPath, String printerName)
        {
            Logger.GetInstance().Debug("Printing shared job : " + printJobPath + " with " + printerName);
            sharedPrinterName = printerName;
            sharedXpsPath = printJobPath;
            sharedJobId = jobId;
            try
            {
                var thread = new Thread(WorkerMethodShared);
                thread.SetApartmentState(ApartmentState.STA);
                thread.Start();
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error(ex);
                if (ex.InnerException != null)
                {
                    Logger.GetInstance().Error(ex.InnerException.Message + ex.InnerException.StackTrace);
                }
            }
        }

        private static void OnCreate(object source, FileSystemEventArgs e)
        {
            Logger.GetInstance().Debug("Local Printing: " + e.FullPath);
            String printerDir;
            String docName;
            metaPath = e.FullPath;
            localJobId = Path.GetFileNameWithoutExtension(metaPath);
            printerDir = Path.GetDirectoryName(metaPath);
            docName = "";
            localPrinterName = printerDir.Substring(
               printerDir.LastIndexOf(Path.DirectorySeparatorChar) + 1,
               printerDir.Length - (printerDir.LastIndexOf(Path.DirectorySeparatorChar) + 1));
            localXpsPath = metaPath.Replace(".meta", ".xps");
            try
            {
                if (WaitForFile(metaPath))
                {
                    string[] metaText = System.IO.File.ReadAllLines(metaPath, System.Text.Encoding.Unicode);
                    if (metaText.Length > 0)
                    {
                        docName = metaText[0].Replace("docname:", "").Trim();
                    }
                }
                else
                {
                    Logger.GetInstance().Error("OnCreate: WaitForFile failed for meta file");
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("OnCreate:" + ex);
            }

            if (SeapClient.NotitfyPrintOperation(docName, localPrinterName, localXpsPath) == FileOperation.Action.ALLOW)
            {
                try
                {
                    var thread = new Thread(WorkerMethodLocal);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();
                }
                catch (Exception ex)
                {
                    Logger.GetInstance().Error(ex);
                    if (ex.InnerException != null)
                    {
                        Logger.GetInstance().Error(ex.InnerException.Message + ex.InnerException.StackTrace);
                    }
                }
            }
            else
            {
                Logger.GetInstance().Debug("blocked print:" + docName + " " + localXpsPath);
                //Do nothing block item  
            }
        }

        private static void WorkerMethodLocal(object state)
        {
            Logger.GetInstance().Debug("Started Printing for MyDLP printer: " + localPrinterName);
            PrintQueue pQueue = null;
            try
            {
                PrinterController controller = PrinterController.getInstance();
                if (controller.IsPrinterConnection(localPrinterName))
                {
                    //It will be printerd remotely
                    //It is a network printer connection on local computer
                    PrinterController.PrinterConnection connection = controller.GetPrinterConnection(localPrinterName);
                    IPAddress[] addresslist = Dns.GetHostAddresses(connection.server);
                    Logger.GetInstance().Debug("Initiating remote print remoteprinter: " +
                        connection.name + " on server:" + connection.server + "of File:" + localXpsPath);
                    SeapClient.InitiateRemotePrint(localJobId, connection.name, addresslist[0].ToString(), localXpsPath);
                }
                else
                {
                    //It is a local printer
                    LocalPrintServer pServer = new LocalPrintServer();
                    PrintQueueCollection qCollection = pServer.GetPrintQueues();
                    foreach (PrintQueue q in qCollection)
                    {
                        //Find mathing non secure printer
                        if (PrinterController.GetSecurePrinterName(q.Name) == localPrinterName)
                        {
                            pQueue = q;
                        }
                    }
                    if (pQueue == null)
                        throw new Exception("Unable to find a matching non secure printer for mydlp printer: " + localPrinterName);
                    Logger.GetInstance().Debug("Adding print job on real printer: " + pQueue.Name +
                        ", path:" + localXpsPath + ", jobID:" + localJobId);
                    if (WaitForFile(localXpsPath))
                    {
                        pQueue.AddJob(localJobId, localXpsPath, false);
                        Thread.Sleep(1000);
                        Logger.GetInstance().Debug("Removing:" + localXpsPath);
                        File.Delete(localXpsPath);
                        File.Delete(metaPath);
                        Logger.GetInstance().Debug("Finished Printing");
                    }
                    else
                    {
                        Logger.GetInstance().Debug("WorkerMethodLocal WaitForFile failed for xps file");
                    }
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("WorkerMethod Exception" + e);
                if (e.InnerException != null)
                {
                    Logger.GetInstance().Error(e.InnerException.Message + e.InnerException.StackTrace);
                }
            }
        }

        private static void WorkerMethodShared(object state)
        {
            Logger.GetInstance().Debug("Started Printing for MyDLP printer: " + sharedPrinterName);
            PrintQueue pQueue = null;
            try
            {
                PrinterController controller = PrinterController.getInstance();
                LocalPrintServer pServer = new LocalPrintServer();
                PrintQueueCollection qCollection = pServer.GetPrintQueues();
                foreach (PrintQueue q in qCollection)
                {
                    //Find mathing non secure printer
                    if (q.Name == sharedPrinterName)
                    {
                        pQueue = q;
                    }
                }
                if (pQueue == null)
                    throw new Exception("Unable to find a matching non secure printer for mydlp printer: " + sharedPrinterName);
                Logger.GetInstance().Debug("Adding print job on real printer: " + pQueue.Name +
                    ", path:" + sharedXpsPath + ", sharedJobID:" + sharedJobId);
                if (WaitForFile(sharedXpsPath))
                {
                    pQueue.AddJob(sharedJobId, sharedXpsPath, false);
                    Thread.Sleep(1000);
                    Logger.GetInstance().Debug("Removing:" + sharedXpsPath);
                    File.Delete(sharedXpsPath);
                    Logger.GetInstance().Debug("Finished Printing");
                }
                else
                {
                    Logger.GetInstance().Debug("WorkerMethodShared WaitForFile failed for xps file");
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("WorkerMethodShared Exception" + e);
                if (e.InnerException != null)
                {
                    Logger.GetInstance().Error(e.InnerException.Message + e.InnerException.StackTrace);
                }
            }
        }

        private static void SpoolWatcherError(Object sender, ErrorEventArgs e)
        {
            try
            {
                Logger.GetInstance().Error("Filewatcher error: " + e.GetException().Message + " restartting TempSpooler");
                Stop();
                Start();
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("SpoolWatcherError Error:" + ex);
            }
        }

        static bool WaitForFile(string fullPath)
        {
            int numTries = 0;
            while (true)
            {
                ++numTries;
                try
                {
                    using (FileStream fs = new FileStream(fullPath,
                        FileMode.Open, FileAccess.ReadWrite,
                        FileShare.None, 100))
                    {
                        fs.ReadByte();
                        break;
                    }
                }
                catch (Exception ex)
                {
                    if (numTries > 10)
                    {
                        Logger.GetInstance().Error("WaitForFile giving up after 10 tries path: " + fullPath);
                        return false;
                    }

                    System.Threading.Thread.Sleep(500);
                }
            }
            return true;
        }
    }
}
