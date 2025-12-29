namespace LibraryStudio.Forms.MarcControlDialog
{
    partial class DeletingFieldDialog
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
            this.button_ok = new System.Windows.Forms.Button();
            this.button_cancel = new System.Windows.Forms.Button();
            this.panel_preview = new System.Windows.Forms.Panel();
            this.label_message = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // button_ok
            // 
            this.button_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_ok.Location = new System.Drawing.Point(763, 514);
            this.button_ok.Name = "button_ok";
            this.button_ok.Size = new System.Drawing.Size(240, 70);
            this.button_ok.TabIndex = 0;
            this.button_ok.Text = "确定";
            this.button_ok.UseVisualStyleBackColor = true;
            this.button_ok.Click += new System.EventHandler(this.button_ok_Click);
            // 
            // button_cancel
            // 
            this.button_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button_cancel.Location = new System.Drawing.Point(1009, 514);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(240, 70);
            this.button_cancel.TabIndex = 1;
            this.button_cancel.Text = "取消";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // panel_preview
            // 
            this.panel_preview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.panel_preview.BackColor = System.Drawing.SystemColors.Window;
            this.panel_preview.Location = new System.Drawing.Point(8, 145);
            this.panel_preview.Name = "panel_preview";
            this.panel_preview.Size = new System.Drawing.Size(1241, 363);
            this.panel_preview.TabIndex = 2;
            this.panel_preview.Paint += new System.Windows.Forms.PaintEventHandler(this.panel_preview_Paint);
            // 
            // label_message
            // 
            this.label_message.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.label_message.Location = new System.Drawing.Point(8, 13);
            this.label_message.Name = "label_message";
            this.label_message.Size = new System.Drawing.Size(1241, 129);
            this.label_message.TabIndex = 3;
            this.label_message.Text = "确实要删除当前字段?";
            // 
            // DeletingFieldDialog
            // 
            this.AcceptButton = this.button_ok;
            this.AutoScaleDimensions = new System.Drawing.SizeF(18F, 36F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button_cancel;
            this.ClientSize = new System.Drawing.Size(1261, 596);
            this.Controls.Add(this.label_message);
            this.Controls.Add(this.panel_preview);
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.button_ok);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "DeletingFieldDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "删除字段";
            this.Load += new System.EventHandler(this.DeletingFieldDialog_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button button_ok;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.Panel panel_preview;
        private System.Windows.Forms.Label label_message;
    }
}
