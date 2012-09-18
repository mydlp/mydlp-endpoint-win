namespace MyDLP.EndPoint.Tools.DeviceConsole
{
    partial class DeviceConsoleForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(DeviceConsoleForm));
            this.getAttachedDevicesButton = new System.Windows.Forms.Button();
            this.dataSet = new System.Data.DataSet();
            this.USBTable = new System.Data.DataTable();
            this.Id = new System.Data.DataColumn();
            this.Model = new System.Data.DataColumn();
            this.Hash = new System.Data.DataColumn();
            this.Size = new System.Data.DataColumn();
            this.Pid = new System.Data.DataColumn();
            this.Vid = new System.Data.DataColumn();
            this.DevNode = new System.Data.DataColumn();
            this.UsBTableBindingSource = new System.Windows.Forms.BindingSource(this.components);
            this.dataGridView1 = new MyDLP.EndPoint.Tools.DeviceConsole.ReadOnlySelectableTextDataGridView();
            this.removeAttachedDevicesButton = new System.Windows.Forms.Button();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.openToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.panel1 = new System.Windows.Forms.Panel();
            this.sendToSeverButton = new System.Windows.Forms.Button();
            this.managementServerTextBox = new System.Windows.Forms.TextBox();
            this.managementServerLabel = new System.Windows.Forms.Label();
            this.hashDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.idDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.modelDataGridViewTextBoxColumn = new System.Windows.Forms.DataGridViewTextBoxColumn();
            ((System.ComponentModel.ISupportInitialize)(this.dataSet)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.USBTable)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.UsBTableBindingSource)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.statusStrip1.SuspendLayout();
            this.menuStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // getAttachedDevicesButton
            // 
            this.getAttachedDevicesButton.Location = new System.Drawing.Point(12, 117);
            this.getAttachedDevicesButton.Name = "getAttachedDevicesButton";
            this.getAttachedDevicesButton.Size = new System.Drawing.Size(159, 60);
            this.getAttachedDevicesButton.TabIndex = 0;
            this.getAttachedDevicesButton.Text = "Get Attached Devices";
            this.getAttachedDevicesButton.UseVisualStyleBackColor = true;
            this.getAttachedDevicesButton.Click += new System.EventHandler(this.getAttachedDevicesClick);
            // 
            // dataSet
            // 
            this.dataSet.DataSetName = "DeviceDataSet";
            this.dataSet.Tables.AddRange(new System.Data.DataTable[] {
            this.USBTable});
            // 
            // USBTable
            // 
            this.USBTable.Columns.AddRange(new System.Data.DataColumn[] {
            this.Id,
            this.Model,
            this.Hash,
            this.Size,
            this.Pid,
            this.Vid,
            this.DevNode});
            this.USBTable.Constraints.AddRange(new System.Data.Constraint[] {
            new System.Data.UniqueConstraint("Constraint1", new string[] {
                        "Hash"}, true)});
            this.USBTable.PrimaryKey = new System.Data.DataColumn[] {
        this.Hash};
            this.USBTable.TableName = "USBTable";
            // 
            // Id
            // 
            this.Id.AllowDBNull = false;
            this.Id.Caption = "Id";
            this.Id.ColumnName = "Id";
            // 
            // Model
            // 
            this.Model.Caption = "Model";
            this.Model.ColumnName = "Model";
            // 
            // Hash
            // 
            this.Hash.AllowDBNull = false;
            this.Hash.ColumnName = "Hash";
            // 
            // Size
            // 
            this.Size.Caption = "Size";
            this.Size.ColumnName = "Size";
            // 
            // Pid
            // 
            this.Pid.Caption = "Pid";
            this.Pid.ColumnName = "Pid";
            // 
            // Vid
            // 
            this.Vid.Caption = "Vid";
            this.Vid.ColumnName = "Vid";
            // 
            // DevNode
            // 
            this.DevNode.Caption = "DevNode";
            this.DevNode.ColumnName = "DevNode";
            // 
            // UsBTableBindingSource
            // 
            this.UsBTableBindingSource.DataMember = "USBTable";
            this.UsBTableBindingSource.DataSource = this.dataSet;
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToResizeColumns = false;
            this.dataGridView1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView1.AutoGenerateColumns = false;
            this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.ControlLight;
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.hashDataGridViewTextBoxColumn,
            this.idDataGridViewTextBoxColumn,
            this.dataGridViewTextBoxColumn3,
            this.modelDataGridViewTextBoxColumn});
            this.dataGridView1.DataSource = this.UsBTableBindingSource;
            this.dataGridView1.GridColor = System.Drawing.SystemColors.ControlDarkDark;
            this.dataGridView1.Location = new System.Drawing.Point(187, 27);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowTemplate.DefaultCellStyle.SelectionBackColor = System.Drawing.Color.White;
            this.dataGridView1.RowTemplate.DefaultCellStyle.SelectionForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(0)))), ((int)(((byte)(64)))));
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.CellSelect;
            this.dataGridView1.Size = new System.Drawing.Size(629, 341);
            this.dataGridView1.TabIndex = 1;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            // 
            // removeAttachedDevicesButton
            // 
            this.removeAttachedDevicesButton.Location = new System.Drawing.Point(12, 194);
            this.removeAttachedDevicesButton.Name = "removeAttachedDevicesButton";
            this.removeAttachedDevicesButton.Size = new System.Drawing.Size(159, 54);
            this.removeAttachedDevicesButton.TabIndex = 2;
            this.removeAttachedDevicesButton.Text = "Remove Attached Devices";
            this.removeAttachedDevicesButton.UseVisualStyleBackColor = true;
            this.removeAttachedDevicesButton.Click += new System.EventHandler(this.removeAttachedDevicesClick);
            // 
            // statusStrip1
            // 
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 371);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(828, 22);
            this.statusStrip1.TabIndex = 3;
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.BorderStyle = System.Windows.Forms.Border3DStyle.Etched;
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(813, 17);
            this.toolStripStatusLabel1.Spring = true;
            // 
            // menuStrip1
            // 
            this.menuStrip1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
            this.menuStrip1.Size = new System.Drawing.Size(828, 24);
            this.menuStrip1.TabIndex = 4;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openToolStripMenuItem,
            this.saveToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(37, 20);
            this.fileToolStripMenuItem.Text = "File";
            // 
            // openToolStripMenuItem
            // 
            this.openToolStripMenuItem.Name = "openToolStripMenuItem";
            this.openToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.openToolStripMenuItem.Text = "Open";
            this.openToolStripMenuItem.Click += new System.EventHandler(this.openToolStripMenuItem_Click);
            // 
            // saveToolStripMenuItem
            // 
            this.saveToolStripMenuItem.Name = "saveToolStripMenuItem";
            this.saveToolStripMenuItem.Size = new System.Drawing.Size(103, 22);
            this.saveToolStripMenuItem.Text = "Save";
            this.saveToolStripMenuItem.Click += new System.EventHandler(this.saveToolStripMenuItem_Click);
            // 
            // panel1
            // 
            this.panel1.BackgroundImage = ((System.Drawing.Image)(resources.GetObject("panel1.BackgroundImage")));
            this.panel1.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Stretch;
            this.panel1.Location = new System.Drawing.Point(12, 27);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(159, 75);
            this.panel1.TabIndex = 5;
            // 
            // sendToSeverButton
            // 
            this.sendToSeverButton.Location = new System.Drawing.Point(12, 309);
            this.sendToSeverButton.Name = "sendToSeverButton";
            this.sendToSeverButton.Size = new System.Drawing.Size(159, 46);
            this.sendToSeverButton.TabIndex = 6;
            this.sendToSeverButton.Text = "Send To Server";
            this.sendToSeverButton.UseVisualStyleBackColor = true;
            this.sendToSeverButton.Visible = false;
            this.sendToSeverButton.Click += new System.EventHandler(this.sendToServerClick);
            // 
            // managementServerTextBox
            // 
            this.managementServerTextBox.Location = new System.Drawing.Point(12, 276);
            this.managementServerTextBox.Name = "managementServerTextBox";
            this.managementServerTextBox.Size = new System.Drawing.Size(159, 20);
            this.managementServerTextBox.TabIndex = 7;
            this.managementServerTextBox.Visible = false;
            // 
            // managementServerLabel
            // 
            this.managementServerLabel.AutoSize = true;
            this.managementServerLabel.Location = new System.Drawing.Point(35, 260);
            this.managementServerLabel.Name = "managementServerLabel";
            this.managementServerLabel.Size = new System.Drawing.Size(103, 13);
            this.managementServerLabel.TabIndex = 8;
            this.managementServerLabel.Text = "Management Server";
            this.managementServerLabel.Visible = false;
            // 
            // hashDataGridViewTextBoxColumn
            // 
            this.hashDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.hashDataGridViewTextBoxColumn.DataPropertyName = "Hash";
            this.hashDataGridViewTextBoxColumn.HeaderText = "Device Token";
            this.hashDataGridViewTextBoxColumn.MinimumWidth = 200;
            this.hashDataGridViewTextBoxColumn.Name = "hashDataGridViewTextBoxColumn";
            this.hashDataGridViewTextBoxColumn.Width = 200;
            // 
            // idDataGridViewTextBoxColumn
            // 
            this.idDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.idDataGridViewTextBoxColumn.DataPropertyName = "Id";
            this.idDataGridViewTextBoxColumn.HeaderText = "Unique Id";
            this.idDataGridViewTextBoxColumn.MinimumWidth = 100;
            this.idDataGridViewTextBoxColumn.Name = "idDataGridViewTextBoxColumn";
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.dataGridViewTextBoxColumn3.DataPropertyName = "Size";
            this.dataGridViewTextBoxColumn3.HeaderText = "Size";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.ReadOnly = true;
            this.dataGridViewTextBoxColumn3.Width = 52;
            // 
            // modelDataGridViewTextBoxColumn
            // 
            this.modelDataGridViewTextBoxColumn.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.modelDataGridViewTextBoxColumn.DataPropertyName = "Model";
            this.modelDataGridViewTextBoxColumn.HeaderText = "Model";
            this.modelDataGridViewTextBoxColumn.MinimumWidth = 150;
            this.modelDataGridViewTextBoxColumn.Name = "modelDataGridViewTextBoxColumn";
            this.modelDataGridViewTextBoxColumn.ReadOnly = true;
            // 
            // DeviceConsoleForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(828, 393);
            this.Controls.Add(this.managementServerLabel);
            this.Controls.Add(this.managementServerTextBox);
            this.Controls.Add(this.sendToSeverButton);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Controls.Add(this.removeAttachedDevicesButton);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.getAttachedDevicesButton);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "DeviceConsoleForm";
            this.Text = "MyDLP Endpoint Device Console";
            this.Load += new System.EventHandler(this.DeviceConsoleForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.dataSet)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.USBTable)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.UsBTableBindingSource)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button getAttachedDevicesButton;
        private System.Data.DataSet dataSet;
        private System.Data.DataTable USBTable;
        private System.Data.DataColumn Id;
        private System.Data.DataColumn Model;
        private System.Windows.Forms.BindingSource UsBTableBindingSource;
        private System.Windows.Forms.Button removeAttachedDevicesButton;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem openToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveToolStripMenuItem;
        private System.Windows.Forms.Panel panel1;
        private System.Data.DataColumn Hash;
        private System.Windows.Forms.Button sendToSeverButton;
        private System.Data.DataColumn Size;
        private System.Windows.Forms.TextBox managementServerTextBox;
        private System.Windows.Forms.Label managementServerLabel;
        private System.Data.DataColumn Pid;
        private System.Data.DataColumn Vid;
        private System.Data.DataColumn DevNode;
        private ReadOnlySelectableTextDataGridView dataGridView1;
        private System.Windows.Forms.DataGridViewTextBoxColumn hashDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn idDataGridViewTextBoxColumn;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.DataGridViewTextBoxColumn modelDataGridViewTextBoxColumn;


    }
}

