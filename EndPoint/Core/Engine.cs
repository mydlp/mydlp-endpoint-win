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
using System.Threading;

namespace MyDLP.EndPoint.Core
{
    public class Engine
    {
        String pythonStartCmd = //@"echo %CD%&& cd ..\..\..\Engine\mydlp\src\backend\py\&& path %path%;" +
            //@"C:\workspace\mydlp-deployment-env\Python26 && echo pp1 && " +
            //@"set PYTHONPATH=..\..\..\Engine\mydlp\src\thrift\gen-py && " +
            //@"echo pp2 && python MyDLPBackendServer.py mydlp-backend-py.pid && echo pp3";
            @"cd C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\backend\py\ && Run.bat";

        String erlStartCmd = //@"echo %CD%&& cd ..\..\..\Engine\mydlp\src\mydlp\&& path %path%;" +
            //@"C:\workspace\mydlp-deployment-env\erl5.7.4\bin;C:\workspace\mydlp-deployment-env\erts-5.7.4\bin &&" +
            @"cd C:\workspace\mydlp-endpoint-win\EndPoint\Engine\mydlp\src\mydlp\ && Run.bat";

        public void Start()
        {
            ExecuteCommandAsync(pythonStartCmd);
            //Thread.Sleep(2000);
            ExecuteCommandAsync(erlStartCmd);
        }


        public void Stop()
        {   //TODO:Handle process with pid files
            KillProcByName("python");
            KillProcByName("epmd");
            KillProcByName("erl");
        }

        public void KillProcByName(String procname)
        {
            try
            {
                Logger.GetInstance().Debug("Killing name:" + procname);
                System.Diagnostics.Process[] process = System.Diagnostics.Process.GetProcessesByName(procname);
                foreach (System.Diagnostics.Process p in process)
                {
                    Logger.GetInstance().Debug("Killing pid:" + p.Id + " name: " + p.ProcessName);
                    p.Kill();
                }
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e.Message + " " + e.StackTrace);
            }
        }



        public void ExecuteCommandAsync(string command)
        {
            try
            {
                Thread objThread = new Thread(new ParameterizedThreadStart(ExecuteCommandSync));
                objThread.Start(command);
            }
            catch (ThreadStartException e)
            {
                Logger.GetInstance().Error(e.Message + " " + e.StackTrace);
            }
            catch (ThreadAbortException e)
            {
                Logger.GetInstance().Error(e.Message + " " + e.StackTrace);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e.Message + " " + e.StackTrace);
            }
        }

        public void ExecuteCommandSync(object command)
        {
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.Start();
                proc.WaitForExit();
                string result = proc.StandardOutput.ReadToEnd();
                Console.WriteLine(result);
                Logger.GetInstance().Info(result);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e.Message + " " + e.StackTrace);
            }
        }
    }
}
