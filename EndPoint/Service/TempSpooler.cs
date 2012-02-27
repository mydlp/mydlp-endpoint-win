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

            Logger.GetInstance().Debug("TempSpooler watch started on path:" + spoolWatcher.Path);
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
                string[] metaText = System.IO.File.ReadAllLines(metaPath,System.Text.Encoding.Unicode);

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
            PrintQueue pQueue;
            try
            {                
                LocalPrintServer pServer = new LocalPrintServer();

                try
                {
                    //get print queue by name
                    pQueue = pServer.GetPrintQueue(printerName.Replace("(MyDLP)", "").Trim());
                    pQueue.AddJob(jobId, xpsPath, false);
                    File.Delete(xpsPath);
                }
                catch
                {
                    //printer does not exist or has "_" for offending chars
                    foreach (PrintQueue queue in pServer.GetPrintQueues())
                    {
                        if (
                            (queue.Name
                            .Replace(":", "_")
                            .Replace("\\", "_")
                            .Replace("/", "_")
                            .Replace("|", "_")
                            .Replace("<", "_")
                            .Replace(">", "_")
                            .Replace("*", "_")
                            ) == printerName.Replace("(MyDLP)","").Trim()) 
                        {
                            try
                            {
                                pQueue = pServer.GetPrintQueue(queue.Name);
                                pQueue.AddJob(jobId, xpsPath, false);
                                File.Delete(xpsPath);
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
                    }
                }               
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
