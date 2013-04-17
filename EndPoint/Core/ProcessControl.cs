//    Copyright (C) 2012 Huseyin Ozgur Batur <ozgur@medra.com.tr>
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
    public class EnvVar
    {
        public String Name;
        public String Value;

        public EnvVar(String name, String value)
        {
            Name = name;
            Value = value;
        }
    }

    public class ExecuteParameters
    {
        public String Command;
        public String Prefix;
        public EnvVar[] Env;

        public ExecuteParameters(String command, String prefix, EnvVar[] env)
        {
            Command = command;
            Prefix = prefix;
            Env = env;
        }

        public ExecuteParameters(String command, String prefix)
        {
            Command = command;
            Prefix = prefix;
            Env = new EnvVar[] { };

        }
    }

    public class ProcessControl
    {
        public static void ExecuteCommandAsync(string command, string prefix)
        {
            ExecuteCommandAsync(command,  prefix, new EnvVar[]{});
        }

        public static void ExecuteCommandAsync(string command, string prefix, EnvVar[] env)
        {
            try
            {
                Thread objThread = new Thread(new ParameterizedThreadStart(ExecuteCommandSync));
                objThread.Start(new ExecuteParameters(command, prefix, env));
            }
            catch (ThreadStartException e)
            {
                Logger.GetInstance().Error(e);
            }
            catch (ThreadAbortException e)
            {
                Logger.GetInstance().Error(e);
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(e);
            }
        }

        public static void ExecuteCommandSync(object parameters)
        {
            ExecuteParameters param = (ExecuteParameters)parameters;

            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + param.Command);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.RedirectStandardError = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                procStartInfo.ErrorDialog = false;

                foreach (EnvVar var in param.Env)
                {
                    if (var.Name == "path")
                        procStartInfo.EnvironmentVariables["path"] = procStartInfo.EnvironmentVariables["path"] + var.Value;
                    else
                        procStartInfo.EnvironmentVariables.Add(var.Name, var.Value);
                    Logger.GetInstance().Debug(param.Prefix + " EvnVar:" + var.Name + " = " + procStartInfo.EnvironmentVariables[var.Name]);

                }

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                Logger.GetInstance().Debug(param.Prefix + " starting process: \"" + param.Command + "\"");
                proc.OutputDataReceived += (sender, args) => SuppressUnnecessaryDebug(param.Prefix, args.Data);
                proc.ErrorDataReceived += (sender, args) => SuppressUnnecessaryError(param.Prefix, args.Data);
                proc.EnableRaisingEvents = true;
                proc.Start();
                proc.BeginOutputReadLine();
                proc.BeginErrorReadLine();
                proc.WaitForExit();
                proc.CancelErrorRead();
                proc.CancelOutputRead();
                Logger.GetInstance().Debug(param.Prefix + " process exited.");
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(param.Prefix + " " + e);
            }
        }

        public static string CommandOutputSync(object parameters)
        {
            ExecuteParameters param = (ExecuteParameters)parameters;

            try
            {
                System.Diagnostics.ProcessStartInfo procStartInfo =
                    new System.Diagnostics.ProcessStartInfo("cmd", "/c " + param.Command);
                procStartInfo.RedirectStandardOutput = true;
                procStartInfo.RedirectStandardError = true;
                procStartInfo.UseShellExecute = false;
                procStartInfo.CreateNoWindow = true;
                procStartInfo.ErrorDialog = false;

                foreach (EnvVar var in param.Env)
                {
                    if (var.Name == "path")
                        procStartInfo.EnvironmentVariables["path"] = procStartInfo.EnvironmentVariables["path"] + var.Value;
                    else
                        procStartInfo.EnvironmentVariables.Add(var.Name, var.Value);
                    System.Console.WriteLine(param.Prefix + " EvnVar:" + var.Name + " = " + procStartInfo.EnvironmentVariables[var.Name]);

                }

                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = procStartInfo;
                proc.EnableRaisingEvents = true;
                proc.Start();

                string stdoutx = proc.StandardOutput.ReadToEnd();
                string stderrx = proc.StandardError.ReadToEnd();
                proc.WaitForExit();
                return stderrx + stdoutx;
            }
            catch (Exception e)
            {
                Logger.GetInstance().Error(param.Prefix + " " + e);
            }
            return null;
        }

        private static void SuppressUnnecessaryError(String prefix, String data)
        {

            if (data != null && data.Trim().Length != 0)
            {
                if (data.Contains("SLF4J")) return;
                Logger.GetInstance().Error(prefix + " " + data);
            }

        }

        private static void SuppressUnnecessaryDebug(String prefix, String data)
        {
            if (data != null && data.Trim().Length != 0)
                Logger.GetInstance().Debug(prefix + " " + data);
        }
    }
}
