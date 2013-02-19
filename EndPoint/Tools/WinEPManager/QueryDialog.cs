using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.DirectoryServices;

namespace MyDLP.EndPoint.Tools.WinEPManager
{
    public partial class QueryDialog : Form
    {
        public QueryDialog()
        {
            InitializeComponent();
        }

        private void QueryDialog_Load(object sender, EventArgs e)
        {
            DirectoryEntry de = new DirectoryEntry("LDAP://" + System.Environment.UserDomainName);
            de.Children.SchemaFilter.Add("computer");
            foreach (DirectoryEntry c in de.Children)
            {
                MessageBox.Show(c.Name);
            }

        }
    }
}
