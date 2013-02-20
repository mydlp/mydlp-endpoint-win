namespace MyDLP.EndPoint.SessionAgent
{
    partial class FormatDialog
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
            this.okButton = new System.Windows.Forms.Button();
            this.cancelButton = new System.Windows.Forms.Button();
            this.descriptionTextBox = new System.Windows.Forms.TextBox();
            this.formatLabel = new System.Windows.Forms.Label();
            this.fat32RadioButton = new System.Windows.Forms.RadioButton();
            this.exFatRadioButton = new System.Windows.Forms.RadioButton();
            this.ntfsRadioButton = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // okButton
            // 
            this.okButton.Location = new System.Drawing.Point(336, 118);
            this.okButton.Name = "okButton";
            this.okButton.Size = new System.Drawing.Size(99, 23);
            this.okButton.TabIndex = 0;
            this.okButton.Text = "OK";
            this.okButton.UseVisualStyleBackColor = true;
            this.okButton.Click += new System.EventHandler(this.okButton_Click);
            // 
            // cancelButton
            // 
            this.cancelButton.Location = new System.Drawing.Point(441, 118);
            this.cancelButton.Name = "cancelButton";
            this.cancelButton.Size = new System.Drawing.Size(100, 23);
            this.cancelButton.TabIndex = 1;
            this.cancelButton.Text = "Cancel";
            this.cancelButton.UseVisualStyleBackColor = true;
            this.cancelButton.Click += new System.EventHandler(this.cancelButton_Click);
            // 
            // descriptionTextBox
            // 
            this.descriptionTextBox.Location = new System.Drawing.Point(13, 13);
            this.descriptionTextBox.Multiline = true;
            this.descriptionTextBox.Name = "descriptionTextBox";
            this.descriptionTextBox.ReadOnly = true;
            this.descriptionTextBox.Size = new System.Drawing.Size(528, 59);
            this.descriptionTextBox.TabIndex = 2;
            // 
            // formatLabel
            // 
            this.formatLabel.AutoSize = true;
            this.formatLabel.Location = new System.Drawing.Point(10, 87);
            this.formatLabel.Name = "formatLabel";
            this.formatLabel.Size = new System.Drawing.Size(75, 13);
            this.formatLabel.TabIndex = 3;
            this.formatLabel.Text = "Select Format:";
            // 
            // fat32RadioButton
            // 
            this.fat32RadioButton.AutoSize = true;
            this.fat32RadioButton.Location = new System.Drawing.Point(94, 85);
            this.fat32RadioButton.Name = "fat32RadioButton";
            this.fat32RadioButton.Size = new System.Drawing.Size(112, 17);
            this.fat32RadioButton.TabIndex = 4;
            this.fat32RadioButton.TabStop = true;
            this.fat32RadioButton.Text = "FAT32 ( max 4GB)";
            this.fat32RadioButton.UseVisualStyleBackColor = true;
            // 
            // exFatRadioButton
            // 
            this.exFatRadioButton.AutoSize = true;
            this.exFatRadioButton.Location = new System.Drawing.Point(212, 85);
            this.exFatRadioButton.Name = "exFatRadioButton";
            this.exFatRadioButton.Size = new System.Drawing.Size(56, 17);
            this.exFatRadioButton.TabIndex = 5;
            this.exFatRadioButton.TabStop = true;
            this.exFatRadioButton.Text = "exFAT";
            this.exFatRadioButton.UseVisualStyleBackColor = true;
            // 
            // ntfsRadioButton
            // 
            this.ntfsRadioButton.AutoSize = true;
            this.ntfsRadioButton.Location = new System.Drawing.Point(274, 85);
            this.ntfsRadioButton.Name = "ntfsRadioButton";
            this.ntfsRadioButton.Size = new System.Drawing.Size(53, 17);
            this.ntfsRadioButton.TabIndex = 6;
            this.ntfsRadioButton.TabStop = true;
            this.ntfsRadioButton.Text = "NTFS";
            this.ntfsRadioButton.UseVisualStyleBackColor = true;
            // 
            // FormatDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(548, 153);
            this.ControlBox = false;
            this.Controls.Add(this.ntfsRadioButton);
            this.Controls.Add(this.exFatRadioButton);
            this.Controls.Add(this.fat32RadioButton);
            this.Controls.Add(this.formatLabel);
            this.Controls.Add(this.descriptionTextBox);
            this.Controls.Add(this.cancelButton);
            this.Controls.Add(this.okButton);
            this.MaximumSize = new System.Drawing.Size(564, 191);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(564, 191);
            this.Name = "FormatDialog";
            this.SizeGripStyle = System.Windows.Forms.SizeGripStyle.Hide;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "FormatDialog";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button okButton;
        private System.Windows.Forms.Button cancelButton;
        private System.Windows.Forms.TextBox descriptionTextBox;
        private System.Windows.Forms.Label formatLabel;
        private System.Windows.Forms.RadioButton fat32RadioButton;
        private System.Windows.Forms.RadioButton exFatRadioButton;
        private System.Windows.Forms.RadioButton ntfsRadioButton;
    }
}