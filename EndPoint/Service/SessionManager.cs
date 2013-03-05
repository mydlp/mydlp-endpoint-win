using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using MyDLP.EndPoint.Core;
using System.Collections;

namespace MyDLP.EndPoint.Service
{
    public class SessionManager
    {

        private static SessionManager sessionManager;
        private ArrayList sessions;

        public static void Start()
        {
            sessionManager = new SessionManager();
            //sessionManager.StartLogonListener();
            //sessionManager.EnumerateSessions();


            //This is to resolve circular dependency
            Configuration.GetLoggedOnUser = new Configuration.GetLoggedOnUserDeleagate(GetCurrentUser);
        }

        public static String GetCurrentUser()
        {
            MyDLPEP.LogonSession session = MyDLPEP.SessionUtils.GetActiveSession();

            

            if (session != null)
            {
                Logger.GetInstance().Debug("Sid:" + session.sid);
                //Update secure printers for shared printers
                if (Configuration.PrinterMonitor)
                    PrinterController.getInstance().ListenPrinterConnections(session.sid);
                return session.name + "@" + session.domain;
            }
            else
            {
                return "No Session";
            }
        }

        public static void Stop()
        {
            sessionManager = null;
        }

        /*public void EnumerateSessions()
        {
            sessions = MyDLPEP.SessionUtils.EnumerateLogonSessions();
        }
         */

        private SessionManager()
        {
            sessions = new ArrayList();

        }

        /*private void StartLogonListener()
        {
            try
            {
                Logger.GetInstance().Debug("Start StartLogonListener");
                ManagementEventWatcher w = null;
                WqlEventQuery q = new WqlEventQuery();
                q.EventClassName = "__InstanceCreationEvent";
                q.WithinInterval = new TimeSpan(0, 0, 30); // query interval
                q.Condition = @"TargetInstance ISA 'Win32_LogonSession'";
                w = new ManagementEventWatcher(q);
                w.EventArrived += new EventArrivedEventHandler(LogonEventArrived);
                w.Start();
                Logger.GetInstance().Debug("Start StartLogonListener Finished");
            }
            catch(Exception ex)
            {
                Logger.GetInstance().Error(ex.StackTrace + " " + ex.Message);
            }
        }*/


        /*private static void LogonEventArrived(object sender, EventArrivedEventArgs e)
        {
            Logger.GetInstance().Debug("LOGON OCCURED");
            sessionManager.EnumerateSessions();
        }*/

    }
}
