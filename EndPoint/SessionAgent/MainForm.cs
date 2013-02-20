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
        public static ServiceClient mainClient = null;
        public static ServiceClient notificationClient;
        public static String format;
        private Thread listenVolumeThread;

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

        public DialogResult GetFormat(String driveName)
        {
            return new FormatDialog(driveName).ShowDialog();
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
            listenVolumeThread = new Thread(ProcessNewVolumes);
            listenVolumeThread.Start();
            notifyIcon1.Visible = true;
            this.Visible = false;
            aboutBox.Text = "MyDLP Endpoint Agent\r\nwww.mydlp.com";
        }

        private void ProcessNewVolumes()
        {
            ConnectOrWait();
            ListenUSBDisks();
        }

        public void ConnectOrWait()
        {
            String response;
            while (true)
            {              
            tryagain: 
                Program.form.BeginInvoke(new Action(() => Program.form.setStatus("Waiting for security service")));
                try
                {                 

                    while (true)
                    {
                        try
                        {
                            if (mainClient == null)
                            {
                                mainClient = new ServiceClient();
                            }
                            else
                            {
                                mainClient.Reconnect();
                            }
                            
                        }
                        catch (Exception e)
                        {
                            System.Threading.Thread.Sleep(10000);
                            continue;
                        }

                        response = mainClient.sendMessage("BEGIN");
                        if (response.StartsWith("OK"))
                        {
                            break;
                        }

                        System.Threading.Thread.Sleep(10000);
                    }
                    Program.form.BeginInvoke(new Action(() => Program.form.setStatus("Connected, waiting server")));

                    while (true)
                    {
                        response = mainClient.sendMessage("HASKEY");

                        if (!response.StartsWith("OK"))
                        {
                            System.Threading.Thread.Sleep(10000);
                            goto tryagain;
                        }

                        if (response.StartsWith("OK YES"))
                        {
                            break;
                        }
                        System.Threading.Thread.Sleep(10000);
                    }
                 
                    Program.form.BeginInvoke(new Action(() => Program.form.setStatus("Running")));
                    break;
                }
                catch (Exception e)
                {
                    MessageBox.Show(e.Message + e.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);                
                    continue;
                }                                               
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
                MessageBox.Show(e.Message + e.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        public class RemovableEventHandler
        {
            String driveName;
            public void Arrived(object sender, EventArrivedEventArgs e)
            {
                driveName = e.NewEvent["DriveName"].ToString().Replace(":", "");
                HandleNewVolume();
            }

            void HandleNewVolume()
            {
                String resp;
                try
                {
                    resp = mainClient.sendMessage("NEWVOLUME " + driveName);
                    if (!resp.StartsWith("OK"))
                    {
                        mainClient.Reconnect();
                        return;
                    }

                    if (!resp.StartsWith("OK NEEDFORMAT"))
                    {
                        return;
                    }

                    DialogResult dResult = Program.form.GetFormat(driveName);

                    if (dResult == DialogResult.Cancel)
                    {
                        MessageBox.Show("Drive " + driveName + " will not be formatted and can not be used.", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    Program.form.BeginInvoke(new Action(() => pDialog.Visible = true));
                    resp = mainClient.sendMessage("FORMAT " + driveName + " " + format);
                    Program.form.BeginInvoke(new Action(() => pDialog.Visible = false));

                    if (!resp.StartsWith("OK"))
                    {
                        mainClient.Reconnect();
                        return;
                    }

                    if (!resp.StartsWith("OK FINISHED"))
                    {
                        MessageBox.Show("Unable to format drive!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                    MessageBox.Show("Drive " + driveName + " formatted", "Format Completed", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                }
                catch (Exception ex)
                {
                    Program.form.ConnectOrWait();
                    MessageBox.Show(ex.Message + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
    }
}
