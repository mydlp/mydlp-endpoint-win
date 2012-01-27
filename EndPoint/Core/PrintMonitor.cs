using System;
using System.Collections;
using System.ComponentModel;
using System.Data;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.ServiceProcess;
using Microsoft.Win32.SafeHandles;
using Microsoft.Win32;
using System.Globalization;
using System.IO;

namespace MyDLP.EndPoint.Core.Print
{
    public class PrintMonitor
    {
        PRINTER_NOTIFY_OPTIONS notifyOptions = new PRINTER_NOTIFY_OPTIONS();

        static int statusCount = 0;
        const int splFileNameLength = 5;
        const int INVALID_HANDLE_VALUE = -1;
        const uint PRINTER_NOTIFY_INFO_DISCARDED = 1;
        static Mutex spoolMutex; 

        [DllImport("winspool.drv", CharSet = CharSet.Auto, SetLastError = true)]
        public static extern bool EnumPrinters(PrinterEnumFlags Flags, string Name, uint Level, IntPtr pPrinterEnum, uint cbBuf, ref uint pcbNeeded, ref uint pcReturned);

        [DllImport("winspool.drv", EntryPoint = "SetJobA")]
        static extern int SetJobA(IntPtr hPrinter, int JobId, int Level, IntPtr pJob, int Command_Renamed);

        [DllImport("winspool.drv")]
        public static extern bool GetJobW(IntPtr hPrinter, int JobId, int Level, IntPtr pJobInfo, uint cdBuf, ref uint pcbNeeded);

       /* [DllImport("winspool.drv", EntryPoint = "OpenPrinterA", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter(String pPrinterName, out IntPtr phPrinter, Int32 pDefault);*/

