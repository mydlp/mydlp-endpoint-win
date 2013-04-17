using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ServiceProcess;

namespace SvcRestart
{
    class Program
    {
        static void Main(string[] args)
        {
            string serviceName;

            try
            {
                if (args.Length != 1)
                {
                    throw new Exception("Invalid argument");
                }

                serviceName = args[0];

                try
                {
                    ServiceController service = new ServiceController(serviceName);
                    
                    if (!service.Status.Equals(ServiceControllerStatus.Stopped) 
                        && !service.Status.Equals(ServiceControllerStatus.StopPending))
                    {
                        service.Stop();  
                    }
                    service.WaitForStatus(ServiceControllerStatus.Stopped);
                    service.Start();
                    service.WaitForStatus(ServiceControllerStatus.Running);
                }

                catch (Exception e)
                {
                    Console.WriteLine(e);
                    return;
                }

            }
            catch (Exception e)
            {
                Console.WriteLine(e);               
                return;
            }
        }
    }
}
