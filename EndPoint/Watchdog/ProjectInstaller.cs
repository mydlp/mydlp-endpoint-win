using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration.Install;


namespace MyDLP.EndPoint.Service
{
    [RunInstaller(true)]
    public partial class ProjectInstaller : Installer
    {
        public ProjectInstaller()
        {
            InitializeComponent();
        }

        private void myDLPServiceProcessInstaller_AfterInstall(object sender, InstallEventArgs e)
        {

        }
    }
}