        [DllImport("winspool.drv", EntryPoint = "ClosePrinter", SetLastError = true, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.drv", EntryPoint = "FindFirstPrinterChangeNotification", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern IntPtr FindFirstPrinterChangeNotification(
            [InAttribute()] IntPtr hPrinter,
            [InAttribute()] Int32 fwFlags,
            [InAttribute()] Int32 fwOptions,
            [InAttribute(), MarshalAs(UnmanagedType.LPStruct)] PRINTER_NOTIFY_OPTIONS pPrinterNotifyOptions);

        [DllImport("winspool.drv", EntryPoint = "FindNextPrinterChangeNotification", SetLastError = true, CharSet = CharSet.Ansi, ExactSpelling = false, CallingConvention = CallingConvention.StdCall)]
        public static extern bool FindNextPrinterChangeNotification(
            [InAttribute()] IntPtr hChangeObject,
            [OutAttribute()] out Int32 pdwChange,
            [InAttribute(), MarshalAs(UnmanagedType.LPStruct)] PRINTER_NOTIFY_OPTIONS pPrinterNotifyOptions,
            [OutAttribute()] out IntPtr lppPrinterNotifyInfo);

        [DllImport("winspool.drv")]
        static extern bool FreePrinterNotifyInfo(IntPtr pPrinterNotifyInfo);

        [DllImport("winspool.drv")]
        static extern bool FindClosePrinterChangeNotification(IntPtr hChange);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern uint WaitForSingleObject(IntPtr hHandle, uint dwMilliseconds);

         [DllImport("winspool.drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        public static extern bool OpenPrinter(String pPrinterName, ref IntPtr phPrinter, PRINTER_DEFAULTS pDefaults);

   
        [DllImport("winspool.Drv", EntryPoint="GetPrinterW", SetLastError=true, CharSet=CharSet.Auto,ExactSpelling=true, CallingConvention=CallingConvention.StdCall)]
        private static extern bool GetPrinter(IntPtr hPrinter, Int32 dwLevel,IntPtr pPrinter, Int32 dwBuf, out Int32 dwNeeded);


        [DllImport("winspool.Drv", EntryPoint = "SetPrinterW", SetLastError = true, CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.StdCall)]
        private static extern bool SetPrinter(IntPtr hPrinter, int Level, ref PRINTER_INFO_2 pinfo, int Command);

        [DllImport("Winspool.drv", SetLastError = true, EntryPoint = "EnumJobsW")]
        public static extern bool EnumJobs(
           IntPtr hPrinter,                    // handle to printer object
           UInt32 FirstJob,                // index of first job
           UInt32 NoJobs,                // number of jobs to enumerate
           UInt32 Level,                    // information level
           IntPtr pJob,                        // job information buffer
           UInt32 cbBuf,                    // size of job information buffer
           out UInt32 pcbNeeded,    // bytes received or required
           out UInt32 pcReturned    // number of jobs received
        );
        
        const uint PRINTER_ATTRIBUTE_QUEUED           = 1;
        const uint PRINTER_ATTRIBUTE_DIRECT = 2;

        const uint PRINTER_ATTRIBUTE_HIDDEN = 32;
        const uint PRINTER_ATTRIBUTE_KEEPPRINTEDJOBS  = 256;
        const uint PRINTER_ATTRIBUTE_RAW_ONLY  = 4096;

        const uint INFINITE = 0xFFFFFFFF;
        const uint WAIT_ABANDONED = 0x00000080;
        const uint WAIT_OBJECT_0 = 0x00000000;
        const uint WAIT_TIMEOUT = 0x00000102;

        private const int ERROR_INSUFFICIENT_BUFFER = 122;
        private static string spoolPath;
 
        IntPtr printerChangeHandle;
        IntPtr printerHandle;

        RegisteredWaitHandle printerChangeNotificationHandle;
        ManualResetEvent printerWaitHandle;

        private static ArrayList printerList;

        private static System.Timers.Timer cleanupTimer;


        public static bool InitMonitors()
        {                
            //top prevent race conds in spool file modification
            spoolMutex = new Mutex();

            Logger.GetInstance().Info("PrintMonitor.InitMonitors start");

            try
            {

                printerList = new ArrayList();
                RegistryKey printKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Control\Print\Printers");
                if (printKey == null)
                {
                    Logger.GetInstance().Error("Unable to get DefaultSpoolDirectory form registry");
                    throw new Exception("Unable to get DefaultSpoolDirectory form registry");             
                }                

                object regPath = printKey.GetValue("DefaultSpoolDirectory");

                if (regPath == null)
                {
                    Logger.GetInstance().Error("Unable to get spool path form registry");
                    throw new Exception("Unable to get spool path form registry");                  
                }

                spoolPath = regPath.ToString();

                PRINTER_INFO_2[] printers = PrintMonitor.enumPrinters(PrinterEnumFlags.PRINTER_ENUM_NAME);
                if (printers != null)
                {
                    ParameterizedThreadStart starter;
                    for (int i = 0; i < printers.Length; i++)
                    {
                        SetPrinterOptions(printers[i].pPrinterName, true);
                        printerList.Add(printers[i].pPrinterName);
                        starter = new ParameterizedThreadStart(new PrintMonitor().StartListener);
                        new Thread(starter).Start(printers[i].pPrinterName);
                    }
                }
                //initialize cleanup timer
                cleanupTimer = new System.Timers.Timer();
                //t.Interval = 15 * 60000; //15 minutes
                cleanupTimer.Interval = 7 * 60000; //7 minutes
                cleanupTimer.Enabled = true;
                cleanupTimer.Elapsed += new System.Timers.ElapsedEventHandler(TimedCleanUp);
                Logger.GetInstance().Debug("PrintMonitor.InitMonitors ends");
                return true;
                
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("PrintMonitor.InitMonitors: " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        public static bool StopMonitors() 
        {
            try
            {
                Logger.GetInstance().Debug("PrintMonitor.StopMonitors started");
                PRINTER_INFO_2[] printers = PrintMonitor.enumPrinters(PrinterEnumFlags.PRINTER_ENUM_NAME);
                if (printers != null)
                {
                    for (int i = 0; i < printers.Length; i++)
                    {
                        SetPrinterOptions(printers[i].pPrinterName, false);
                    }                   
                }

                cleanupTimer.Stop();
                cleanupTimer.Close();
                Logger.GetInstance().Debug("PrintMonitor.StopMonitors stopped");
                return true;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("PrintMonitor.StopMonitors: " + ex.Message + " " + ex.StackTrace);
                return false;
            }
        }

        public static PRINTER_INFO_2[] enumPrinters(PrinterEnumFlags Flags)
        {
            try
            {
                Logger.GetInstance().Debug("PrintMonitor.enumPrinters started");
                uint cbNeeded = 0;
                uint cReturned = 0;
                if (EnumPrinters(Flags, null, 2, IntPtr.Zero, 0, ref cbNeeded, ref cReturned))
                {
                    return null;
                }
                int lastWin32Error = Marshal.GetLastWin32Error();

                if (lastWin32Error == ERROR_INSUFFICIENT_BUFFER)
                {
                    IntPtr pAddr = Marshal.AllocHGlobal((int)cbNeeded);
                    if (EnumPrinters(Flags, null, 2, pAddr, cbNeeded, ref cbNeeded, ref cReturned))
                    {
                        PRINTER_INFO_2[] printerInfo2 = new PRINTER_INFO_2[cReturned];
                        int offset = pAddr.ToInt32();
                        Type type = typeof(PRINTER_INFO_2);
                        int increment = Marshal.SizeOf(type);
                        for (int i = 0; i < cReturned; i++)
                        {
                            printerInfo2[i] = (PRINTER_INFO_2)Marshal.PtrToStructure(new IntPtr(offset), type);
                            offset += increment;
                        }
                        Marshal.FreeHGlobal(pAddr);
                        Logger.GetInstance().Debug("PrintMonitor.enumPrinters stopped");
                        return printerInfo2;
                    }
                    lastWin32Error = Marshal.GetLastWin32Error();
                    Logger.GetInstance().Error("PrintMonitor.enumPrinters: " + lastWin32Error.ToString());
                    return null;
                }
                return null;
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("PrintMonitor.enumPrinters: " + ex.Message + " " + ex.StackTrace);
                return null;
            }          
        }

        public static System.Collections.ArrayList EnumJob(IntPtr hPrinter, uint jobCount)
        {
            try
            {
                Logger.GetInstance().Debug("PrintMonitor.EnumJob started");
                System.Collections.ArrayList arrList = new System.Collections.ArrayList();
                uint cbNeeded = 0;
                uint cReturned = 0;
                if (EnumJobs(hPrinter, (uint)0, jobCount, (uint)1, IntPtr.Zero, 0, out cbNeeded, out cReturned))
                {
                    return new ArrayList();
                }
                int lastWin32Error = Marshal.GetLastWin32Error();

                if (lastWin32Error == ERROR_INSUFFICIENT_BUFFER)
                {
                    IntPtr pJob = Marshal.AllocHGlobal((int)cbNeeded);
                    if (EnumJobs(hPrinter, (uint)0, jobCount, (uint)1, pJob, cbNeeded, out cbNeeded, out cReturned))
                    {
                        JOB_INFO_1[] jobInfo1 = new JOB_INFO_1[cReturned];
                        IntPtr[] pJobInfo1 = new IntPtr[cReturned];
                        int offset = pJob.ToInt32();
                        Type type = typeof(JOB_INFO_1);
                        int increment = Marshal.SizeOf(type);
                        for (int i = 0; i < cReturned; i++)
                        {
                            arrList.Add(Marshal.PtrToStructure(new IntPtr(offset), type));
                            arrList.Add(new IntPtr(offset));
                            offset += increment;
                        }
                        return arrList;
                    }
                    lastWin32Error = Marshal.GetLastWin32Error();
                    Logger.GetInstance().Error("PrintMonitor.enumJobs: " + lastWin32Error.ToString());
                    return new ArrayList();
                }
                return new ArrayList();              
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("PrintMonitor.enumJobs: " + ex.Message + " " + ex.StackTrace);
                return new ArrayList();
            }        
        }

        private static void Failsafe(IntPtr hPrinter, System.Collections.ArrayList jobList)
        {
            Logger.GetInstance().Debug("PrintMonitor.Failsafe started");
            try {
                if (hPrinter != null && jobList.Count != 0)
                {
                    Logger.GetInstance().Debug("PrintMonitor.Failsafe There are " + (jobList.Count / 2).ToString() + " jobs on the list");
                    for (int i = 0; i < jobList.Count / 2; i++)
                    {
                        string idString = ((int)((JOB_INFO_1)jobList[i * 2]).JobId).ToString();
                        for (int j = idString.Length; j < splFileNameLength; j++)
                        {
                            idString = "0" + idString;
                        }

                        string splPath = spoolPath + @"\" + idString + ".SPL";

                        try
                        {
                            bool delJob = false;

                            if (((JOB_INFO_1)jobList[i * 2]).pStatus != null)
                            {
                                string stat = ((JOB_INFO_1)jobList[i * 2]).pStatus.ToLower();
                                if (stat.Contains("printed") || stat.Contains("completed"))
                                {
                                    delJob = true;
                                }
                            }

                            if ((((JOB_INFO_1)jobList[i * 2]).Status & ((uint)JOBSTATUS.JOB_STATUS_PRINTED | (uint)JOBSTATUS.JOB_STATUS_COMPLETE)) > 0)
                            {
                                delJob = true;
                            }

                            if (delJob && File.Exists(splPath))
                            {
                                Logger.GetInstance().Debug("PrintMonitor.Failsafe Deleting " + splPath + " - job: " 
                                    + ((JOB_INFO_1)jobList[i * 2]).pDocument + "on printer: " + ((JOB_INFO_1)jobList[i * 2]).pPrinterName);
                                File.Delete(splPath);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.GetInstance().Error("PrintMonitor.Failsafe: " + ex.Message + " " + ex.StackTrace);
                        }
                    }
                    // restart spooler service
                    // we don't do this for now
                    // context related to printers wiped out if spooler service restarted
                    /*
                    PrintMonitor.StopMonitors();
                    try
                    {
                        ServiceController sc = new ServiceController("Spooler");
                        sc.Stop();
                        sc.WaitForStatus(ServiceControllerStatus.Stopped, System.TimeSpan.FromMinutes(4));
                        sc.Start();
                    }
                    catch (Exception ex)
                    {
                        Logger.GetInstance().Error("PrintMonitor.Failsafe: " + ex.Message + " " + ex.StackTrace);
                    }
                    PrintMonitor.InitMonitors();
                    */
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("PrintMonitor.Failsafe: " + ex.Message + " " + ex.StackTrace);
            }

            Logger.GetInstance().Debug("PrintMonitor.Failsafe stopped");
        }

        public static void TimedCleanUp(object source, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                Logger.GetInstance().Debug("PrintMonitor.TimedCleanUp started");
                IntPtr pBytes = IntPtr.Zero;
                PRINTER_DEFAULTS pDefault = new PRINTER_DEFAULTS();
                //printer_all_access 0x000F000C
                pDefault.DesiredAccess = 0x000F000C;
                pDefault.pDataType = null;
                pDefault.pDevMode = IntPtr.Zero;
                IntPtr hPrinter = IntPtr.Zero;

                PRINTER_INFO_2[] printers = PrintMonitor.enumPrinters(PrinterEnumFlags.PRINTER_ENUM_NAME);
                if (printers != null)
                {
                    for (int i = 0; i < printers.Length; i++)
                    {
                        ParameterizedThreadStart starter;

                        //check new printers
                        if (!printerList.Contains(printers[i].pPrinterName))
                        {
                            SetPrinterOptions(printers[i].pPrinterName, true);
                            printerList.Add(printers[i].pPrinterName);
                            starter = new ParameterizedThreadStart(new PrintMonitor().StartListener);
                            new Thread(starter).Start(printers[i].pPrinterName);
                        }

                        //delete job;
                        if(OpenPrinter(printers[i].pPrinterName, ref hPrinter, pDefault)) {
                            System.Collections.ArrayList arrList = EnumJob(hPrinter, printers[i].cJobs);

                            //failsafe loop
                            int printedJobCount = 0;
                            for (int j = 0; j < (arrList.Count / 2); j++)
                            {
                                if (((JOB_INFO_1)arrList[j * 2]).pStatus != null)
                                {
                                    string stat = ((JOB_INFO_1)arrList[j * 2]).pStatus.ToLower();
                                    if (stat.Contains("printed") || stat.Contains("completed"))
                                    {
                                        printedJobCount++;
                                    }
                                }

                                if ((((JOB_INFO_1)arrList[j * 2]).Status & ((uint)JOBSTATUS.JOB_STATUS_PRINTED | (uint)JOBSTATUS.JOB_STATUS_COMPLETE)) > 0)
                                {
                                    printedJobCount++;
                                }
                            }


                            Logger.GetInstance().Debug("Printed Job Count: " + printedJobCount);
                            // run failsafe if printed job count is larger than 25
                            // assumption: in 7 minutes there can not be 25 jobs printed
                            if (printedJobCount >= 25)
                            {
                                Failsafe(hPrinter, arrList);
                            } 
                            else
                            {
                                //delete loop
                                for (int j = 0; j < (arrList.Count / 2); j++)
                                {
                                    bool delJob = false;

                                    if (((JOB_INFO_1)arrList[j * 2]).pStatus != null)
                                    {
                                        string stat = ((JOB_INFO_1)arrList[j * 2]).pStatus.ToLower();
                                        if (stat.Contains("printed") || stat.Contains("completed"))
                                        {
                                            delJob = true;
                                        }
                                    }

                                    if ((((JOB_INFO_1)arrList[j * 2]).Status & ((uint)JOBSTATUS.JOB_STATUS_PRINTED | (uint)JOBSTATUS.JOB_STATUS_COMPLETE)) > 0)
                                    {
                                        delJob = true;
                                    }

                                    if (delJob)
                                    {
                                        Logger.GetInstance().Debug("PrintMonitor.TimedCleanUp delete job doc:" + ((JOB_INFO_1)arrList[j * 2]).pDocument
                                           + " user: " + ((JOB_INFO_1)arrList[j * 2]).pUserName + " printer: " + ((JOB_INFO_1)arrList[j * 2]).pPrinterName);
                                   
                                        int retCode = SetJobA(hPrinter, (int)((JOB_INFO_1)arrList[j * 2]).JobId, 1, (IntPtr)arrList[(j * 2) + 1], 5);
                                        if (retCode == 0)
                                            Logger.GetInstance().Debug("FAILED TO DELETE - doc:" + ((JOB_INFO_1)arrList[j * 2]).pDocument);
                                    }
                                }
                            }
                        }
                    }
                    Logger.GetInstance().Debug("PrintMonitor.TimedCleanUp stopped");
                }
                else
                {
                    Logger.GetInstance().Debug("PrintMonitor.TimedCleanUp stopped");
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error(ex.Message + " " + ex.StackTrace);
            }
        }

        public void StartListener(object printerName)
        {
            try
            {
                Logger.GetInstance().Debug("PrintMonitor.StartListener started for printer: " + printerName);
                printerWaitHandle = new ManualResetEvent(false);

                PRINTER_DEFAULTS pDefault = new PRINTER_DEFAULTS();
                pDefault.DesiredAccess = 0;
                pDefault.pDataType = null;
                pDefault.pDevMode = IntPtr.Zero;

                bool success = OpenPrinter((string)printerName, ref printerHandle, pDefault);
                if (success)
                {
                    printerWaitHandle = new ManualResetEvent(false);
                    printerChangeHandle = FindFirstPrinterChangeNotification(printerHandle, (int)PRINTER_CHANGES.PRINTER_CHANGE_DELETE_JOB, 0, notifyOptions);
                    printerWaitHandle.SafeWaitHandle = new SafeWaitHandle(printerChangeHandle, true);

                    printerChangeNotificationHandle = ThreadPool.RegisterWaitForSingleObject(printerWaitHandle, new WaitOrTimerCallback(PrinterNotifyWaitCallback), printerWaitHandle, -1, true);
                    Logger.GetInstance().Debug("PrintMonitor.StartListener stopped for printer: " + printerName);
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error(ex.Message + " " + ex.StackTrace);
            }
        }
        
        public void PrinterNotifyWaitCallback(object state, bool timedOut)
        {
            // BUG: if we Thread.Sleep here for the amount of time it takes to spool the print job, we only get the right notification for number of pages.

            try
            {
                int changeReason = 0;
                IntPtr pNotifyInfo = IntPtr.Zero;

                bool nxt = FindNextPrinterChangeNotification(printerChangeHandle, out changeReason, null, out pNotifyInfo);
                if (!nxt)
                {
                    int lastError = Marshal.GetLastWin32Error();
                }
                else
                {
                    try
                    {
                        if ((int)pNotifyInfo != 0)
                        {
                            PRINTER_NOTIFY_INFO info = (PRINTER_NOTIFY_INFO)Marshal.PtrToStructure(pNotifyInfo, typeof(PRINTER_NOTIFY_INFO));
                            if ((info.Flags & PRINTER_NOTIFY_INFO_DISCARDED) == PRINTER_NOTIFY_INFO_DISCARDED)
                            {

                            }

                            int pData = (int)pNotifyInfo + Marshal.SizeOf(typeof(PRINTER_NOTIFY_INFO));
                            PRINTER_NOTIFY_INFO_DATA[] data = new PRINTER_NOTIFY_INFO_DATA[info.Count];
                            for (uint i = 0; i < info.Count; i++)
                            {
                                data[i] = (PRINTER_NOTIFY_INFO_DATA)Marshal.PtrToStructure((IntPtr)pData, typeof(PRINTER_NOTIFY_INFO_DATA));
                                pData += Marshal.SizeOf(typeof(PRINTER_NOTIFY_INFO_DATA));
                            }

                            for (int i = 0; i < data.Length; i++)
                            {
                                string buffer = Marshal.PtrToStringAnsi(data[i].NotifyData.Data.pBuf);

                                switch (data[i].Type)
                                {
                                    case (ushort)PRINTERNOTIFICATIONTYPES.JOB_NOTIFY_TYPE:

                                        switch (data[i].Field)
                                        {
                                            case (ushort)PRINTERJOBNOTIFICATIONTYPES.JOB_NOTIFY_FIELD_STATUS:
                                                JOBSTATUS jobStatus = (JOBSTATUS)Enum.Parse(typeof(JOBSTATUS), data[i].NotifyData.Data.cbBuf.ToString());
                                                JOB_INFO_1 jobInfo = new JOB_INFO_1();
                                                uint pcbNeeded = 0;
                                                IntPtr pJobInfo = IntPtr.Zero;
                                                bool getJobSuccessful = GetJobW(printerHandle, (int)data[i].Id, 1, IntPtr.Zero, 0, ref pcbNeeded);
                                                if (pcbNeeded > 0)
                                                {
                                                    pJobInfo = Marshal.AllocHGlobal((int)pcbNeeded);

                                                    getJobSuccessful = GetJobW(printerHandle, (int)data[i].Id, 1, pJobInfo, pcbNeeded, ref pcbNeeded);
                                                    if (getJobSuccessful)
                                                    {
                                                        jobInfo = (JOB_INFO_1)Marshal.PtrToStructure(pJobInfo, typeof(JOB_INFO_1));
                                                    }
                                                }

                                                if (jobStatus.ToString() == "JOB_STATUS_PRINTING")
                                                {
                                                    try
                                                    {
                                                        /* Critical section */
                                                        spoolMutex.WaitOne(120000, false);

                                                        Logger.GetInstance().Debug("PrintMonitor.PrinterNotifyWaitCallback " + jobStatus.ToString() + "captured");
                                                        string idString = data[i].Id.ToString();
                                                        for (int j = data[i].Id.ToString().Length; j < splFileNameLength; j++)
                                                        {
                                                            idString = "0" + idString;
                                                        }

                                                        string splPath = spoolPath + @"\" + idString + ".SPL";

                                                        //Determine ile type safely
                                                        int tryCount = 0;
                                                        SpoolFile sp = null;
                                                        for (; tryCount < 25; )
                                                        {
                                                            try
                                                            {
                                                                sp = new SpoolFile(splPath);
                                                                break;
                                                            }
                                                            catch (Exception)
                                                            {
                                                                Thread.Sleep(1000);
                                                            }
                                                        }

                                                        if (sp == null)
                                                        {
                                                            sp = new SpoolFile();
                                                        }

                                                        //Get job information
                                                        string documentName = jobInfo.pDocument;
                                                        string userName = jobInfo.pUserName;
                                                        string printerName = jobInfo.pPrinterName;
                                                        uint pageCount = jobInfo.TotalPages;

                                                        string safeDocName = "unknown";

                                                        try
                                                        {
                                                            safeDocName = Path.GetFileName(documentName);
                                                        }
                                                        catch (Exception ex)
                                                        {
                                                            Logger.GetInstance().Debug("Unable to get safe document name of: " + documentName);
                                                            Logger.GetInstance().Debug(ex.Message + ex.StackTrace);
                                                        }

                                                        Logger.GetInstance().Debug("safedocName " + safeDocName);
                                                        string typedPath = Path.GetTempFileName() + sp.Extension;

                                                        Logger.GetInstance().Debug("typedPath :" + typedPath);

                                                        //Copy spool file to temp safely
                                                        tryCount = 0;
                                                        for (; tryCount < 25; )
                                                        {
                                                            try
                                                            {
                                                                File.Copy(splPath, typedPath, true);
                                                                break;
                                                            }
                                                            catch (Exception)
                                                            {
                                                                Thread.Sleep(1000);
                                                            }
                                                        }

                                                       // string zipPath = Path.GetTempPath() + safeDocName + sp.Extension + ".zip";
                                                       // Logger.GetInstance().Debug("zipPath :" + zipPath);
                                                       // Compress(typedPath, zipPath);

                                                        //Send zip file archive to server    
                                                        Logger.GetInstance().Debug("PrintMonitor.PrinterNotifyWaitCallback PushPrinterCopyLog " +
                                                            "pagecount :" + pageCount + " userName:" + userName +
                                                            " documentName: " + documentName + " printerName: " + printerName +
                                                            " typedPath:" + typedPath);
                                                        SeapClient.NotitfyPrintOperation((int)pageCount, userName, documentName, printerName, typedPath);

                                                        /*delete tempfiles                                                     
                                                        try
                                                        {
                                                            //File.Delete(typedPath);
                                                        }
                                                        catch (IOException)
                                                        {
                                                            Thread.Sleep(3000);
                                                            try
                                                            {
                                                                File.Delete(typedPath);
                                                            }
                                                            catch (IOException ex)
                                                            {
                                                                Logger.GetInstance().Debug(ex.Message + ex.StackTrace);
                                                                Logger.GetInstance().Debug("Unable to delete temp file: " + typedPath);
                                                            }
                                                        }
                                                        */
                                                    }
                                                    catch (Exception ex)
                                                    {
                                                        Logger.GetInstance().Error(ex.Message + " " + ex.StackTrace);
                                                    }
                                                    finally
                                                    {
                                                        spoolMutex.ReleaseMutex();
                                                        /* End of critical section */
                                                    }
                                                    statusCount++;
                                                }
                                                break;

                                            default:
                                                break;
                                        }
                                        break;

                                    default:
                                        break;
                                }
                            }
                        }
                    }
                    finally
                    {
                        if (((int)pNotifyInfo) != 0)
                        {
                            FreePrinterNotifyInfo(pNotifyInfo);
                        }
                    }

                    printerWaitHandle.Reset();
                    printerChangeNotificationHandle = ThreadPool.RegisterWaitForSingleObject(printerWaitHandle, new WaitOrTimerCallback(PrinterNotifyWaitCallback), printerWaitHandle, -1, true);
                }
            }
            catch (Exception ex) 
            {
                Logger.GetInstance().Error("PPrintNotifyCallback failed: " + ex.Message + " " + ex.StackTrace);
            }
        }
              
        /*
         * This function sets keep printed documents and start printing 
         * after last page is spooled options on start. 
         *         
         * On stop it disables keep printed documents since print queue
         *  may get full without mydlp running.
         */
        public static bool SetPrinterOptions(string printerName,bool start)
        {
            Logger.GetInstance().Debug("PrintMonitor.SetPrinterOptions started finished: " + printerName + " bool = " + start.ToString());
            try
            {               
                string ReturnValue = "";
                Int32 level = 2;
                Int32 Needed = 0;
                IntPtr hPrinter = new IntPtr(0);
                IntPtr pPrinter = new IntPtr(0);
                PRINTER_INFO_2 pi = new PRINTER_INFO_2();
                IntPtr pBytes = IntPtr.Zero;
                PRINTER_DEFAULTS pDefault = new PRINTER_DEFAULTS();
                //printer_all_access 0x000F000C
                pDefault.DesiredAccess = 0x000F000C;
                pDefault.pDataType=null;
                pDefault.pDevMode=IntPtr.Zero;
               
                // Open the printer.
                if (OpenPrinter(printerName, ref hPrinter, pDefault))
                {
                    // Get printer level page size needed - first call
                    GetPrinter(hPrinter, level, pPrinter, 0, out Needed);
                    if (Needed > 0)
                    {
                        // Allocate needed memory       2
                        pBytes = Marshal.AllocHGlobal((int)Needed);

                        // Get printer level data - second call
                        if (GetPrinter(hPrinter, level, pBytes, Needed, out Needed))
                        {
                            // Convert printer data block into class structure          
                            pi = (PRINTER_INFO_2)Marshal.PtrToStructure(pBytes, typeof(PRINTER_INFO_2));

                            ReturnValue = pi.pPrinterName; // get printer driver name                   

                            if (start)
                            {               
                                pi.Attributes = pi.Attributes | PRINTER_ATTRIBUTE_KEEPPRINTEDJOBS | PRINTER_ATTRIBUTE_QUEUED;
                                
                                // Disabling advanced printing features (EMF)
                                pi.Attributes = pi.Attributes | PRINTER_ATTRIBUTE_RAW_ONLY;
                                pi.Attributes = pi.Attributes & ~PRINTER_ATTRIBUTE_DIRECT;
                                pi.Attributes = pi.Attributes & ~PRINTER_ATTRIBUTE_HIDDEN;
                            }
                            else 
                            {
                                pi.Attributes = pi.Attributes & ~PRINTER_ATTRIBUTE_KEEPPRINTEDJOBS;
                            }
                                                        
                            bool x = SetPrinter(hPrinter, level, ref pi, 0);
                            if (!x)
                            {                               
                                int error = Marshal.GetLastWin32Error();
                                Logger.GetInstance().Error("GetLastWin32Error():" + error);
                                return false;
                            }
                        }                     
                    }                   
                }
                Logger.GetInstance().Debug("PrintMonitor.SetPrinterOptions stopped"); 
            }
            catch (Exception ex) 
            {
                Logger.GetInstance().Error(ex.Message + ex.StackTrace);
                return false;
            }

            return true;
        }        

        public static void PrintJobStatus(uint flag)
        {
            if ((flag & (uint)JOBSTATUS.JOB_STATUS_BLOCKED_DEVQ) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_BLOCKED_DEVQ");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_COMPLETE) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_COMPLETE");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_DELETED) > 0) 
                Console.WriteLine("JOBSTATUS.JOB_STATUS_DELETED");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_DELETING) > 0) 
                Console.WriteLine("JOBSTATUS.JOB_STATUS_DELETING");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_ERROR) > 0) 
                Console.WriteLine("JOBSTATUS.JOB_STATUS_ERROR");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_OFFLINE) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_OFFLINE");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_PAPEROUT) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_PAPEROUT");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_PAUSED) > 0) 
                Console.WriteLine("JOBSTATUS.JOB_STATUS_PAUSED");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_PRINTED) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_PRINTED");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_PRINTING) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_PRINTING");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_RENDERING_LOCALLY) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_RENDERING_LOCALLY");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_RESTART) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_RESTART");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_RETAINED) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_RETAINED");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_SPOOLING) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_SPOOLING");

            if ((flag & (uint)JOBSTATUS.JOB_STATUS_USER_INTERVENTION) > 0)
                Console.WriteLine("JOBSTATUS.JOB_STATUS_USER_INTERVENTION");
        }

        public static void PrintPrinterStatus(uint flag) 
        {
            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_BUSY) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_BUSY");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_DOOR_OPEN) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_DOOR_OPEN");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_ERROR) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_ERROR");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_INITIALIZING) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_INITIALIZING");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_IO_ACTIVE) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_IO_ACTIVE");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_MANUAL_FEED) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_MANUAL_FEED");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_NO_TONER) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_NO_TONER");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_NOT_AVAILABLE) > 0)
                Console.WriteLine(PRINTERSTATUS.PRINTER_STATUS_NOT_AVAILABLE);

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_OFFLINE) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_OFFLINE");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_OUT_OF_MEMORY) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_OUT_OF_MEMORY");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_OUTPUT_BIN_FULL) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_OUTPUT_BIN_FULL");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_PAGE_PUNT) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_PAGE_PUNT");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_PAPER_JAM) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_PAPER_JAM");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_PAPER_OUT) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_PAPER_OUT");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_PAPER_PROBLEM) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_PAPER_PROBLEM");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_PAUSED) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_PAUSED");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_PENDING_DELETION) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_PENDING_DELETION");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_POWER_SAVE) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_POWER_SAVE");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_PRINTING) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_PRINTING");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_PROCESSING) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_PROCESSING");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_SERVER_UNKNOWN) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_SERVER_UNKNOWN");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_TONER_LOW) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_TONER_LOW");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_USER_INTERVENTION) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_USER_INTERVENTION");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_WAITING) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_WAITING");

            if ((flag & (uint)PRINTERSTATUS.PRINTER_STATUS_WARMING_UP) > 0)
                Console.WriteLine("PRINTERSTATUS.PRINTER_STATUS_WARMING_UP");
        }

        public static void PrintPrinterAttributes(uint flag)
        {
            if ((flag & (uint)PRINTERATTRIBUTES.DEFAULT) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.DEFAULT");

            if ((flag & (uint)PRINTERATTRIBUTES.DIRECT) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.DIRECT");

            if ((flag & (uint)PRINTERATTRIBUTES.DO_COMPLETE_FIRST) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.DO_COMPLETE_FIRST");

            if ((flag & (uint)PRINTERATTRIBUTES.ENABLE_BIDI) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.ENABLE_BIDI");

            if ((flag & (uint)PRINTERATTRIBUTES.ENABLE_DEVQ) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.ENABLE_DEVQ");

            if ((flag & (uint)PRINTERATTRIBUTES.HIDDEN) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.HIDDEN");

            if ((flag & (uint)PRINTERATTRIBUTES.KEEPPRINTEDJOBS) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.KEEPPRINTEDJOBS");

            if ((flag & (uint)PRINTERATTRIBUTES.LOCAL) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.LOCAL");

            if ((flag & (uint)PRINTERATTRIBUTES.NETWORK) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.NETWORK");

            if ((flag & (uint)PRINTERATTRIBUTES.PUBLISHED) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.PUBLISHED");

            if ((flag & (uint)PRINTERATTRIBUTES.QUEUED) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.QUEUED");

            if ((flag & (uint)PRINTERATTRIBUTES.RAW_ONLY) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.RAW_ONLY");

            if ((flag & (uint)PRINTERATTRIBUTES.SHARED) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.SHARED");

            if ((flag & (uint)PRINTERATTRIBUTES.WORK_OFFLINE) > 0)
                Console.WriteLine("PRINTERATTRIBUTES.WORK_OFFLINE");
        }
    }
     
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public class PRINTER_DEFAULTS
    {
        public string pDataType;
        public IntPtr pDevMode;
        public int DesiredAccess;
    }

}