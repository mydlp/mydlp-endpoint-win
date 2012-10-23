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
    public class WatchdogController
    {
        Timer watchdogTimer;
        public static EventLog serviceLogger;

        int watchdogTimerPeriod = 180000;


        public static WatchdogController GetInstance()
        {
            if (controller == null)
            {
                controller = new WatchdogController();
            }
            return controller;
        }

        public static void SetServiceLogger(EventLog eventLog)
        {
            serviceLogger = eventLog;
        }

        public void Start()
        {
            //notify logger that we are in watchdog service
            Logger.GetInstance().InitializeWatchdogLogger(serviceLogger);
            if (Configuration.GetAppConf() == false)
            {
                //to defer error message
                System.Threading.Thread.Sleep(2000);
                serviceLogger.WriteEntry("Unable to get mydlp registry configuration, mydlpepwatchdog service stopped", EventLogEntryType.Error);
                Logger.GetInstance().Error("Unable to get mydlp registry configuration, mydlpepwatchdog service stopped");
                Environment.Exit(1);
            }

            Configuration.GetUserConf();

            watchdogTimer = new Timer(watchdogTimerPeriod);
            watchdogTimer.Elapsed += new ElapsedEventHandler(OnTimedWatchdogEvent);
            watchdogTimer.Enabled = true;

            Logger.GetInstance().Info("mydlpepwatchdog service started");
        }

        private void OnTimedWatchdogEvent(object source, ElapsedEventArgs e)
        {
            Logger.GetInstance().CheckLogLimit();

            Logger.GetInstance().Debug("OnTimedWatchdogEvent");

            //Check upto date pids in file
            Configuration.SetPids();

            bool error = false;

            if (Configuration.ErlPid != 0)
            {
                try
                {
                    Process p = Process.GetProcessById(Configuration.ErlPid);
                    if (p.HasExited)
                    {
                        Logger.GetInstance().Error("service has exited");
                        error = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.GetInstance().Error("erl service exception " + Configuration.ErlPid + " " + ex.Message);
                    error = true;
                }
            }
            else
            {
                if (!CheckProcRunningByName("erl") || !CheckProcRunningByName("werl"))
                {
                    error = true;
                    Logger.GetInstance().Error("no erl or werl named process");
                }
            }

            if (!CheckProcRunningByName("java"))
            {
                error = true;
                Logger.GetInstance().Error("no java process");
            }

            ServiceController service = new ServiceController("mydlpepwin");
            try
            {
                if (!service.Status.Equals(ServiceControllerStatus.Running) && !service.Status.Equals(ServiceControllerStatus.StartPending))
                {
                    error = true;
                    Logger.GetInstance().Error("service status not running or not pending start");
                }

                if (error)
                {

                    Logger.GetInstance().Info("Starting service");
                    if (service.Status.Equals(ServiceControllerStatus.Running))
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped);
                    }
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running);
                    //Wait for proper initialisation of java and erlang
                    System.Threading.Thread.Sleep(20000);
                    Configuration.SetPids();
                }
            }
            catch (Exception ex)
            {
                Logger.GetInstance().Error("service exception" + ex.Message);
                Logger.GetInstance().Error("mydlpepwin service not found reinstall MyDLP Endpoint");
                Environment.Exit(1);
            }
        }

        public bool CheckProcRunningByName(String procname)
        {
            try
            {
                System.Diagnostics.Process[] process = System.Diagnostics.Process.GetProcessesByName(procname);
                if (process.Length == 0)
                    return false;

                if (process[0].HasExited)
                    return false;
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        public void Stop()
        {
            Logger.GetInstance().Info("mydlpepwatchdog service stopped");
        }

        private static WatchdogController controller = null;
        private WatchdogController()
        {
        }
    }
}
