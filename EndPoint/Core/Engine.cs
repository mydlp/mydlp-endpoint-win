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
            int shortPathLength);

        static String javaStartCmd = @"cd " + Configuration.JavaBackendPath + " && Run.bat";
        static String erlStartCmd = @"cd " + Configuration.ErlangPath + " && Run.bat";
        static String erlInstallCmd = @"cd " + Configuration.ErlangPath + " && RegisterService.bat";
        static String erlStartInteractiveCmd = @"cd " + Configuration.ErlangPath + " && InteractiveRun.bat";

        public static void Start()
        {
            //clear pid files before startup
            String path = Configuration.AppPath + @"\run\mydlp.pid";
            File.Delete(path);

            path = Configuration.AppPath + @"\run\backend.pid";
            File.Delete(path);

            EnvVar[] erlEnv = new EnvVar[] {
                new EnvVar("MYDLP_CONF", GetShortPath(Configuration.MydlpConfPath).Replace(@"\", @"/")), 
                new EnvVar("MYDLPBEAMDIR",GetShortPath(Configuration.ErlangPath)), 
                new EnvVar("MYDLP_APPDIR",GetShortPath(Configuration.AppPath)),
                new EnvVar("ERLANG_HOME", GetShortPath(Configuration.ErlangHome))
            };

            EnvVar[] javaEnv = new EnvVar[] {
                new EnvVar("JRE_BIN_DIR", GetShortPath(Configuration.JavaBinPaths)), 
                new EnvVar("BACKEND_DIR",GetShortPath(Configuration.JavaPath)), 
                new EnvVar("MYDLP_APPDIR",GetShortPath(Configuration.AppPath))
            };


           if (!System.Environment.UserInteractive && SvcController.IsServiceInstalled("mydlpengine"))
            {
                if (SvcController.IsServiceRunning("mydlpengine"))
                {
                    SvcController.StopService("mydlpengine", 5000);
                }

                ProcessControl.ExecuteCommandSync(new ExecuteParameters("sc delete mydlpengine", "SC", erlEnv));               
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Ericsson\\Erlang", true))
                {
                    key.DeleteSubKeyTree("ErlSrv");
                }
            }
            catch
            {
                Logger.GetInstance().Info("Unable to delete uneccessary keys or keys do not exists");
            }

            Logger.GetInstance().Info("Starting Java Backend");
           

            ProcessControl.ExecuteCommandAsync(javaStartCmd, "JAVA:", javaEnv);

            Configuration.SetErlConf();

            Logger.GetInstance().Info("Starting Erlang Backend");
            if (System.Environment.UserInteractive)
            {
                ProcessControl.ExecuteCommandAsync(erlStartInteractiveCmd, "WERL", erlEnv);
            }
            else
            {
                ProcessControl.ExecuteCommandAsync(erlStartCmd, "ERL", erlEnv);
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

            if (!System.Environment.UserInteractive && SvcController.IsServiceInstalled("mydlpengine"))
            {
                if (SvcController.IsServiceRunning("mydlpengine"))
                {
                    SvcController.StopService("mydlpengine", 5000);
                }

                
                EnvVar[] erlEnv = new EnvVar[] {
                new EnvVar("MYDLP_CONF", GetShortPath(Configuration.MydlpConfPath).Replace(@"\", @"/")), 
                new EnvVar("MYDLPBEAMDIR",GetShortPath(Configuration.ErlangPath)), 
                new EnvVar("MYDLP_APPDIR",GetShortPath(Configuration.AppPath)),
                new EnvVar("ERLANG_HOME", GetShortPath(Configuration.ErlangHome))
                };

                ProcessControl.ExecuteCommandSync(new ExecuteParameters("sc delete mydlpengine", "SC", erlEnv));                                          
            }

            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Ericsson\\Erlang", true))
                {
                    key.DeleteSubKeyTree("ErlSrv");
                }
            }
            catch
            {
                Logger.GetInstance().Info("Unable to delete uneccessary keys or keys do not exists");
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
    }
}
