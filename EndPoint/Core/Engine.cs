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
        {


        }

        public void ExecuteCommandAsync(string command)
        {
            try
            {
                //Asynchronously start the Thread to process the Execute command request.
                Thread objThread = new Thread(new ParameterizedThreadStart(ExecuteCommandSync));
                //Make the thread as background thread.
                //objThread.IsBackground = true;
                //Set the Priority of the thread.
                //objThread.Priority = ThreadPriority.AboveNormal;
                //Start the thread.
                objThread.Start(command);
            }
            catch (ThreadStartException objException)
            {
                // Log the exception
            }
            catch (ThreadAbortException objException)
            {
                // Log the exception
            }
            catch (Exception objException)
            {
                // Log the exception
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
            catch (Exception objException)
            {
                // Log the exception
            }
        }
    }
}
