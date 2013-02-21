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
using System.Management;
using System.Collections;

namespace MyDLP.EndPoint.SessionAgent
{
    public partial class MainForm : Form
    {
        public static ProgressDialog pDialog;
        public static ServiceClient mainClient; 
        public static ServiceClient notificationClient;
        private Thread listenVolumeThread;
        private bool noMainWindow;

        public MainForm()
        {
            InitializeComponent();
            pDialog = new ProgressDialog();
        }               
           
        public void setStatus(String message)
        {
            statusLabel.Text = message;
        }

        private void ShowMaximized(object sender, MouseEventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.WindowState = FormWindowState.Minimized;
            FormUpdate();
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            FormUpdate();
        }

        private void FormUpdate()
        {
            //notifyIcon1.BalloonTipTitle = "Minimize to Tray App";
            //notifyIcon1.BalloonTipText = "You have successfully minimized your form.";

            if (FormWindowState.Minimized == this.WindowState)
            {
                //notifyIcon1.Visible = true;
                //notifyIcon1.ShowBalloonTip(500);
                this.Hide();
            }      
        }
        
        private void Form1_Load(object sender, EventArgs e)
        {
            mainClient = new ServiceClient();
            listenVolumeThread = new Thread(ProcessNewVolumes);
            listenVolumeThread.Start();
            notifyIcon1.Visible = true;
            this.Visible = false;
            aboutBox.Text = "MyDLP Endpoint Agent\r\nwww.mydlp.com";
        }
                
        private void ProcessNewVolumes()
        {
            Program.form.BeginInvoke(new Action(() => Program.form.setStatus("Waiting for security service"))); 
            try
            {        
                bool testSuccess = false;
                while(true)
                {
                    testSuccess = mainClient.ServiceConnectionTest();
                    if (testSuccess)
                        break;
                    System.Threading.Thread.Sleep(10000);
                }
                Program.form.BeginInvoke(new Action(() => Program.form.setStatus("Connected"))); 
                ListenUSBDisks();
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace);
            }
        }

        private void ListenUSBDisks()
        {
            try
            {
                ManagementEventWatcher watcher = new ManagementEventWatcher();
                WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_VolumeChangeEvent WHERE EventType = 2");
                RemovableEventHandler handler = new RemovableEventHandler();
                watcher.EventArrived += new EventArrivedEventHandler(handler.Arrived);
                watcher.Query = query;
                watcher.Start();
                Program.form.BeginInvoke(new Action(() => Program.form.setStatus("Running"))); 
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace);
            }
        }

        public class RemovableEventHandler
        {
            String driveName;
            public void Arrived(object sender, EventArrivedEventArgs e)
            {
                String resp = "";
                //Thread backgroundThread = new Thread(HandleNewVolume);
                driveName = e.NewEvent["DriveName"].ToString();
                //backgroundThread.Start();
                HandleNewVolume();
            }

            void HandleNewVolume()
            {                
                String resp;
                try
                {
                    resp = mainClient.sendMessage("NEWVOLUME " + driveName);
                    if (!resp.StartsWith("NEEDFORMAT"))
                    {
                        //volume is ok driveName nothing
                        return;
                    }

                    DialogResult dResult = MessageBox.Show("Attached drive " + driveName + " is insecure and can not be used." +
                    "It needs to be formatted and secured before usage. This will delete all information" +
                    "on disk and it will not be used on outside this organisation.", "MyDLP New Drive Encryption Alert", MessageBoxButtons.YesNo);

                    if (dResult == DialogResult.No)
                    {
                        MessageBox.Show("Drive " + driveName + " will not be formatted and can not be used.");
                        return;
                    }

                    Program.form.BeginInvoke(new Action(()=> pDialog.Visible= true));
                    resp = mainClient.sendMessage("FORMAT " + driveName);
                    Program.form.BeginInvoke(new Action(() => pDialog.Visible = false));           

                    if (!resp.StartsWith("FINISHED"))
                    {
                        MessageBox.Show("Unable to format drive!");
                        return;
                    }
                    MessageBox.Show("Drive " + driveName + " formatted");

                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + ex.StackTrace);
                }
            }
        }
    }
}
