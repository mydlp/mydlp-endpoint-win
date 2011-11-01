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
                //MessageBox.Show("USB storage id: " + id + " " + currentObject["Size"] + " " + currentObject["Model"]);


                if (id.StartsWith("USBSTOR"))
                {
                    String uniqID = id.Substring(id.LastIndexOf("\\") + 1);
                    //MessageBox.Show("USB storage uniq id: " + uniqID);
                    try
                    {
                        MD5 md5 = MD5.Create();
                        byte [] md5buf = md5.ComputeHash(ASCIIEncoding.ASCII.GetBytes(uniqID));

                        String idHash="";

                        foreach (byte b in md5buf)
                        {
                            idHash += b.ToString("X");
                        }

                        DataRow row = USBTable.NewRow();
                        row.SetField(Id, idHash);
                        row.SetField(Model, currentObject["Model"]);
                        USBTable.Rows.Add(row);
                        row.AcceptChanges();
                    }
                    catch (Exception ex)
                    {

                    }
                }
            }
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void dataGridView1_CellContentClick_1(object sender, DataGridViewCellEventArgs e)
        {

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
                    String uniqID = id.Substring(id.LastIndexOf("\\") + 1);
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
    }
}
