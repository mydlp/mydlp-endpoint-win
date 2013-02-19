namespace MyDLP.EndPoint.SessionAgent
{
    partial class ProgressDialog
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
            this.encryptionProgressBar = new System.Windows.Forms.ProgressBar();
            this.description = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // encryptionProgressBar
            // 
            this.encryptionProgressBar.Location = new System.Drawing.Point(12, 25);
            this.encryptionProgressBar.Name = "encryptionProgressBar";
            this.encryptionProgressBar.Size = new System.Drawing.Size(493, 23);
            this.encryptionProgressBar.Style = System.Windows.Forms.ProgressBarStyle.Marquee;
            this.encryptionProgressBar.TabIndex = 0;
            // 
            // description
            // 
            this.description.AutoSize = true;
            this.description.Location = new System.Drawing.Point(9, 9);
            this.description.Name = "description";
            this.description.Size = new System.Drawing.Size(91, 13);
            this.description.TabIndex = 1;
            this.description.Text = "Formating drive ...";
            // 
            // ProgressDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(517, 58);
            this.ControlBox = false;
            this.Controls.Add(this.description);
            this.Controls.Add(this.encryptionProgressBar);
            this.Name = "ProgressDialog";
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MyDLP Drive Encryption";
            this.TopMost = true;
            this.Load += new System.EventHandler(this.ProgressDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.ProgressBar encryptionProgressBar;
        private System.Windows.Forms.Label description;
    }
}