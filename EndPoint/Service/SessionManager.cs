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
        public static void Start()
        {
            sessionManager = new SessionManager();
            //sessionManager.StartLogonListener();
            //sessionManager.EnumerateSessions();
            Configuration.GetLoggedOnUser = new Configuration.GetLoggedOnUserDeleagate(GetCurrentUser);
        }

        public static String GetCurrentUser()
        {
            MyDLPEP.InteractiveSession session = MyDLPEP.SessionUtils.GetActiveSession();

            if (session != null)
            {
                Logger.GetInstance().Debug("Sid:" + session.sid);
                return session.name + "@" + session.domain;
            }
            else
            {
                return "NO OWNER";
            }
        }

        public static void Stop()
        {
            sessionManager = null;
        }

        private static SessionManager sessionManager;
        //private ArrayList sessions;

        private SessionManager()
        {
            //sessions = new ArrayList();

        }

        /*private void StartLogonListener()
        {
            try
            {
                Logger.GetInstance().Debug("Start StartLogonListener");
                ManagementEventWatcher w = null;
                WqlEventQuery q = new WqlEventQuery();
                q.EventClassName = "__InstanceCreationEvent";
                q.WithinInterval = new TimeSpan(0, 0, 15); // query interval
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

        /*
        private static void LogonEventArrived(object sender, EventArrivedEventArgs e)
        {
            Logger.GetInstance().Debug("LOGOON/LOGOF OCCURED");
            //sessionManager.EnumerateSessions();
        }*/

        /*
        private static String FindActiveUser() 
        {
            ArrayList a = MyDLPEP.SessionUtils.EnumerateLogonSessions();
            foreach ( MyDLPEP.InteractiveSession s in a )
            {
                if (s.sessionId == sessionId) 
                {
                    return s.name + "@" + s.domain;                
                }
                //Console.WriteLine("SessionId:" + s.sessionId + " Name:" + s.name + " Domain:" + s.domain + " upn:" + s.upn + " sid:" + s.sid + " logontime:" + s.logonTime);
            }
            return "NOOWNER";

            MyDLPEP.InteractiveSession session = MyDLPEP.SessionUtils.GetActiveSession();
            if (session != null) 
            {
                return session.name + "@" + session.domain;
            } 
            return "NO OWNER";
        }*/
    }
}
