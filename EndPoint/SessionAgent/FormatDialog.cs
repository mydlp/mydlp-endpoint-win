using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MyDLP.EndPoint.SessionAgent
{
    public partial class FormatDialog : Form
    {
        public String driveName;
        public FormatDialog()
        {
            InitializeComponent();
            fat32RadioButton.Checked = true;
            descriptionTextBox.Text = "Attached drive " + driveName + " is insecure and can not be used." 
                + Environment.NewLine
                + "It needs to be formatted and secured before usage. This will delete all information"
                + Environment.NewLine 
                + "on disk and it will not be used on outside this organisation.";
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            if (fat32RadioButton.Checked)
            {
                MainForm.format = "fat32";
            }
            if (exFatRadioButton.Checked)
            {
                MainForm.format = "exfat";
            }
            if (ntfsRadioButton.Checked)
            {
                MainForm.format = "ntfs";
            }            
            this.DialogResult = DialogResult.OK;
        }

        private void cancelButton_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
        }
    }
}
