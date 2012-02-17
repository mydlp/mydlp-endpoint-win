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

namespace MyDLP.EndPoint.Service
{
    public class MainController
    {
        Engine engine;
        Timer watchdogTimer;
        Timer confTimer;
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
            //notify logger that we are in main service
            Logger.GetInstance().InitializeMainLogger(serviceLogger);

            Logger.GetInstance().Info("Starting mydlpepwin service");

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
                engine = new Engine();
                engine.Start();
                Configuration.setPids();

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
                    System.Threading.Thread.Sleep(1000);
                }

                if (!testSuccess)
                {
                    Logger.GetInstance().Error("Seap connection test failed");
                }

                if (Configuration.PrinterMonitor)
                {
                    Core.PrinterController.Start();
                }

                if (Configuration.UsbSerialAccessControl)
                {
                    Core.USBController.AddUSBHandler();
                    Core.USBController.GetUSBStorages();
                }
            }

            //Keep watchdog tied up during debugging
            if (System.Environment.UserInteractive == false)
            {
                //enable watchdog check

                Logger.GetInstance().Info("Watchdog check enabled");
                watchdogTimer = new Timer(watchdogTimerPeriod);
                watchdogTimer.Elapsed += new ElapsedEventHandler(OnTimedWatchdogEvent);
                watchdogTimer.Enabled = true;
            }


            //initialize configuration timer

            Logger.GetInstance().Info("Configuration check enabled");
            confTimer = new Timer(confCheckTimerPeriod);
            confTimer.Elapsed += new ElapsedEventHandler(OnTimedConfCheckEvent);
            confTimer.Enabled = true;

        }

        public void Stop()
        {
            MyDLPEP.MiniFilterController.GetInstance().Stop();
            engine.Stop();

            if (Configuration.UsbSerialAccessControl)
            {
                Core.USBController.RemoveUSBHandler();
            }

            if (Configuration.PrinterMonitor)
            {
                Core.PrinterController.Stop();
            }

            Logger.GetInstance().Info("mydlpepwin service stopped");
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
            catch
            {
                //todo:
            }
        }

        private void OnTimedConfCheckEvent(object source, ElapsedEventArgs e)
        {
            bool oldUSBSerialAC = Configuration.UsbSerialAccessControl;
            bool oldPrinterMonitor = Configuration.PrinterMonitor;
            bool oldArchiveInbound = Configuration.ArchiveInbound;

            if (SeapClient.HasNewConfiguration())
            {
                Logger.GetInstance().Info("New configuration notified.");
                Configuration.GetUserConf();

                if (Configuration.UsbSerialAccessControl && !oldUSBSerialAC)
                {
                    Core.USBController.AddUSBHandler();
                    Core.USBController.GetUSBStorages();
                }
                else if (!Configuration.UsbSerialAccessControl && oldUSBSerialAC)
                {
                    Core.USBController.RemoveUSBHandler();
                }

                if (Configuration.PrinterMonitor && !oldPrinterMonitor)
                {
                    Core.PrinterController.Start();
                }
                else if (!Configuration.PrinterMonitor && oldPrinterMonitor)
                {
                    Core.PrinterController.Stop();
                }


                if (oldArchiveInbound != Configuration.ArchiveInbound
                    || oldUSBSerialAC != Configuration.UsbSerialAccessControl)
                {
                    Logger.GetInstance().Debug("New mydlpmf configuration");
                    MyDLPEP.MiniFilterController.GetInstance().Stop();
                    MyDLPEP.MiniFilterController.GetInstance().Start();
                    MyDLPEP.FilterListener.getInstance().StartListener();
                }
            }
        }

        private static MainController controller = null;
        private MainController() { }
    }
}
