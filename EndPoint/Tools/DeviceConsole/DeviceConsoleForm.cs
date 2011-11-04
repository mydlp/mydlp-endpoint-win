using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Management;
using System.Security.Cryptography;
using Microsoft.Win32;

namespace MyDLP.EndPoint.Tools.DeviceConsole
{
    public partial class DeviceConsoleForm : Form
    {
        public DeviceConsoleForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {

            ManagementObjectSearcher theSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
            foreach (ManagementObject currentObject in theSearcher.Get())
            {
                String id = currentObject["PNPDeviceID"].ToString();
                String pid = "";
                String vid= "";

                if (id.StartsWith("USBSTOR"))
                {
                    int start = id.LastIndexOf("\\") + 1;
                    int lentgh = id.LastIndexOf("&") - start;
                    String uniqID = id.Substring(start, lentgh);
                    //MessageBox.Show("USB storage uniq id: " + uniqID);

                    RegistryKey enumUSBKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\USBSTOR\Enum");
                    int count = (int)enumUSBKey.GetValue("Count");

                    for (int i = 0; i < count; i++)
                    {
                        String usbDeviceString = (String)enumUSBKey.GetValue(i.ToString());
                        if (usbDeviceString.Contains(uniqID)) 
                        {
                            int startVid = usbDeviceString.IndexOf("Vid_") + 4;
                            int endVid = usbDeviceString.IndexOf("&",startVid);
                            vid = usbDeviceString.Substring(startVid, endVid - startVid);
                            int endPid = usbDeviceString.IndexOf("\\", endVid + 1);
                            pid = usbDeviceString.Substring(endVid + 5, endPid - endVid - 5);

                        }
                    }

                    RegistryKey enumUSBDevKey = Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB");
                    String devNode = "";
                    foreach (String devNodeKeyString in enumUSBDevKey.GetSubKeyNames())
                    {
                        foreach (String devNodeInstanceString in enumUSBDevKey.OpenSubKey(devNodeKeyString).GetSubKeyNames())
                        {
                            if (devNodeInstanceString == uniqID)
                            {
                                devNode = devNodeKeyString;
                                break;
                            }
                        }
                    }

                    try
                    {
                        MD5 md5 = MD5.Create();
                        byte[] md5buf = md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(uniqID));

                        String idHash = "";

                        foreach (byte b in md5buf)
                        {
                            idHash += b.ToString("X");
                        }

                        DataRow row = USBTable.NewRow();
                        row.SetField(Hash, idHash);
                        row.SetField(Id, uniqID);
                        row.SetField(Pid, pid);
                        row.SetField(Vid, vid);
                        row.SetField(Model, currentObject["Model"]);
                        row.SetField(Comment, "");
                        row.SetField(DevNode, devNode);
                        USBTable.Rows.Add(row);
                        row.AcceptChanges();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            ManagementObjectSearcher theSearcher = new ManagementObjectSearcher("SELECT * FROM Win32_DiskDrive WHERE InterfaceType='USB'");
            foreach (ManagementObject currentObject in theSearcher.Get())
            {
                String id = currentObject["PNPDeviceID"].ToString();
                //MessageBox.Show("USB storage id: " + id + " " + currentObject["Size"] + " " + currentObject["Model"]);


                if (id.StartsWith("USBSTOR"))
                {
                    int start = id.LastIndexOf("\\") + 1;
                    int lentgh = id.LastIndexOf("&") - start;
                    String uniqID = id.Substring(start, lentgh);
                    //MessageBox.Show("USB storage uniq id: " + uniqID);
                    try
                    {
                        MD5 md5 = MD5.Create();
                        byte[] md5buf = md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(uniqID));

                        String idHash = "";

                        foreach (byte b in md5buf)
                        {
                            idHash += b.ToString("X");
                        }
                        USBTable.Rows.Remove(USBTable.Rows.Find(idHash));
                        USBTable.AcceptChanges();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "devices";
            saveFileDialog.Filter = "Xml (*.xml)|*.xml";
            saveFileDialog.ShowDialog();
            try
            {
                if (saveFileDialog.FileName != "")
                    dataSet.WriteXml(saveFileDialog.FileName);
            }
            catch (Exception ex)
            {

            }
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Xml (*.xml)|*.xml";
            openFileDialog.ShowDialog();
            try
            {
                if (openFileDialog.FileName != "")
                    dataSet.ReadXml(openFileDialog.FileName);
            }
            catch (Exception ex)
            {

            }
        }

        private void exportToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.ShowDialog();
            try
            {
                if (saveFileDialog.FileName != "")
                {
                    System.IO.StreamWriter file = new System.IO.StreamWriter(saveFileDialog.FileName);

                    foreach (DataRow row in USBTable.Rows)
                    {
                        file.WriteLine(row[Id].ToString());
                    }

                    file.Close();
                }

            }
            catch (Exception ex)
            {

            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            String managementServer = "";
            RegistryKey mydlpKey;
            try
            {
                mydlpKey = Registry.LocalMachine.OpenSubKey("Software", true).CreateSubKey("MyDLP");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Registry mydlp key error: " + ex.Message);
                return;
            }

            try
            {
                managementServer = (String)mydlpKey.GetValue("ManagementServer", "");
                if (managementServer == "" || managementServer != managementServerTextBox.Text)
                {
                    mydlpKey.SetValue("ManagementServer", managementServerTextBox.Text);
                    managementServer = managementServerTextBox.Text;
                }
            }
            catch
            {
                MessageBox.Show("Registry management server error");
            }

            try
            {
                foreach (DataRow row in USBTable.Rows)
                {
                    //MessageBox.Show(managementServer + row[Hash].ToString() + " " +row[Id].ToString() + " " + row[Comment].ToString() + " "+ row[Model].ToString());
                    HTTPUtil.notifyServer(managementServer, row[Hash].ToString(), row[Id].ToString(), row[Comment].ToString(), row[Model].ToString());

                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void DeviceConsoleForm_Load(object sender, EventArgs e)
        {
            String managementServer;
            RegistryKey mydlpKey;
            try
            {
                mydlpKey = Registry.LocalMachine.OpenSubKey("Software", true).CreateSubKey("MyDLP");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Registry mydlp key error: " + ex.Message);
                return;
            }

            try
            {
                managementServer = (String)mydlpKey.GetValue("ManagementServer", "");
                if (managementServer == "")
                {
                    mydlpKey.SetValue("ManagementServer", "10.0.0.1");
                    managementServer = "10.0.0.1";
                }
            }
            catch
            {
                MessageBox.Show("Registry management server error");
                return;
            }
            managementServerTextBox.Text = managementServer;
        }
    }
}
