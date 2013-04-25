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
            List<MyDLPEP.LogonSession>sessions = MyDLPEP.SessionUtils.GetActiveSessions();
            Dictionary<int,int> sessionDic = new Dictionary<int,int>();
            MyDLPEP.LogonSession activeSession = null;
            
            Process[] processlist = Process.GetProcesses();
            foreach (Process process in processlist)
            {
                if (sessionDic.ContainsKey(process.SessionId)){
                    sessionDic[process.SessionId] = sessionDic[process.SessionId] + 1;                
                }
                else
                {
                    sessionDic.Add(process.SessionId, 1); 
                }
            } 
                        
            int sessionWithMostProcesses = 0;

            foreach (MyDLPEP.LogonSession session in sessions)
            {
                if (sessionDic[session.sessionId] >= sessionDic[sessionWithMostProcesses])
                {
                    sessionWithMostProcesses = session.sessionId;
                    activeSession = session;
                }
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
