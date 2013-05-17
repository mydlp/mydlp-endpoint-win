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
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.ServiceProcess;
using System.Text;

namespace MyDLP.EndPoint.Service
{
    public partial class MyDLPService : ServiceBase
    {
        public MyDLPService()
        {
            InitializeComponent();
            InitializeLogSource();
        }

        protected override void OnStart(string[] args)
        {
            MainController.SetServiceLogger(myDLPEventLog);
            MainController controller =
                MainController.GetInstance();
            controller.Start();
            //myDLPEventLog.WriteEntry("MyDLP-EP-Win service started");
        }

        protected override void OnStop()
        {
            MainController controller =
                MainController.GetInstance();
            controller.Stop();
            //myDLPEventLog.WriteEntry("MyDLP-EP-Win service stopped");
        }

        private void InitializeLogSource()
        {
            if (!System.Diagnostics.EventLog.SourceExists(myDLPEventLog.Source))
            {
                System.Diagnostics.EventLog.CreateEventSource(
                    myDLPEventLog.Source,
                    myDLPEventLog.Log
                    );
            }
        }
    }
}
