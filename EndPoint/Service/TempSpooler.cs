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

namespace MyDLP.EndPoint.Service
{
    public class TempSpooler
    {
        static string jobId;
        static string xpsPath;
        static string printerName;
        static bool started = false;

        static FileSystemWatcher spoolWatcher;

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
                    Logger.GetInstance().Error(e.Message);
                }
            }

            try
            {
                System.IO.Directory.CreateDirectory(Configuration.PrintSpoolPath);
            }

            catch (System.IO.IOException e)
            {
                Logger.GetInstance().Error(e.Message);
            }

            spoolWatcher.Path = Configuration.PrintSpoolPath;
            spoolWatcher.IncludeSubdirectories = true;
            spoolWatcher.Filter = "*.meta";
            spoolWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
           | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            //spoolWatcher.Created += new FileSystemEventHandler(OnCreate);
            spoolWatcher.Changed += new FileSystemEventHandler(OnCreate);
            spoolWatcher.EnableRaisingEvents = true;

            //todo check this
            Logger.GetInstance().Debug("TempSpooler watch started on path:" + spoolWatcher.Path);
            started = true;
            return true;
        }

        public static bool Stop()
        {
            try
            {
                if (started)
                {
                    spoolWatcher.EnableRaisingEvents = false;
                    started = false;
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("TempSpooler watch cannot be stopped on path:" + spoolWatcher.Path + " " + ex.StackTrace + " " + ex.Message);
            }
            return true;
        }

        private static void OnCreate(object source, FileSystemEventArgs e)
        {
            Logger.GetInstance().Debug("File: " + e.FullPath + " " + e.ChangeType);
            String metaPath;
            String printerDir;
            String docName;

            metaPath = e.FullPath;
            jobId = Path.GetFileNameWithoutExtension(metaPath);

            printerDir = Path.GetDirectoryName(metaPath);
            docName = "";

            printerName = printerDir.Substring(
               printerDir.LastIndexOf(Path.DirectorySeparatorChar) + 1,
               printerDir.Length - (printerDir.LastIndexOf(Path.DirectorySeparatorChar) + 1));

            xpsPath = metaPath.Replace(".meta", ".xps");


            try
            {
                string[] metaText = System.IO.File.ReadAllLines(metaPath, System.Text.Encoding.Unicode);

                if (metaText.Length > 0)
                {
                    docName = metaText[0].Replace("docname:", "").Trim();
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error(ex.Message);
            }

            if (SeapClient.NotitfyPrintOperation(docName, printerName, xpsPath) == FileOperation.Action.ALLOW)
            {
                try
                {
                    var thread = new Thread(WorkerMethod);
                    thread.SetApartmentState(ApartmentState.STA);
                    thread.Start();


                }
                catch (Exception ex)
                {
                    Logger.GetInstance().Error(ex.Message + ex.StackTrace);
                    if (ex.InnerException != null)
                    {
                        Logger.GetInstance().Error(ex.InnerException.Message + ex.InnerException.StackTrace);
                    }
                }
            }
            else
            {
                Logger.GetInstance().Debug("blocked print:" + docName + " " + xpsPath);
                //Do nothing block item  
            }
            File.Delete(metaPath);

        }

        private static void WorkerMethod(object state)
        {
            Logger.GetInstance().Debug("Started Printing for MyDLP printer: " + printerName);
            PrintQueue pQueue = null;
            try
            {
                LocalPrintServer pServer = new LocalPrintServer();

                PrintQueueCollection qCollection = pServer.GetPrintQueues();
                foreach (PrintQueue q in qCollection)
                {
                    //Find mathicng non secure printer
                    if (PrinterController.GetSecurePrinterName(q.Name) == printerName)
                    {
                        pQueue = q;
                    }
                }
                if (pQueue == null)
                    throw new Exception("Unable to find a matching non secure printer for mydlp printer: " + printerName);
                Logger.GetInstance().Debug("Adding print job on real printer: " + pQueue.Name +
                    ", path:" + xpsPath + ", jobID:" + jobId);
                pQueue.AddJob(jobId, xpsPath, false);
                Thread.Sleep(1000);
                Logger.GetInstance().Debug("Removing:" + xpsPath);
                File.Delete(xpsPath);
                Logger.GetInstance().Debug("Finished Printing");

            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e.Message + e.StackTrace);
                if (e.InnerException != null)
                {
                    Logger.GetInstance().Error(e.InnerException.Message + e.InnerException.StackTrace);
                }
            }
        }
    }
}
