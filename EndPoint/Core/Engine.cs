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
using Microsoft.Win32;
using System.Diagnostics;

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
        String pythonManualStartCmd = @"cd " + Configuration.PyBackendPath + " && ManualRun.bat";
        String erlStartCmd = @"cd " + Configuration.ErlangPath + " && Run.bat";
        String erlStartInteractiveCmd = @"cd " + Configuration.ErlangPath + " && InteractiveRun.bat";

        public void Start()
        {
            if (System.Environment.UserInteractive)
            {
                ExecuteCommandAsync(pythonManualStartCmd);
            }
            else
            {
                ExecuteCommandAsync(pythonStartCmd);
            }
            
            // TODO: When SetErlConf fails service is consuming system resources, user
            // can hardly use system. When this command fails service should exit.
            Configuration.SetErlConf();

            if (System.Environment.UserInteractive)
            {
                ExecuteCommandAsync(erlStartInteractiveCmd);
            }
            else
            {
                ExecuteCommandAsync(erlStartCmd);
            }
        }

        public void Stop()
        {   //TODO:Handle process with pid files
            Logger.GetInstance().Debug("Kill python by pid:" + Configuration.PythonPid);
            if (Configuration.PythonPid != 0)
            {
                try
                {
                    Process p = Process.GetProcessById(Configuration.PythonPid);
                    p.Kill();
                }
                catch
                {
                    Logger.GetInstance().Debug("Kill python by pid failed:" + Configuration.PythonPid);
                    KillProcByName("python");
                }
            }
            else 
            {
                KillProcByName("python");
            }

            KillProcByName("epmd");

            Logger.GetInstance().Debug("Kill erlang by pid:" + Configuration.PythonPid);
            if (Configuration.ErlPid != 0)
            {
                try
                {
                    Process p = Process.GetProcessById(Configuration.ErlPid);
                    p.Kill();
                }
                catch
                {
                    Logger.GetInstance().Debug("Kill erlang by pid failed:" + Configuration.ErlPid);
                    KillProcByName("erl");
                    KillProcByName("werl");
                }
            }
            else
            {
                KillProcByName("erl");
                KillProcByName("werl");
            }
        }

        public static String GetShortPath(String path)
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
            //TODO:this is dirty
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;

                if (command.ToString() == pythonStartCmd || command.ToString() == pythonManualStartCmd)
                {
                    procStartInfo.EnvironmentVariables["path"] = procStartInfo.EnvironmentVariables["path"] + @";" + Configuration.PythonBinPaths;

                    if (procStartInfo.EnvironmentVariables.ContainsKey("PYTHONPATH"))
                    {
                        //We want only our python path
                        //procStartInfo.EnvironmentVariables["PYTHONPATH"] = procStartInfo.EnvironmentVariables["PYTHONPATH"] + ";" + Configuration.PythonPath;
                        procStartInfo.EnvironmentVariables["PYTHONPATH"] = Configuration.PythonPath;
                    }
                    else
                    {
                        procStartInfo.EnvironmentVariables.Add("PYTHONPATH", Configuration.PythonPath);
                    }
                    procStartInfo.EnvironmentVariables.Add("MYDLP_APPDIR", GetShortPath(Configuration.AppPath));

                    Logger.GetInstance().Debug("Environment path for python:" + procStartInfo.EnvironmentVariables["path"]);
                    Logger.GetInstance().Debug("Environment PYTHONPATH:" + procStartInfo.EnvironmentVariables["PYTHONPATH"]);

                }

                if (command.ToString() == erlStartCmd)
                {
                    procStartInfo.EnvironmentVariables.Add("MYDLP_CONF", GetShortPath(Configuration.MydlpConfPath).Replace(@"\", @"/"));
                    procStartInfo.EnvironmentVariables.Add("MYDLPBEAMDIR", GetShortPath(Configuration.ErlangPath));
                    procStartInfo.EnvironmentVariables.Add("MYDLP_APPDIR", GetShortPath(Configuration.AppPath));
                    procStartInfo.EnvironmentVariables["path"] = procStartInfo.EnvironmentVariables["path"] + @";" + Configuration.ErlangBinPaths;

                    Logger.GetInstance().Debug("Environment path for erlang:" + procStartInfo.EnvironmentVariables["path"]);
                    Logger.GetInstance().Debug("Environment MYDLP_CONF:" + procStartInfo.EnvironmentVariables["MYDLP_CONF"]);
                    Logger.GetInstance().Debug("Environment MYDLP_APPDIR:" + procStartInfo.EnvironmentVariables["MYDLP_APPDIR"]);
                }

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                Logger.GetInstance().Debug("Starting process:" + command);
                proc.Start();
                string result = proc.StandardOutput.ReadToEnd();
                Logger.GetInstance().Debug(result);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e.Message + " " + e.StackTrace);
            }
        }
    }
}
