namespace LibraryStudio.Forms.MarcControlDialog
{
    partial class VisualStyleDialog
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
            this.label1 = new System.Windows.Forms.Label();
            this.comboBox_colorTheme = new System.Windows.Forms.ComboBox();
            this.marcControl_preview = new LibraryStudio.Forms.MarcControl();
            this.label2 = new System.Windows.Forms.Label();
            this.button_cancel = new System.Windows.Forms.Button();
            this.button_ok = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(6, 63);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(231, 36);
            this.label1.TabIndex = 0;
            this.label1.Text = "颜色主题(&C):";
            // 
            // comboBox_colorTheme
            // 
            this.comboBox_colorTheme.FormattingEnabled = true;
            this.comboBox_colorTheme.Location = new System.Drawing.Point(279, 60);
            this.comboBox_colorTheme.Name = "comboBox_colorTheme";
            this.comboBox_colorTheme.Size = new System.Drawing.Size(638, 44);
            this.comboBox_colorTheme.TabIndex = 1;
            this.comboBox_colorTheme.TextChanged += new System.EventHandler(this.comboBox_colorTheme_TextChanged);
            // 
            // marcControl_preview
            // 
            this.marcControl_preview.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.marcControl_preview.AutoScroll = true;
            this.marcControl_preview.AutoScrollMinSize = new System.Drawing.Size(1370, 0);
            this.marcControl_preview.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.marcControl_preview.CaptionFont = new System.Drawing.Font("宋体", 9F);
            this.marcControl_preview.ClientBoundsWidth = 0;
            this.marcControl_preview.Content = "";
            this.marcControl_preview.DeleteKeyStyle = LibraryStudio.Forms.DeleteKeyStyle.DeleteFieldTerminator;
            this.marcControl_preview.FixedSizeFont = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Bold);
            this.marcControl_preview.HighlightBlankChar = ' ';
            this.marcControl_preview.Location = new System.Drawing.Point(12, 194);
            this.marcControl_preview.Name = "marcControl_preview";
            this.marcControl_preview.PaddingChar = ' ';
            this.marcControl_preview.PadWhileEditing = false;
            this.marcControl_preview.ReadOnly = false;
            this.marcControl_preview.Size = new System.Drawing.Size(1379, 578);
            this.marcControl_preview.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 145);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(159, 36);
            this.label2.TabIndex = 3;
            this.label2.Text = "预览(&P):";
            // 
            // button_cancel
            // 
            this.button_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button_cancel.Location = new System.Drawing.Point(1151, 791);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(240, 70);
            this.button_cancel.TabIndex = 5;
            this.button_cancel.Text = "取消";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // button_ok
            // 
            this.button_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_ok.Location = new System.Drawing.Point(905, 791);
            this.button_ok.Name = "button_ok";
            this.button_ok.Size = new System.Drawing.Size(240, 70);
            this.button_ok.TabIndex = 4;
            this.button_ok.Text = "确定";
            this.button_ok.UseVisualStyleBackColor = true;
            this.button_ok.Click += new System.EventHandler(this.button_ok_Click);
            // 
            // VisualStyleDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(18F, 36F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1403, 873);
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.button_ok);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.marcControl_preview);
            this.Controls.Add(this.comboBox_colorTheme);
            this.Controls.Add(this.label1);
            this.Name = "VisualStyleDialog";
            this.Text = "VisualStyleDialog";
            this.Load += new System.EventHandler(this.VisualStyleDialog_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox comboBox_colorTheme;
        private MarcControl marcControl_preview;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button_cancel;
        private System.Windows.Forms.Button button_ok;
    }
}
