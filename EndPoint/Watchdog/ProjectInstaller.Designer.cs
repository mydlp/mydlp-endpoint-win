namespace MyDLP.EndPoint.Service
{
    partial class ProjectInstaller
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.watchdogServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.watchdogServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // watchdogServiceProcessInstaller
            // 
            this.watchdogServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.watchdogServiceProcessInstaller.Password = null;
            this.watchdogServiceProcessInstaller.Username = null;
            this.watchdogServiceProcessInstaller.AfterInstall += new System.Configuration.Install.InstallEventHandler(this.myDLPServiceProcessInstaller_AfterInstall);
            // 
            // watchdogServiceInstaller
            // 
            this.watchdogServiceInstaller.Description = "MyDLP EP Watchdog";
            this.watchdogServiceInstaller.DisplayName = "MyDLP EP Watchdog";
            this.watchdogServiceInstaller.ServiceName = "mydlpepwatchdog";
            this.watchdogServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.watchdogServiceProcessInstaller,
            this.watchdogServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller watchdogServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller watchdogServiceInstaller;
    }
}