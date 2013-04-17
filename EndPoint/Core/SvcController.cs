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
using System.ServiceProcess;
using System.Threading;

namespace MyDLP.EndPoint.Core
{
    public class SvcController
    {
        public static void StartService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

                if (!service.Status.Equals(ServiceControllerStatus.Running))
                    service.Start();
                service.WaitForStatus(ServiceControllerStatus.Running, timeout);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Unable to start sevice: " + serviceName + " error: " + e);
            }
        }

        public static void StartServiceNonBlocking(string serviceName, int timeoutMilliseconds)
        {

            try
            {
                Thread thread = new Thread(new ParameterizedThreadStart(StartServiceBackround));
                thread.Start(new ServiceParameters(serviceName, timeoutMilliseconds));
            }
            catch (ThreadStartException e)
            {
                Logger.GetInstance().Error(e);
            }
            catch (ThreadAbortException e)
            {
                Logger.GetInstance().Error(e);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e);
            }
        }

        public static void StopService(string serviceName, int timeoutMilliseconds)
        {
            ServiceController service = new ServiceController(serviceName);
            try
            {
                TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);
                if (service.Status.Equals(ServiceControllerStatus.Running))
                    service.Stop();
                service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("Unable to stop sevice: " + serviceName + " error: " + e);
            }
        }

        public static void RestartService(string serviceName, int timeoutMilliseconds)
        {
            StopService(serviceName, timeoutMilliseconds / 2);
            StartService(serviceName, timeoutMilliseconds / 2);
        }

        public static bool IsServiceInstalled(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController service in services)
            {
                if (service.ServiceName == serviceName)
                    return true;
            }
            return false;
        }

        public static bool IsServiceRunning(string serviceName)
        {
            ServiceController[] services = ServiceController.GetServices();

            foreach (ServiceController service in services)
            {
                if (service.ServiceName == serviceName)
                    if (service.Status == ServiceControllerStatus.Running)
                        return true;
            }
            return false;
        }

        private static void StartServiceBackround(object parameters)
        {
            ServiceParameters param = (ServiceParameters)parameters;
            StartService(param.serviceName, param.timeout);
        }

        public delegate void StopMyDLPDelegate();
        public static StopMyDLPDelegate StopMyDLP;

        class ServiceParameters
        {
            public ServiceParameters(String serviceName, int timeout)
            {
                this.timeout = timeout;
                this.serviceName = serviceName;
            }

            public String serviceName;
            public int timeout;
        }
    }
}
