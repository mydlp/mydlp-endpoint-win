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
            this.myDLPServiceProcessInstaller = new System.ServiceProcess.ServiceProcessInstaller();
            this.myDLPServiceInstaller = new System.ServiceProcess.ServiceInstaller();
            // 
            // myDLPServiceProcessInstaller
            // 
            this.myDLPServiceProcessInstaller.Account = System.ServiceProcess.ServiceAccount.LocalSystem;
            this.myDLPServiceProcessInstaller.Password = null;
            this.myDLPServiceProcessInstaller.Username = null;
            // 
            // myDLPServiceInstaller
            // 
            this.myDLPServiceInstaller.Description = "MyDLP EP Win";
            this.myDLPServiceInstaller.DisplayName = "MyDLP EP Win";
            this.myDLPServiceInstaller.ServiceName = "mydlpepwin";
            this.myDLPServiceInstaller.StartType = System.ServiceProcess.ServiceStartMode.Automatic;
            // 
            // ProjectInstaller
            // 
            this.Installers.AddRange(new System.Configuration.Install.Installer[] {
            this.myDLPServiceProcessInstaller,
            this.myDLPServiceInstaller});

        }

        #endregion

        private System.ServiceProcess.ServiceProcessInstaller myDLPServiceProcessInstaller;
        private System.ServiceProcess.ServiceInstaller myDLPServiceInstaller;
    }
}