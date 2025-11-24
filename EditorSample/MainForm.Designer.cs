namespace EditorSample
{
    partial class MainForm
    {
        /// <summary>
        /// 必需的设计器变量。
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// 清理所有正在使用的资源。
        /// </summary>
        /// <param name="disposing">如果应释放托管资源，为 true；否则为 false。</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows 窗体设计器生成的代码

        /// <summary>
        /// 设计器支持所需的方法 - 不要修改
        /// 使用代码编辑器修改此方法的内容。
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MenuItem_file = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_openFile = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_save = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_saveAs = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_exit = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_edit = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_undo = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_redo = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.MenuItem_cut = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_copy = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_paste = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_selectAll = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_view = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_autoWrap = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.toolStripButton1 = new System.Windows.Forms.ToolStripButton();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel1 = new System.Windows.Forms.ToolStripStatusLabel();
            this.editControl11 = new LibraryStudio.Forms.EditControl1();
            this.MenuItem_font = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            this.toolStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(28, 28);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_file,
            this.MenuItem_edit,
            this.MenuItem_view});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(976, 37);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // MenuItem_file
            // 
            this.MenuItem_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_openFile,
            this.MenuItem_save,
            this.MenuItem_saveAs,
            this.toolStripSeparator1,
            this.MenuItem_exit});
            this.MenuItem_file.Name = "MenuItem_file";
            this.MenuItem_file.Size = new System.Drawing.Size(65, 33);
            this.MenuItem_file.Text = "&File";
            // 
            // MenuItem_openFile
            // 
            this.MenuItem_openFile.Name = "MenuItem_openFile";
            this.MenuItem_openFile.Size = new System.Drawing.Size(228, 40);
            this.MenuItem_openFile.Text = "&Open ...";
            this.MenuItem_openFile.Click += new System.EventHandler(this.MenuItem_openFile_Click);
            // 
            // MenuItem_save
            // 
            this.MenuItem_save.Name = "MenuItem_save";
            this.MenuItem_save.Size = new System.Drawing.Size(228, 40);
            this.MenuItem_save.Text = "&Save";
            this.MenuItem_save.Click += new System.EventHandler(this.MenuItem_save_Click);
            // 
            // MenuItem_saveAs
            // 
            this.MenuItem_saveAs.Name = "MenuItem_saveAs";
            this.MenuItem_saveAs.Size = new System.Drawing.Size(228, 40);
            this.MenuItem_saveAs.Text = "S&ave As ...";
            this.MenuItem_saveAs.Click += new System.EventHandler(this.MenuItem_saveAs_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(225, 6);
            // 
            // MenuItem_exit
            // 
            this.MenuItem_exit.Name = "MenuItem_exit";
            this.MenuItem_exit.Size = new System.Drawing.Size(228, 40);
            this.MenuItem_exit.Text = "&Exit";
            this.MenuItem_exit.Click += new System.EventHandler(this.MenuItem_exit_Click);
            // 
            // MenuItem_edit
            // 
            this.MenuItem_edit.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_undo,
            this.MenuItem_redo,
            this.toolStripSeparator2,
            this.MenuItem_cut,
            this.MenuItem_copy,
            this.MenuItem_paste,
            this.MenuItem_selectAll});
            this.MenuItem_edit.Name = "MenuItem_edit";
            this.MenuItem_edit.Size = new System.Drawing.Size(69, 33);
            this.MenuItem_edit.Text = "&Edit";
            this.MenuItem_edit.DropDownOpening += new System.EventHandler(this.MenuItem_edit_DropDownOpening);
            // 
            // MenuItem_undo
            // 
            this.MenuItem_undo.Name = "MenuItem_undo";
            this.MenuItem_undo.Size = new System.Drawing.Size(223, 40);
            this.MenuItem_undo.Text = "&Undo";
            this.MenuItem_undo.Click += new System.EventHandler(this.MenuItem_undo_Click);
            // 
            // MenuItem_redo
            // 
            this.MenuItem_redo.Name = "MenuItem_redo";
            this.MenuItem_redo.Size = new System.Drawing.Size(223, 40);
            this.MenuItem_redo.Text = "&Redo";
            this.MenuItem_redo.Click += new System.EventHandler(this.MenuItem_redo_Click);
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(220, 6);
            // 
            // MenuItem_cut
            // 
            this.MenuItem_cut.Name = "MenuItem_cut";
            this.MenuItem_cut.Size = new System.Drawing.Size(223, 40);
            this.MenuItem_cut.Text = "Cu&t";
            this.MenuItem_cut.Click += new System.EventHandler(this.MenuItem_cut_Click);
            // 
            // MenuItem_copy
            // 
            this.MenuItem_copy.Name = "MenuItem_copy";
            this.MenuItem_copy.Size = new System.Drawing.Size(223, 40);
            this.MenuItem_copy.Text = "&Copy";
            this.MenuItem_copy.Click += new System.EventHandler(this.MenuItem_copy_Click);
            // 
            // MenuItem_paste
            // 
            this.MenuItem_paste.Name = "MenuItem_paste";
            this.MenuItem_paste.Size = new System.Drawing.Size(223, 40);
            this.MenuItem_paste.Text = "&Paste";
            this.MenuItem_paste.Click += new System.EventHandler(this.MenuItem_paste_Click);
            // 
            // MenuItem_selectAll
            // 
            this.MenuItem_selectAll.Name = "MenuItem_selectAll";
            this.MenuItem_selectAll.Size = new System.Drawing.Size(223, 40);
            this.MenuItem_selectAll.Text = "Select &All";
            this.MenuItem_selectAll.Click += new System.EventHandler(this.MenuItem_selectAll_Click);
            // 
            // MenuItem_view
            // 
            this.MenuItem_view.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_autoWrap,
            this.MenuItem_font});
            this.MenuItem_view.Name = "MenuItem_view";
            this.MenuItem_view.Size = new System.Drawing.Size(79, 33);
            this.MenuItem_view.Text = "&View";
            this.MenuItem_view.DropDownOpening += new System.EventHandler(this.MenuItem_view_DropDownOpening);
            // 
            // MenuItem_autoWrap
            // 
            this.MenuItem_autoWrap.Name = "MenuItem_autoWrap";
            this.MenuItem_autoWrap.Size = new System.Drawing.Size(315, 40);
            this.MenuItem_autoWrap.Text = "Auto &Wrap";
            this.MenuItem_autoWrap.Click += new System.EventHandler(this.MenuItem_autoWrap_Click);
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(28, 28);
            this.toolStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripButton1});
            this.toolStrip1.Location = new System.Drawing.Point(0, 37);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(976, 38);
            this.toolStrip1.TabIndex = 1;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // toolStripButton1
            // 
            this.toolStripButton1.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Image;
            this.toolStripButton1.Image = ((System.Drawing.Image)(resources.GetObject("toolStripButton1.Image")));
            this.toolStripButton1.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.toolStripButton1.Name = "toolStripButton1";
            this.toolStripButton1.Size = new System.Drawing.Size(40, 32);
            this.toolStripButton1.Text = "toolStripButton1";
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(28, 28);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel1});
            this.statusStrip1.Location = new System.Drawing.Point(0, 485);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(976, 37);
            this.statusStrip1.TabIndex = 2;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel1
            // 
            this.toolStripStatusLabel1.Name = "toolStripStatusLabel1";
            this.toolStripStatusLabel1.Size = new System.Drawing.Size(228, 28);
            this.toolStripStatusLabel1.Text = "toolStripStatusLabel1";
            // 
            // editControl11
            // 
            this.editControl11.AutoScroll = true;
            this.editControl11.AutoScrollMinSize = new System.Drawing.Size(976, 0);
            this.editControl11.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.editControl11.Changed = false;
            this.editControl11.ClientBoundsWidth = 0;
            this.editControl11.Content = "";
            this.editControl11.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editControl11.Location = new System.Drawing.Point(0, 75);
            this.editControl11.Name = "editControl11";
            this.editControl11.Size = new System.Drawing.Size(976, 410);
            this.editControl11.TabIndex = 3;
            // 
            // MenuItem_font
            // 
            this.MenuItem_font.Name = "MenuItem_font";
            this.MenuItem_font.Size = new System.Drawing.Size(315, 40);
            this.MenuItem_font.Text = "&Font ...";
            this.MenuItem_font.Click += new System.EventHandler(this.MenuItem_font_Click);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(976, 522);
            this.Controls.Add(this.editControl11);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "MainForm";
            this.ShowIcon = false;
            this.Text = "Sample Editor";
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.toolStrip1.ResumeLayout(false);
            this.toolStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private LibraryStudio.Forms.EditControl1 editControl11;
        private System.Windows.Forms.ToolStripButton toolStripButton1;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel1;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_file;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_openFile;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_exit;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_save;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_saveAs;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_edit;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_cut;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_copy;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_paste;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_selectAll;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_undo;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_redo;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_view;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_autoWrap;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_font;
    }
}

