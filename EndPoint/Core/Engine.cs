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
using System.IO;
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

        static String javaStartCmd = @"cd " + Configuration.JavaBackendPath + " && Run.bat";
        // String javaManualStartCmd = @"cd " + Configuration.JavaBackendPath + " && ManualRun.bat";
        static String javaManualStartCmd = @"cd " + Configuration.JavaBackendPath + " && Run.bat";
        static String erlStartCmd = @"cd " + Configuration.ErlangPath + " && Run.bat";
        static String erlStartInteractiveCmd = @"cd " + Configuration.ErlangPath + " && InteractiveRun.bat";

        public static void Start()
        {
            //clear pid files befire startup
            String path = Configuration.AppPath + @"\run\mydlp.pid";
            File.Delete(path);

            path = Configuration.AppPath + @"\run\backend.pid";
            File.Delete(path);

            Logger.GetInstance().Info("Starting Java Backend");
            if (System.Environment.UserInteractive)
            {
                ExecuteCommandAsync(javaStartCmd);
            }
            else
            {
                ExecuteCommandAsync(javaManualStartCmd);
            }

            // TODO: When SetErlConf fails service is consuming system resources, user
            // can hardly use system. When this command fails service should exit.
            Configuration.SetErlConf();

            Logger.GetInstance().Info("Starting Erlang Backend");
            if (System.Environment.UserInteractive)
            {
                ExecuteCommandAsync(erlStartInteractiveCmd);
            }
            else
            {
                ExecuteCommandAsync(erlStartCmd);
            }
        }

        public static void Stop()
        {
            Logger.GetInstance().Info("Stopping Java Backend");
            Logger.GetInstance().Debug("Kill java by pid:" + Configuration.JavaPid);
            if (Configuration.JavaPid != 0)
            {
                try
                {
                    Process p = Process.GetProcessById(Configuration.JavaPid);
                    p.Kill();
                }
                catch
                {
                    Logger.GetInstance().Debug("Kill java by pid failed:" + Configuration.JavaPid);
                    KillProcByName("java");
                }
            }
            else
            {
                KillProcByName("java");
            }

            KillProcByName("epmd");

            Logger.GetInstance().Info("Stopping Erlang Backend");
            Logger.GetInstance().Debug("Kill erlang by pid:" + Configuration.ErlPid);
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

        public static void KillProcByName(String procname)
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

        public static void ExecuteCommandAsync(string command)
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

        public static void ExecuteCommandSync(object command)
        {
            //TODO:this is dirty
            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + command);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;

                if (command.ToString() == javaStartCmd || command.ToString() == javaManualStartCmd)
                {
                    procStartInfo.EnvironmentVariables.Add("JRE_BIN_DIR", GetShortPath(Configuration.JavaBinPaths));
                    procStartInfo.EnvironmentVariables.Add("BACKEND_DIR", GetShortPath(Configuration.JavaPath));
                    procStartInfo.EnvironmentVariables.Add("MYDLP_APPDIR", GetShortPath(Configuration.AppPath));

                    Logger.GetInstance().Debug("Environment JRE_BIN_DIR for backend:" + procStartInfo.EnvironmentVariables["JRE_BIN_DIR"]);
                    Logger.GetInstance().Debug("Environment BACKEND_DIR for backend:" + procStartInfo.EnvironmentVariables["BACKEND_DIR"]);
                    Logger.GetInstance().Debug("Environment MYDLP_APPDIR for backend:" + procStartInfo.EnvironmentVariables["MYDLP_APPDIR"]);

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
