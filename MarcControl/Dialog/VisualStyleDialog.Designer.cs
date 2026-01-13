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
            this.splitContainer1 = new System.Windows.Forms.SplitContainer();
            this.tabControl_colorTheme = new System.Windows.Forms.TabControl();
            this.tabPage_colorTheme = new System.Windows.Forms.TabPage();
            this.tableLayoutPanel_colorTheme = new System.Windows.Forms.TableLayoutPanel();
            this.panel_color_theme_list = new System.Windows.Forms.Panel();
            this.tabPage_font = new System.Windows.Forms.TabPage();
            this.button_font_reset = new System.Windows.Forms.Button();
            this.button_font_caption = new System.Windows.Forms.Button();
            this.textBox_font_caption = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.button_font_fixed = new System.Windows.Forms.Button();
            this.textBox_font_fixed = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.button_font_content = new System.Windows.Forms.Button();
            this.textBox_font_content = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.marcControl_preview = new LibraryStudio.Forms.MarcControl();
            this.tableLayoutPanel_main = new System.Windows.Forms.TableLayoutPanel();
            this.panel_ok_cancel = new System.Windows.Forms.Panel();
            this.tableLayoutPanel_marcPreview = new System.Windows.Forms.TableLayoutPanel();
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).BeginInit();
            this.splitContainer1.Panel1.SuspendLayout();
            this.splitContainer1.Panel2.SuspendLayout();
            this.splitContainer1.SuspendLayout();
            this.tabControl_colorTheme.SuspendLayout();
            this.tabPage_colorTheme.SuspendLayout();
            this.tableLayoutPanel_colorTheme.SuspendLayout();
            this.panel_color_theme_list.SuspendLayout();
            this.tabPage_font.SuspendLayout();
            this.tableLayoutPanel_main.SuspendLayout();
            this.panel_ok_cancel.SuspendLayout();
            this.tableLayoutPanel_marcPreview.SuspendLayout();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(-5, 6);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(193, 30);
            this.label1.TabIndex = 0;
            this.label1.Text = "颜色主题(&C):";
            // 
            // comboBox_colorTheme
            // 
            this.comboBox_colorTheme.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.comboBox_colorTheme.FormattingEnabled = true;
            this.comboBox_colorTheme.Location = new System.Drawing.Point(184, 3);
            this.comboBox_colorTheme.Name = "comboBox_colorTheme";
            this.comboBox_colorTheme.Size = new System.Drawing.Size(434, 38);
            this.comboBox_colorTheme.TabIndex = 1;
            this.comboBox_colorTheme.SelectedIndexChanged += new System.EventHandler(this.comboBox_colorTheme_SelectedIndexChanged);
            this.comboBox_colorTheme.TextChanged += new System.EventHandler(this.comboBox_colorTheme_TextChanged);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(3, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(133, 30);
            this.label2.TabIndex = 3;
            this.label2.Text = "预览(&P):";
            // 
            // button_cancel
            // 
            this.button_cancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_cancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button_cancel.Location = new System.Drawing.Point(867, 1);
            this.button_cancel.Name = "button_cancel";
            this.button_cancel.Size = new System.Drawing.Size(200, 59);
            this.button_cancel.TabIndex = 5;
            this.button_cancel.Text = "取消";
            this.button_cancel.UseVisualStyleBackColor = true;
            this.button_cancel.Click += new System.EventHandler(this.button_cancel_Click);
            // 
            // button_ok
            // 
            this.button_ok.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button_ok.Location = new System.Drawing.Point(664, 0);
            this.button_ok.Name = "button_ok";
            this.button_ok.Size = new System.Drawing.Size(200, 59);
            this.button_ok.TabIndex = 4;
            this.button_ok.Text = "确定";
            this.button_ok.UseVisualStyleBackColor = true;
            this.button_ok.Click += new System.EventHandler(this.button_ok_Click);
            // 
            // listView_colors
            // 
            this.listView_colors.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader_name,
            this.columnHeader_color,
            this.columnHeader_sample,
            this.columnHeader_button});
            this.listView_colors.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView_colors.FullRowSelect = true;
            this.listView_colors.HideSelection = false;
            this.listView_colors.Location = new System.Drawing.Point(3, 55);
            this.listView_colors.MultiSelect = false;
            this.listView_colors.Name = "listView_colors";
            this.listView_colors.OwnerDraw = true;
            this.listView_colors.Size = new System.Drawing.Size(1037, 234);
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
            this.button_clearDelimeterColor.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.button_clearDelimeterColor.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.button_clearDelimeterColor.Location = new System.Drawing.Point(693, 6);
            this.button_clearDelimeterColor.Name = "button_clearDelimeterColor";
            this.button_clearDelimeterColor.Size = new System.Drawing.Size(338, 37);
            this.button_clearDelimeterColor.TabIndex = 7;
            this.button_clearDelimeterColor.Text = "清除分隔符颜色(&C)";
            this.button_clearDelimeterColor.UseVisualStyleBackColor = true;
            this.button_clearDelimeterColor.Click += new System.EventHandler(this.button_clearDelimeterColor_Click);
            // 
            // splitContainer1
            // 
            this.splitContainer1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.splitContainer1.Location = new System.Drawing.Point(3, 3);
            this.splitContainer1.Name = "splitContainer1";
            this.splitContainer1.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitContainer1.Panel1
            // 
            this.splitContainer1.Panel1.Controls.Add(this.tabControl_colorTheme);
            // 
            // splitContainer1.Panel2
            // 
            this.splitContainer1.Panel2.Controls.Add(this.tableLayoutPanel_marcPreview);
            this.splitContainer1.Size = new System.Drawing.Size(1069, 758);
            this.splitContainer1.SplitterDistance = 356;
            this.splitContainer1.SplitterWidth = 7;
            this.splitContainer1.TabIndex = 8;
            // 
            // tabControl_colorTheme
            // 
            this.tabControl_colorTheme.Controls.Add(this.tabPage_colorTheme);
            this.tabControl_colorTheme.Controls.Add(this.tabPage_font);
            this.tabControl_colorTheme.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl_colorTheme.Location = new System.Drawing.Point(0, 0);
            this.tabControl_colorTheme.Name = "tabControl_colorTheme";
            this.tabControl_colorTheme.SelectedIndex = 0;
            this.tabControl_colorTheme.Size = new System.Drawing.Size(1069, 356);
            this.tabControl_colorTheme.TabIndex = 0;
            // 
            // tabPage_colorTheme
            // 
            this.tabPage_colorTheme.Controls.Add(this.tableLayoutPanel_colorTheme);
            this.tabPage_colorTheme.Location = new System.Drawing.Point(10, 48);
            this.tabPage_colorTheme.Name = "tabPage_colorTheme";
            this.tabPage_colorTheme.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.tabPage_colorTheme.Size = new System.Drawing.Size(1049, 298);
            this.tabPage_colorTheme.TabIndex = 0;
            this.tabPage_colorTheme.Text = "颜色主题";
            this.tabPage_colorTheme.UseVisualStyleBackColor = true;
            // 
            // tableLayoutPanel_colorTheme
            // 
            this.tableLayoutPanel_colorTheme.ColumnCount = 1;
            this.tableLayoutPanel_colorTheme.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_colorTheme.Controls.Add(this.listView_colors, 0, 1);
            this.tableLayoutPanel_colorTheme.Controls.Add(this.panel_color_theme_list, 0, 0);
            this.tableLayoutPanel_colorTheme.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel_colorTheme.Location = new System.Drawing.Point(3, 3);
            this.tableLayoutPanel_colorTheme.Name = "tableLayoutPanel_colorTheme";
            this.tableLayoutPanel_colorTheme.RowCount = 3;
            this.tableLayoutPanel_colorTheme.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel_colorTheme.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_colorTheme.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel_colorTheme.Size = new System.Drawing.Size(1043, 292);
            this.tableLayoutPanel_colorTheme.TabIndex = 10;
            // 
            // panel_color_theme_list
            // 
            this.panel_color_theme_list.AutoSize = true;
            this.panel_color_theme_list.Controls.Add(this.button_clearDelimeterColor);
            this.panel_color_theme_list.Controls.Add(this.comboBox_colorTheme);
            this.panel_color_theme_list.Controls.Add(this.label1);
            this.panel_color_theme_list.Location = new System.Drawing.Point(3, 3);
            this.panel_color_theme_list.Name = "panel_color_theme_list";
            this.panel_color_theme_list.Size = new System.Drawing.Size(1031, 46);
            this.panel_color_theme_list.TabIndex = 9;
            // 
            // tabPage_font
            // 
            this.tabPage_font.AutoScroll = true;
            this.tabPage_font.Controls.Add(this.button_font_reset);
            this.tabPage_font.Controls.Add(this.button_font_caption);
            this.tabPage_font.Controls.Add(this.textBox_font_caption);
            this.tabPage_font.Controls.Add(this.label5);
            this.tabPage_font.Controls.Add(this.button_font_fixed);
            this.tabPage_font.Controls.Add(this.textBox_font_fixed);
            this.tabPage_font.Controls.Add(this.label4);
            this.tabPage_font.Controls.Add(this.button_font_content);
            this.tabPage_font.Controls.Add(this.textBox_font_content);
            this.tabPage_font.Controls.Add(this.label3);
            this.tabPage_font.Location = new System.Drawing.Point(10, 48);
            this.tabPage_font.Name = "tabPage_font";
            this.tabPage_font.Padding = new System.Windows.Forms.Padding(3, 3, 3, 3);
            this.tabPage_font.Size = new System.Drawing.Size(1049, 299);
            this.tabPage_font.TabIndex = 1;
            this.tabPage_font.Text = "字体";
            this.tabPage_font.UseVisualStyleBackColor = true;
            // 
            // button_font_reset
            // 
            this.button_font_reset.Location = new System.Drawing.Point(23, 213);
            this.button_font_reset.Name = "button_font_reset";
            this.button_font_reset.Size = new System.Drawing.Size(360, 51);
            this.button_font_reset.TabIndex = 9;
            this.button_font_reset.Text = "恢复默认字体(&D)";
            this.button_font_reset.UseVisualStyleBackColor = true;
            this.button_font_reset.Click += new System.EventHandler(this.button_font_reset_Click);
            // 
            // button_font_caption
            // 
            this.button_font_caption.Location = new System.Drawing.Point(799, 156);
            this.button_font_caption.Name = "button_font_caption";
            this.button_font_caption.Size = new System.Drawing.Size(164, 41);
            this.button_font_caption.TabIndex = 8;
            this.button_font_caption.Text = "设置 ...";
            this.button_font_caption.UseVisualStyleBackColor = true;
            this.button_font_caption.Click += new System.EventHandler(this.button_font_caption_Click);
            // 
            // textBox_font_caption
            // 
            this.textBox_font_caption.Location = new System.Drawing.Point(226, 153);
            this.textBox_font_caption.Name = "textBox_font_caption";
            this.textBox_font_caption.ReadOnly = true;
            this.textBox_font_caption.Size = new System.Drawing.Size(569, 42);
            this.textBox_font_caption.TabIndex = 7;
            this.textBox_font_caption.TextChanged += new System.EventHandler(this.textBox_font_content_TextChanged);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(18, 156);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(193, 30);
            this.label5.TabIndex = 6;
            this.label5.Text = "提示字体(&A):";
            // 
            // button_font_fixed
            // 
            this.button_font_fixed.Location = new System.Drawing.Point(799, 97);
            this.button_font_fixed.Name = "button_font_fixed";
            this.button_font_fixed.Size = new System.Drawing.Size(164, 41);
            this.button_font_fixed.TabIndex = 5;
            this.button_font_fixed.Text = "设置 ...";
            this.button_font_fixed.UseVisualStyleBackColor = true;
            this.button_font_fixed.Click += new System.EventHandler(this.button_font_fixed_Click);
            // 
            // textBox_font_fixed
            // 
            this.textBox_font_fixed.Location = new System.Drawing.Point(226, 94);
            this.textBox_font_fixed.Name = "textBox_font_fixed";
            this.textBox_font_fixed.ReadOnly = true;
            this.textBox_font_fixed.Size = new System.Drawing.Size(569, 42);
            this.textBox_font_fixed.TabIndex = 4;
            this.textBox_font_fixed.TextChanged += new System.EventHandler(this.textBox_font_content_TextChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(18, 97);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(193, 30);
            this.label4.TabIndex = 3;
            this.label4.Text = "等宽字体(&F):";
            // 
            // button_font_content
            // 
            this.button_font_content.Location = new System.Drawing.Point(799, 43);
            this.button_font_content.Name = "button_font_content";
            this.button_font_content.Size = new System.Drawing.Size(164, 41);
            this.button_font_content.TabIndex = 2;
            this.button_font_content.Text = "设置 ...";
            this.button_font_content.UseVisualStyleBackColor = true;
            this.button_font_content.Click += new System.EventHandler(this.button_font_content_Click);
            // 
            // textBox_font_content
            // 
            this.textBox_font_content.Location = new System.Drawing.Point(226, 41);
            this.textBox_font_content.Name = "textBox_font_content";
            this.textBox_font_content.ReadOnly = true;
            this.textBox_font_content.Size = new System.Drawing.Size(569, 42);
            this.textBox_font_content.TabIndex = 1;
            this.textBox_font_content.TextChanged += new System.EventHandler(this.textBox_font_content_TextChanged);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(18, 43);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(193, 30);
            this.label3.TabIndex = 0;
            this.label3.Text = "内容字体(&C):";
            // 
            // marcControl_preview
            // 
            this.marcControl_preview.AutoScroll = true;
            this.marcControl_preview.AutoScrollMinSize = new System.Drawing.Size(1050, 0);
            this.marcControl_preview.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.marcControl_preview.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.marcControl_preview.CaptionFont = new System.Drawing.Font("宋体", 9F);
            this.marcControl_preview.ClientBoundsWidth = 0;
            this.marcControl_preview.ColorThemeName = null;
            this.marcControl_preview.Content = "";
            this.marcControl_preview.DeleteKeyStyle = LibraryStudio.Forms.DeleteKeyStyle.DeleteFieldTerminator;
            this.marcControl_preview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.marcControl_preview.FixedSizeFont = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Bold);
            this.marcControl_preview.HighlightBlankChar = ' ';
            this.marcControl_preview.Location = new System.Drawing.Point(3, 33);
            this.marcControl_preview.Name = "marcControl_preview";
            this.marcControl_preview.PaddingChar = ' ';
            this.marcControl_preview.PadWhileEditing = false;
            this.marcControl_preview.ReadOnly = false;
            this.marcControl_preview.Size = new System.Drawing.Size(1063, 359);
            this.marcControl_preview.TabIndex = 2;
            // 
            // tableLayoutPanel_main
            // 
            this.tableLayoutPanel_main.ColumnCount = 1;
            this.tableLayoutPanel_main.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_main.Controls.Add(this.panel_ok_cancel, 0, 2);
            this.tableLayoutPanel_main.Controls.Add(this.splitContainer1, 0, 1);
            this.tableLayoutPanel_main.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel_main.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel_main.Name = "tableLayoutPanel_main";
            this.tableLayoutPanel_main.RowCount = 3;
            this.tableLayoutPanel_main.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel_main.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_main.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel_main.Size = new System.Drawing.Size(1075, 831);
            this.tableLayoutPanel_main.TabIndex = 9;
            // 
            // panel_ok_cancel
            // 
            this.panel_ok_cancel.AutoSize = true;
            this.panel_ok_cancel.Controls.Add(this.button_cancel);
            this.panel_ok_cancel.Controls.Add(this.button_ok);
            this.panel_ok_cancel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel_ok_cancel.Location = new System.Drawing.Point(3, 767);
            this.panel_ok_cancel.Name = "panel_ok_cancel";
            this.panel_ok_cancel.Size = new System.Drawing.Size(1069, 61);
            this.panel_ok_cancel.TabIndex = 0;
            // 
            // tableLayoutPanel_marcPreview
            // 
            this.tableLayoutPanel_marcPreview.ColumnCount = 1;
            this.tableLayoutPanel_marcPreview.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_marcPreview.Controls.Add(this.label2, 0, 0);
            this.tableLayoutPanel_marcPreview.Controls.Add(this.marcControl_preview, 0, 1);
            this.tableLayoutPanel_marcPreview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel_marcPreview.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel_marcPreview.Name = "tableLayoutPanel_marcPreview";
            this.tableLayoutPanel_marcPreview.RowCount = 2;
            this.tableLayoutPanel_marcPreview.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel_marcPreview.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel_marcPreview.Size = new System.Drawing.Size(1069, 395);
            this.tableLayoutPanel_marcPreview.TabIndex = 4;
            // 
            // VisualStyleDialog
            // 
            this.AcceptButton = this.button_ok;
            this.AutoScaleDimensions = new System.Drawing.SizeF(15F, 30F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.button_clearDelimeterColor;
            this.ClientSize = new System.Drawing.Size(1075, 831);
            this.Controls.Add(this.tableLayoutPanel_main);
            this.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.MinimizeBox = false;
            this.Name = "VisualStyleDialog";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.Text = "视觉风格";
            this.Load += new System.EventHandler(this.VisualStyleDialog_Load);
            this.splitContainer1.Panel1.ResumeLayout(false);
            this.splitContainer1.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitContainer1)).EndInit();
            this.splitContainer1.ResumeLayout(false);
            this.tabControl_colorTheme.ResumeLayout(false);
            this.tabPage_colorTheme.ResumeLayout(false);
            this.tableLayoutPanel_colorTheme.ResumeLayout(false);
            this.tableLayoutPanel_colorTheme.PerformLayout();
            this.panel_color_theme_list.ResumeLayout(false);
            this.panel_color_theme_list.PerformLayout();
            this.tabPage_font.ResumeLayout(false);
            this.tabPage_font.PerformLayout();
            this.tableLayoutPanel_main.ResumeLayout(false);
            this.tableLayoutPanel_main.PerformLayout();
            this.panel_ok_cancel.ResumeLayout(false);
            this.tableLayoutPanel_marcPreview.ResumeLayout(false);
            this.tableLayoutPanel_marcPreview.PerformLayout();
            this.ResumeLayout(false);

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
        private System.Windows.Forms.SplitContainer splitContainer1;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_main;
        private System.Windows.Forms.Panel panel_ok_cancel;
        private System.Windows.Forms.Panel panel_color_theme_list;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_colorTheme;
        private System.Windows.Forms.TabControl tabControl_colorTheme;
        private System.Windows.Forms.TabPage tabPage_colorTheme;
        private System.Windows.Forms.TabPage tabPage_font;
        private System.Windows.Forms.Button button_font_content;
        private System.Windows.Forms.TextBox textBox_font_content;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button_font_caption;
        private System.Windows.Forms.TextBox textBox_font_caption;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button button_font_fixed;
        private System.Windows.Forms.TextBox textBox_font_fixed;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button_font_reset;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel_marcPreview;
    }
}
