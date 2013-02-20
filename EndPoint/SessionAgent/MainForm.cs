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
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace MyDLP.EndPoint.SessionAgent
{
    public partial class MainForm : Form
    {
        public static ProgressDialog pDialog;
        public static ServiceClient mainClient = null;
        public static ServiceClient notificationClient;
        public static String format;

        private Thread listenVolumeThread;

        // For Windows Mobile, replace user32.dll with coredll.dll
        [DllImport("user32.dll", SetLastError = true)]
        static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        // Find window by Caption only. Note you must pass IntPtr.Zero as the first parameter.

        [DllImport("user32.dll", EntryPoint = "FindWindow", SetLastError = true)]
        static extern IntPtr FindWindowByCaption(IntPtr ZeroOnly, string lpWindowName);

        // You can also call FindWindow(default(string), lpWindowName) or FindWindow((string)null, lpWindowName)
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

        public const Int32 WM_SYSCOMMAND = 0x0112;
        public const Int32 SC_CLOSE = 0xF060;
        
        public static String connectLock;
        public static String handleLock;
        public static bool connecting = false;
        public static bool handling = false;


        public static bool ready = false;

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
            try
            {
                connectLock = "";
                handleLock = "";
                RegistryKey mydlpKey = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\MyDLP\");
                int usbstor_encryption = (int)mydlpKey.GetValue("usbstor_encryption", 0);
                if (usbstor_encryption == 0)
                {
                    Application.Exit();
                    return;
                }

                listenVolumeThread = new Thread(ProcessNewVolumes);
                listenVolumeThread.Start();
                notifyIcon1.Visible = true;
                this.Visible = false;
                aboutBox.Text = "MyDLP Endpoint Agent\r\nwww.mydlp.com";
               
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message + ex.StackTrace);
                Application.Exit();                
                //Unable to open registry nothing to do
            }
        }

        private void ProcessNewVolumes()
        {
            Thread listenUSBThread = new Thread(ListenUSBDisks);
            listenUSBThread.Start();
            ConnectOrWait();
        }

        public void ConnectOrWait()
        {
            /*if (connecting)
                return;
            lock (connectLock)
            {*/
                connecting = true;
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
                        ready = true;
                        break;
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message + e.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        continue;
                    }
                    finally 
                    {
                        connecting = false;
                    }
               // }
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


            private void TryClosingDialogs()
            {
                int i;
                IntPtr handle;

                for (i = 0; i < 10; i++)
                {
                    handle = FindWindow(null, "Microsoft Windows");
                    if (handle != null && handle != IntPtr.Zero)
                    {
                        SendMessage(handle, (uint)WM_SYSCOMMAND, (IntPtr)SC_CLOSE, IntPtr.Zero);
                        break;
                    }
                    else
                    {
                        Thread.Sleep(1000);
                    }
                }
            }

            void HandleNewVolume()
            {
                /*if (handling) 
                {
                    return;
                }
                lock (handleLock)
                {*/
                    String resp;
                    try
                    {
                        handling = true;
                        Thread closeThread = new Thread(TryClosingDialogs);
                        closeThread.Start();
                        if (!MainForm.ready)
                        {
                            MessageBox.Show("MyDLP Security Service is not ready. USB drives can not be used for now.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        }


                        resp = mainClient.sendMessage("NEWVOLUME " + driveName);
                        if (!resp.StartsWith("OK"))
                        {
                            mainClient.Reconnect();
                            handling = false;
                            return;
                        }

                        if (!resp.StartsWith("OK NEEDFORMAT"))
                        {
                            handling = false;
                            return;
                        }

                        DialogResult dResult = Program.form.GetFormat(driveName);

                        if (dResult == DialogResult.Cancel)
                        {
                            MessageBox.Show("Drive " + driveName + " will not be formatted and can not be used.", "Notification", MessageBoxButtons.OK, MessageBoxIcon.Information);
                            handling = false;
                            return;
                        }

                        Program.form.BeginInvoke(new Action(() => pDialog.Visible = true));
                        resp = mainClient.sendMessage("FORMAT " + driveName + " " + format);
                        Program.form.BeginInvoke(new Action(() => pDialog.Visible = false));

                        if (!resp.StartsWith("OK"))
                        {
                            mainClient.Reconnect();
                            handling = false;
                            return;
                        }

                        if (!resp.StartsWith("OK FINISHED"))
                        {
                            MessageBox.Show("Unable to format drive!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            handling = false;
                            return;
                        }
                        MessageBox.Show("Drive " + driveName + " formatted", "Format Completed", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1);
                    }
                    catch (Exception ex)
                    {
                        ready = false;
                        Program.form.ConnectOrWait();
                        //MessageBox.Show(ex.Message + ex.StackTrace, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally                     
                    {
                        handling = false;
                    }
                //}
            }
        }
    }
}
