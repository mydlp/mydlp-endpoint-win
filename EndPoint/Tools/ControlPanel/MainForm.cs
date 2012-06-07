using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;
using System.ServiceProcess;
using System.Security;

namespace MyDLP.EndPoint.Tools.ControlPanel
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadConf();
        }

        private void saveButton_Click(object sender, EventArgs e)
        {
            try
            {
                RegistryKey mydlpKey = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("MyDLP", true);
                mydlpKey.SetValue("management_server", textBox1.Text, RegistryValueKind.String);

                ServiceController service = new ServiceController("mydlpepwin");
                try
                {
                    TimeSpan timeout = TimeSpan.FromMilliseconds(2000);

                    if (service.Status.Equals(ServiceControllerStatus.Running))
                    {
                        service.Stop();
                        service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                        service.Start();
                    }
                }
                catch 
                {

                }
            }            
            catch (Exception)
            {
            
            }

        }

        private void LoadConf()
        {
            try
            {
                RegistryKey mydlpKey = Registry.LocalMachine.OpenSubKey("Software").OpenSubKey("MyDLP", true);
                
                int logLevel = (int) mydlpKey.GetValue("log_level");
                if (logLevel <= 0) 
                    textBox4.Text = "ERROR";
                else if (logLevel == 1) 
                    textBox4.Text = "INFO";
                else if (logLevel >= 2) 
                    textBox4.Text = "DEBUG";


                int maximumObjectSize = (int) mydlpKey.GetValue("maximum_object_size");
                if (maximumObjectSize < 1024)
                    textBox3.Text = maximumObjectSize + " B";
                if (1024 * 1024 >  maximumObjectSize && maximumObjectSize >= 1024)
                    textBox3.Text = maximumObjectSize / 1024 + " KB";
                else
                    textBox3.Text = maximumObjectSize / (1024 * 1024) + " MB";

                int logLimit = (int)mydlpKey.GetValue("log_limit");
                if (logLimit < 1024)
                    textBox2.Text = logLimit + " B";
                if (1024 * 1024 > logLimit && logLimit >= 1024)
                    textBox2.Text = logLimit / 1024 + " KB";
                else
                    textBox2.Text = logLimit / (1024 * 1024) + " MB";


                int printMonitor = (int)mydlpKey.GetValue("print_monitor");
                if (printMonitor == 1)
                    checkBox1.Checked = true;

                int archiveInbound = (int)mydlpKey.GetValue("archive_inbound");
                if (archiveInbound == 1)
                    checkBox2.Checked = true;


                int usbSac = (int)mydlpKey.GetValue("usb_serial_access_control");
                if (usbSac == 1)
                    checkBox3.Checked = true;


                String managementServer = (String)mydlpKey.GetValue("management_server");
                textBox1.Text = managementServer;
            }

            catch (SecurityException se)
            {
                MessageBox.Show("You do not have enough permission.");
                System.Environment.Exit(1);            
            }
            catch (Exception e)
            {
                
            }

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }
    }
     
}
