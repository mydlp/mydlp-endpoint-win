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

        static FileSystemWatcher spoolWatcher;

        public static bool Start()
        {
            spoolWatcher = new FileSystemWatcher();
            if (Environment.UserInteractive)
            {
                spoolWatcher.Path = Path.Combine("C:\\windows\\temp", "mydlp");
            }
            else
            {
                spoolWatcher.Path = Path.Combine(Path.GetTempPath(), "mydlp");
            }

            Logger.GetInstance().Debug("TempSpooler watch started on path:" + spoolWatcher.Path);
            spoolWatcher.IncludeSubdirectories = true;
            spoolWatcher.Filter = "*.meta";
            spoolWatcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
           | NotifyFilters.FileName | NotifyFilters.DirectoryName;
            spoolWatcher.Created += new FileSystemEventHandler(OnCreate);
            spoolWatcher.EnableRaisingEvents = true;
            return true;
        }

        public static bool Stop()
        {
            spoolWatcher.EnableRaisingEvents = false;
            return false;
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
                string[] metaText = System.IO.File.ReadAllLines(metaPath);

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
            try
            {
                LocalPrintServer pServer = new LocalPrintServer();
                PrintQueue pQueue = pServer.GetPrintQueue(printerName.Replace("(MyDLP)", "").Trim());
                
                pQueue.AddJob(jobId, xpsPath, false);
                File.Delete(xpsPath);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e.Message + e.StackTrace);
            }          
        }        
    }
}
