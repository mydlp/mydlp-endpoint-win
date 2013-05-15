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
            String upn = "No Session";
            String sid = "";

            List<MyDLPEP.ActiveSession> consoleSessions = null;
            List<MyDLPEP.LogonSession> logonSessions = null;

            try
            {
                consoleSessions = MyDLPEP.SessionUtils.EnumerateActiveSessionIds();
                logonSessions = MyDLPEP.SessionUtils.GetLogonSessions();

                //just debugging
                foreach (MyDLPEP.ActiveSession session in consoleSessions)
                {
                    Logger.GetInstance().Debug("ConsoleSession: name=" + session.name + " domain=" + session.domain + " sessionId=" + session.sessionId);
                }

                foreach (MyDLPEP.LogonSession session in logonSessions)
                {
                    Logger.GetInstance().Debug("LogonSession: " + session);
                }

                foreach (MyDLPEP.ActiveSession cSession in consoleSessions)
                {
                    foreach (MyDLPEP.LogonSession lSession in logonSessions)
                    {
                        if (cSession.sessionId == lSession.sessionId && cSession.name == lSession.name)
                        {
                            upn = lSession.name + "@" + lSession.domain;
                            sid = lSession.sid;
                        }
                    }
                }

            }
            catch (Exception e)
            {
                Logger.GetInstance().Error("GetUSer error:" + e);
            }


            if (sid != "")
            {
                Logger.GetInstance().Debug("Sid:" + sid);
                //Update secure printers for shared printers
                if (Configuration.PrinterMonitor)
                    PrinterController.getInstance().ListenPrinterConnections(sid);
            }

            return upn;
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
