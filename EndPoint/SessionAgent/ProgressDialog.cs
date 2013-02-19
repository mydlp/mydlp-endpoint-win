using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace MyDLP.EndPoint.SessionAgent
{
    public partial class ProgressDialog : Form
    {
        BackgroundWorker backgroundWorker1;
        public ProgressDialog()
        {
            InitializeComponent();
            backgroundWorker1 = new BackgroundWorker();
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            for (int i = 1; i <= 100; i++)
            {
                // Wait 100 milliseconds.
                Thread.Sleep(100);
                // Report progress.
                backgroundWorker1.ReportProgress(i);
            }
        }

        private void ProgressDialog_Load(object sender, EventArgs e)
        {
            // Start the BackgroundWorker.
            backgroundWorker1.RunWorkerAsync();
        }
    }
}
