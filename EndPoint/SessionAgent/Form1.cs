using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Security.Principal;
using System.Diagnostics;

namespace MyDLP.EndPoint.SessionAgent
{
    public partial class Form1 : Form
    {

        public Form1()
        {
            InitializeComponent();
        }

        private Thread backgroundThread;

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            notifyIcon1.BalloonTipTitle = "Minimize to Tray App";
            notifyIcon1.BalloonTipText = "You have successfully minimized your form.";

            if (FormWindowState.Minimized == this.WindowState)
            {
                notifyIcon1.Visible = true;
                notifyIcon1.ShowBalloonTip(500);
                this.Hide();
            }
            else if (FormWindowState.Normal == this.WindowState)
            {
                notifyIcon1.Visible = false;
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            backgroundThread = new Thread(DoBackgroundWork);
            backgroundThread.Start();
        }

        private void DoBackgroundWork()
        {
            bool testSuccess = false;
            for (int i = 0; i < 10; i++)
            {
                testSuccess = ServiceClient.ServiceConnectionTest();
                if (testSuccess)
                    break;
                System.Threading.Thread.Sleep(1000);
            }

            ServiceClient.GetInstance().sendMessage("Abdulleyaayayreeymeeooooo!!1");
            ServiceClient.GetInstance().sendMessage(System.Environment.UserDomainName);
            ServiceClient.GetInstance().sendMessage(System.Environment.UserName);
            WindowsIdentity user = WindowsIdentity.GetCurrent();
            SecurityIdentifier sid = user.User;
            ServiceClient.GetInstance().sendMessage(sid.ToString());
            ServiceClient.GetInstance().sendMessage(user.Name);
            ServiceClient.GetInstance().sendMessage(Process.GetCurrentProcess().SessionId.ToString());
            
        }
    }
}
