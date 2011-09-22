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
using System.Runtime.InteropServices;

namespace MyDLP.EndPoint.Core
{
    public class Engine
    {
        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        public static extern int GetShortPathName(
            [MarshalAs(UnmanagedType.LPTStr)]
        string path,
            [MarshalAs(UnmanagedType.LPTStr)]
        StringBuilder shortPath,
            int shortPathLength
            );

        String pythonStartCmd = @"cd " + Configuration.PyBackendPath + " && Run.bat";        
        String erlStartCmd = @"cd " + Configuration.ErlangPath + " && Run.bat";

        public void Start()
        {
            ExecuteCommandAsync(pythonStartCmd);
            ExecuteCommandAsync(erlStartCmd);            
        }


        public void Stop()
        {   //TODO:Handle process with pid files
            KillProcByName("python");
            KillProcByName("epmd");
            KillProcByName("erl");
        }

        public String GetShortPath(String path)
        {
            StringBuilder shortPath = new StringBuilder(255);
            GetShortPathName(path, shortPath, shortPath.Capacity);
            return shortPath.ToString();
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
            //TODO:this is dity
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;

                if (command.ToString() == pythonStartCmd)
                {
                    procStartInfo.EnvironmentVariables["path"] = procStartInfo.EnvironmentVariables["path"] + @";" + Configuration.PythonBinPaths;

                    if (procStartInfo.EnvironmentVariables.ContainsKey("PYTHONPATH"))
                    {
                        procStartInfo.EnvironmentVariables["PYTHONPATH"] = procStartInfo.EnvironmentVariables["PYTHONPATH"] + ";" + Configuration.PythonPath;
                    }
                    else
                    {
                        procStartInfo.EnvironmentVariables.Add("PYTHONPATH", Configuration.PythonPath);
                    }
                    Logger.GetInstance().Debug("Environment path:" + procStartInfo.EnvironmentVariables["path"]);
                    Logger.GetInstance().Debug("Environment PYTHONPATH:" + procStartInfo.EnvironmentVariables["PYTHONPATH"]);

                }

                if (command.ToString() == erlStartCmd)
                {
                   
                    if (System.Environment.UserInteractive)
                    {
                        procStartInfo.EnvironmentVariables.Add("MYDLP_CONF", GetShortPath(Configuration.AppPath).Replace(@"\", @"/") + "mydlp-ep.conf");
                    }
                    else
                    {
                        procStartInfo.EnvironmentVariables.Add("MYDLP_CONF", GetShortPath(Configuration.AppPath).Replace(@"\", @"/") + "mydlp.conf");
                    }

                    procStartInfo.EnvironmentVariables.Add("MYDLPBEAMDIR", GetShortPath(Configuration.ErlangPath));

                    procStartInfo.EnvironmentVariables["path"] = procStartInfo.EnvironmentVariables["path"] + @";" + Configuration.ErlangBinPaths;
                    Logger.GetInstance().Debug("Environment path:" + procStartInfo.EnvironmentVariables["path"]);
                    Logger.GetInstance().Debug("Environment MYDLP_CONF:" + procStartInfo.EnvironmentVariables["MYDLP_CONF"]);
                }
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;

                Logger.GetInstance().Debug("Starting process:" + command);            
                proc.Start();                
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
