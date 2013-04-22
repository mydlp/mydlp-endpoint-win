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
using System.Text;
using MyDLP.EndPoint.Core;
using System.Diagnostics;
using System.Timers;
using System.ServiceProcess;
using Microsoft.Win32;
using System.Threading;

namespace MyDLP.EndPoint.Service
{
    public class MainController
    {
        System.Timers.Timer watchdogTimer;
        System.Timers.Timer confTimer = null;
        public static EventLog serviceLogger;

        int watchdogTimerPeriod = 120000;
        int confCheckTimerPeriod = 30000;

        public static MainController GetInstance()
        {
            if (controller == null)
            {
                controller = new MainController();
            }
            return controller;
        }

        public static void SetServiceLogger(EventLog eventLog)
        {
            serviceLogger = eventLog;
        }

        public void Start()
        {
            Thread thread = new Thread(StartBackground);
            Logger.GetInstance().Info("Starting mydlpepwin service");
            thread.Start();

        }

        public void StartBackground()
        {

            lock (MainController.GetInstance())
            {
                //notify logger that we are in main service
                Logger.GetInstance().InitializeMainLogger(serviceLogger);
                SvcController.StopMyDLP = new SvcController.StopMyDLPDelegate(Stop);

                //Keep watchdog tied up during debugging
                if (System.Environment.UserInteractive == false)
                {

                    ServiceController service = new ServiceController("mydlpepwatchdog");
                    try
                    {
                        if (!service.Status.Equals(ServiceControllerStatus.Running) && !service.Status.Equals(ServiceControllerStatus.StartPending))
                        {
                            Logger.GetInstance().Info("Starting mydlpepwatchdog at start up");
                            SvcController.StartServiceNonBlocking("mydlpepwatchdog", 10000);
                            Logger.GetInstance().Info("Starting mydlpepwatchdog at start up finished");
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.GetInstance().Error("Unable to start watchdog" + e);
                    }
                    //enable watchdog check

                    Logger.GetInstance().Info("Watchdog check enabled");
                    watchdogTimer = new System.Timers.Timer(watchdogTimerPeriod);
                    watchdogTimer.Elapsed += new ElapsedEventHandler(OnTimedWatchdogEvent);
                    watchdogTimer.Enabled = true;
                }



                if (Configuration.GetAppConf() == false)
                {
                    Logger.GetInstance().Error("Unable to get configuration exiting!");
                    //Environment.Exit(1);
                }
                else
                {
                    //start backend engine

                    Configuration.GetUserConf();
                    Configuration.StartTime = DateTime.Now;

                    SessionManager.Start();

                    Engine.GetPhysicalMemory = new Engine.GetPhysicalMemoryDelegate(GetPhysicalMemory);
                    Engine.Start();
                    Configuration.SetPids();

                    Logger.GetInstance().Debug("mydlpepwin tries to install mydlpmf");
                    MyDLPEP.MiniFilterController.GetInstance().Start();

                    MyDLPEP.FilterListener.getInstance().StartListener();
                    Logger.GetInstance().Info("mydlpepwin service started");

                    bool testSuccess = false;
                    for (int i = 0; i < 10; i++)
                    {
                        testSuccess = SeapClient.SeapConnectionTest();
                        if (testSuccess)
                            break;
                        Logger.GetInstance().Debug("Seap connection test attempt:" + i);
                        System.Threading.Thread.Sleep(3000);
                    }

                    if (!testSuccess)
                    {
                        Logger.GetInstance().Error("Seap connection test failed");
                        Stop();
                    }

                    if (Configuration.BlockScreenShot)
                    {
                        ScreenShotContoller.Start();
                    }

                    SessionServer.Start();

                    if (Configuration.PrinterMonitor)
                    {
                        Service.PrinterController.getInstance().Start();
                    }

                    if (Configuration.RemovableStorageEncryption)
                    {
                        DiskCryptor.StartDcrypt();
                    }

                    if (Configuration.UsbSerialAccessControl)
                    {
                        Core.USBController.Activate();
                        Core.USBController.GetUSBStorages();
                    }
                }

                //initialize configuration timer
                Logger.GetInstance().Info("Configuration check enabled");
                confTimer = new System.Timers.Timer(confCheckTimerPeriod);
                confTimer.Elapsed += new ElapsedEventHandler(OnTimedConfCheckEvent);
                confTimer.Enabled = true;

                Logger.GetInstance().Info("mydlpepwin service started");

            }
        }

        public void Stop()
        {
            lock (MainController.GetInstance())
            {
                if (confTimer != null)
                    confTimer.Enabled = false;
                              
                if (Configuration.BlockScreenShot)
                {
                    ScreenShotContoller.Stop();
                }

                MyDLPEP.MiniFilterController.GetInstance().Stop();

                Engine.Stop();

                SessionServer.Stop();

                if (Configuration.UsbSerialAccessControl)
                {
                    Core.USBController.Deactive();
                }

                if (Configuration.PrinterMonitor)
                {
                    Service.PrinterController.getInstance().Stop();
                }

                if (Configuration.RemovableStorageEncryption)
                {
                    DiskCryptor.StopDcrypt();
                }

                Logger.GetInstance().Info("mydlpepwin service stopped");
            }
        }

        private void OnTimedWatchdogEvent(object source, ElapsedEventArgs e)
        {
            Logger.GetInstance().CheckLogLimit();

            ServiceController service = new ServiceController("mydlpepwatchdog");
            try
            {
                if (!service.Status.Equals(ServiceControllerStatus.Running) && !service.Status.Equals(ServiceControllerStatus.StartPending))
                {
                    Logger.GetInstance().Info("Watchdog isdead!, starting mydlpepwatchdog");
                    service.Start();
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("Unable to start watchdog" + ex.Message);
            }
        }

        private void OnTimedConfCheckEvent(object source, ElapsedEventArgs e)
        {
            bool oldUSBSerialAC = Configuration.UsbSerialAccessControl;
            bool oldPrinterMonitor = Configuration.PrinterMonitor;
            bool oldArchiveInbound = Configuration.ArchiveInbound;
            bool oldBlockScreenShot = Configuration.BlockScreenShot;
            bool oldRemStorEncryption = Configuration.RemovableStorageEncryption;
            bool oldHasEncryptionKey = Configuration.HasEncryptionKey;
            String oldPrinterPrefix = Configuration.PrinterPrefix;

            if (SeapClient.HasNewConfiguration())
            {
                Logger.GetInstance().Info("New configuration notified.");

                Configuration.GetUserConf();

                if (Configuration.UsbSerialAccessControl && !oldUSBSerialAC)
                {
                    Core.USBController.Activate();
                }
                else if (!Configuration.UsbSerialAccessControl && oldUSBSerialAC)
                {
                    Core.USBController.Deactive();
                }                

                if (Configuration.UsbSerialAccessControl)
                {
                    USBController.InvalidateCache();
                    Core.USBController.GetUSBStorages();
                }
                                
                if (Configuration.PrinterMonitor && !oldPrinterMonitor)
                {
                    Service.PrinterController.getInstance().Start();
                }
                else if (!Configuration.PrinterMonitor && oldPrinterMonitor)
                {
                    Service.PrinterController.getInstance().Stop();
                }
                else if (Configuration.PrinterMonitor && (Configuration.PrinterPrefix != oldPrinterPrefix)) 
                {
                    Service.PrinterController.getInstance().Stop();
                    Service.PrinterController.getInstance().Start();                
                }

                if (Configuration.RemovableStorageEncryption && !oldRemStorEncryption)
                {
                    DiskCryptor.StartDcrypt();
                }
                else if (!Configuration.RemovableStorageEncryption && oldRemStorEncryption)
                {
                    DiskCryptor.StopDcrypt();
                }

                if (oldArchiveInbound != Configuration.ArchiveInbound
                    || oldUSBSerialAC != Configuration.UsbSerialAccessControl)
                {
                    Logger.GetInstance().Debug("New mydlpmf configuration");
                    MyDLPEP.MiniFilterController.GetInstance().Stop();
                    MyDLPEP.MiniFilterController.GetInstance().Start();
                    MyDLPEP.FilterListener.getInstance().StartListener();
                }

                if (oldBlockScreenShot)
                {
                    ScreenShotContoller.Stop();
                }

                if (Configuration.BlockScreenShot)
                {
                    ScreenShotContoller.Start();
                }
            }

            if (Configuration.RemovableStorageEncryption)
            {
                if (oldHasEncryptionKey)
                {
                    if (!SeapClient.HasKeyfile())
                    {
                        Configuration.HasEncryptionKey = false;
                        DiskCryptor.AfterKeyLose();
                    }
                }
                else
                {
                    if (SeapClient.HasKeyfile())
                    {
                        Configuration.HasEncryptionKey = true;
                        DiskCryptor.AfterKeyReceive();
                    }
                }
            }
        }

        public static int GetPhysicalMemory()
        {
            return MyDLPEP.SessionUtils.GetPhysicalMemory();
        }

        private static MainController controller = null;
        private MainController() { }
    }
}
