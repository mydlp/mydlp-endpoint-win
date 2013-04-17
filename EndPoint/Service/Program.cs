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
using System.ServiceProcess;
using System.Text;
using System.Configuration.Install;
using Microsoft.Win32;
using System.Reflection;

namespace MyDLP.EndPoint.Service
{
    static class Program
    {
        static void Main(string[] args)
        {

            if (System.Environment.UserInteractive)
            {
                ManagedInstallerClass.InstallHelper(new string[] { Assembly.GetExecutingAssembly().Location });
                try
                {
                    RegistryKey ckey =
                    Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\mydlpepwin",
                    true);

                    if (ckey != null)
                    {

                        if (ckey.GetValue("Type") != null)
                        {
                            ckey.SetValue("Type", ((int)ckey.GetValue("Type") | 256));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
            }
            else
            {
                ServiceBase.Run(new MyDLPService());
            }
        }
    }
}
