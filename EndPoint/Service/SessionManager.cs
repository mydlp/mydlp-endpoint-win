using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management;
using MyDLP.EndPoint.Core;
using System.Collections;
using System.Diagnostics;

namespace MyDLP.EndPoint.Service
{
    public class SessionManager
    {

        private static SessionManager sessionManager;
        private ArrayList sessions;

        public static void Start()
        {
            sessionManager = new SessionManager();

            //This is to resolve circular dependency
            Configuration.GetLoggedOnUser = new Configuration.GetLoggedOnUserDeleagate(GetCurrentUser);
        }

        public static String GetCurrentUser()
        {
            List<MyDLPEP.LogonSession> sessions = MyDLPEP.SessionUtils.GetActiveSessions();
            List<int> sessionIds = MyDLPEP.SessionUtils.EnumerateActiveSessionIds();
            
            MyDLPEP.LogonSession activeSession = null;
                     
            //just debugging
            foreach (int sessionId in sessionIds) 
            {
                Logger.GetInstance().Debug("EnumerateActiveSessionId: " + sessionId);
            }


            //this works reliable in Win 7
            foreach (MyDLPEP.LogonSession session in sessions)
            {
                Logger.GetInstance().Debug("Logon Session:" + session);
                if (sessionIds.Contains(session.sessionId)) 
                {
                    activeSession = session;
                }
            }

            if (activeSession == null && sessions.Count > 0) 
            {
                //last resort
                activeSession = sessions[0];            
            }                       

            if (activeSession != null)
            {
                Logger.GetInstance().Debug("Sid:" + activeSession.sid);
                //Update secure printers for shared printers
                if (Configuration.PrinterMonitor)
                    PrinterController.getInstance().ListenPrinterConnections(activeSession.sid);
                return activeSession.name + "@" + activeSession.domain;
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

         private SessionManager()
        {
            sessions = new ArrayList();

        }
    }
}
