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
            this.label2 = new System.Windows.Forms.Label();
            this.button_cancel = new System.Windows.Forms.Button();
            this.button_ok = new System.Windows.Forms.Button();
            this.listView_colors = new System.Windows.Forms.ListView();
            this.columnHeader_name = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_color = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_sample = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.columnHeader_button = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.button_clearDelimeterColor = new System.Windows.Forms.Button();
            this.marcControl_preview = new LibraryStudio.Forms.MarcControl();
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
            this.comboBox_colorTheme.SelectedIndexChanged += new System.EventHandler(this.comboBox_colorTheme_SelectedIndexChanged);
            this.comboBox_colorTheme.TextChanged += new System.EventHandler(this.comboBox_colorTheme_TextChanged);
            // 
            // label2
            // 
            this.label2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(6, 620);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(159, 36);
            this.label2.TabIndex = 3;
            this.label2.Text = "预览(&P):";
            // 
            // button_cancel
            // 
            this.button_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button_cancel.Location = new System.Drawing.Point(1166, 975);
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
            this.button_ok.Location = new System.Drawing.Point(920, 975);
            this.button_ok.Name = "button_ok";
            this.button_ok.Size = new System.Drawing.Size(240, 70);
            this.button_ok.TabIndex = 4;
            this.button_ok.Text = "确定";
            this.button_ok.UseVisualStyleBackColor = true;
            this.button_ok.Click += new System.EventHandler(this.button_ok_Click);
            // 
            // listView_colors
            // 
            this.listView_colors.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.listView_colors.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader_name,
            this.columnHeader_color,
            this.columnHeader_sample,
            this.columnHeader_button});
            this.listView_colors.FullRowSelect = true;
            this.listView_colors.HideSelection = false;
            this.listView_colors.Location = new System.Drawing.Point(12, 137);
            this.listView_colors.MultiSelect = false;
            this.listView_colors.Name = "listView_colors";
            this.listView_colors.OwnerDraw = true;
            this.listView_colors.Size = new System.Drawing.Size(1394, 445);
            this.listView_colors.TabIndex = 6;
            this.listView_colors.UseCompatibleStateImageBehavior = false;
            this.listView_colors.View = System.Windows.Forms.View.Details;
            this.listView_colors.DrawColumnHeader += new System.Windows.Forms.DrawListViewColumnHeaderEventHandler(this.listView_colors_DrawColumnHeader);
            this.listView_colors.DrawItem += new System.Windows.Forms.DrawListViewItemEventHandler(this.listView_colors_DrawItem);
            this.listView_colors.DrawSubItem += new System.Windows.Forms.DrawListViewSubItemEventHandler(this.listView_colors_DrawSubItem);
            this.listView_colors.MouseClick += new System.Windows.Forms.MouseEventHandler(this.listView_colors_MouseClick);
            this.listView_colors.MouseUp += new System.Windows.Forms.MouseEventHandler(this.listView_colors_MouseUp);
            // 
            // columnHeader_name
            // 
            this.columnHeader_name.Text = "部件";
            this.columnHeader_name.Width = 237;
            // 
            // columnHeader_color
            // 
            this.columnHeader_color.Text = "颜色";
            this.columnHeader_color.Width = 229;
            // 
            // columnHeader_sample
            // 
            this.columnHeader_sample.Text = "样本";
            this.columnHeader_sample.Width = 123;
            // 
            // columnHeader_button
            // 
            this.columnHeader_button.Text = "设置";
            this.columnHeader_button.Width = 123;
            // 
            // button_clearDelimeterColor
            // 
            this.button_clearDelimeterColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_clearDelimeterColor.Location = new System.Drawing.Point(1001, 588);
            this.button_clearDelimeterColor.Name = "button_clearDelimeterColor";
            this.button_clearDelimeterColor.Size = new System.Drawing.Size(405, 53);
            this.button_clearDelimeterColor.TabIndex = 7;
            this.button_clearDelimeterColor.Text = "清除分隔符颜色(&C)";
            this.button_clearDelimeterColor.UseVisualStyleBackColor = true;
            this.button_clearDelimeterColor.Click += new System.EventHandler(this.button_clearDelimeterColor_Click);
            // 
            // marcControl_preview
            // 
            this.marcControl_preview.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.marcControl_preview.AutoScroll = true;
            this.marcControl_preview.AutoScrollMinSize = new System.Drawing.Size(1381, 0);
            this.marcControl_preview.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.marcControl_preview.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.marcControl_preview.CaptionFont = new System.Drawing.Font("宋体", 9F);
            this.marcControl_preview.ClientBoundsWidth = 0;
            this.marcControl_preview.ColorThemeName = null;
            this.marcControl_preview.Content = "";
            this.marcControl_preview.DeleteKeyStyle = LibraryStudio.Forms.DeleteKeyStyle.DeleteFieldTerminator;
            this.marcControl_preview.FixedSizeFont = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Bold);
            this.marcControl_preview.HighlightBlankChar = ' ';
            this.marcControl_preview.Location = new System.Drawing.Point(12, 668);
            this.marcControl_preview.Name = "marcControl_preview";
            this.marcControl_preview.PaddingChar = ' ';
            this.marcControl_preview.PadWhileEditing = false;
            this.marcControl_preview.ReadOnly = false;
            this.marcControl_preview.Size = new System.Drawing.Size(1394, 288);
            this.marcControl_preview.TabIndex = 2;
            // 
            // VisualStyleDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(18F, 36F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1418, 1057);
            this.Controls.Add(this.button_clearDelimeterColor);
            this.Controls.Add(this.listView_colors);
            this.Controls.Add(this.button_cancel);
            this.Controls.Add(this.button_ok);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.marcControl_preview);
            this.Controls.Add(this.comboBox_colorTheme);
            this.Controls.Add(this.label1);
            this.MinimizeBox = false;
            this.Name = "VisualStyleDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "视觉风格";
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
        private System.Windows.Forms.ListView listView_colors;
        private System.Windows.Forms.ColumnHeader columnHeader_name;
        private System.Windows.Forms.ColumnHeader columnHeader_sample;
        private System.Windows.Forms.ColumnHeader columnHeader_button;
        private System.Windows.Forms.ColumnHeader columnHeader_color;
        private System.Windows.Forms.Button button_clearDelimeterColor;
    }
}
