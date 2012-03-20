using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace MyDLP.EndPoint.Tools.DeviceConsole
{
    class ReadOnlySelectableTextDataGridView : DataGridView
    {

        public ReadOnlySelectableTextDataGridView()
        {
            this.EditingControlShowing += new DataGridViewEditingControlShowingEventHandler(ReadOnlySelectableTextDataGridViewEditingControlShowing);
        }

        void ReadOnlySelectableTextDataGridViewEditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {   
            TextBox textBoxCell = (TextBox)e.Control;
            textBoxCell.ReadOnly = true;
        }
    }
}
