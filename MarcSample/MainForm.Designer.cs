namespace MarcSample
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.MenuItem_file = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_dumpHistory = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_verifyCharCount = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_exit = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_apperance = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_testCallback = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_clearCallback = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_readonly = new System.Windows.Forms.ToolStripMenuItem();
            this.MenuItem_setFont = new System.Windows.Forms.ToolStripMenuItem();
            this.statusStrip1 = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel_caretFieldRegion = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStrip1 = new System.Windows.Forms.ToolStrip();
            this.marcControl1 = new LibraryStudio.Forms.MarcControl();
            this.toolStripStatusLabel_selectionRange = new System.Windows.Forms.ToolStripStatusLabel();
            this.toolStripStatusLabel_caretOffs = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip1.SuspendLayout();
            this.statusStrip1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_file,
            this.MenuItem_apperance});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1309, 72);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // MenuItem_file
            // 
            this.MenuItem_file.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_dumpHistory,
            this.MenuItem_verifyCharCount,
            this.MenuItem_exit});
            this.MenuItem_file.Name = "MenuItem_file";
            this.MenuItem_file.Size = new System.Drawing.Size(107, 68);
            this.MenuItem_file.Text = "&File";
            // 
            // MenuItem_dumpHistory
            // 
            this.MenuItem_dumpHistory.Name = "MenuItem_dumpHistory";
            this.MenuItem_dumpHistory.Size = new System.Drawing.Size(497, 66);
            this.MenuItem_dumpHistory.Text = "Dump History ...";
            this.MenuItem_dumpHistory.Click += new System.EventHandler(this.MenuItem_dumpHistory_Click);
            // 
            // MenuItem_verifyCharCount
            // 
            this.MenuItem_verifyCharCount.Name = "MenuItem_verifyCharCount";
            this.MenuItem_verifyCharCount.Size = new System.Drawing.Size(497, 66);
            this.MenuItem_verifyCharCount.Text = "检查字符不足 ...";
            this.MenuItem_verifyCharCount.Click += new System.EventHandler(this.MenuItem_verifyCharCount_Click);
            // 
            // MenuItem_exit
            // 
            this.MenuItem_exit.Name = "MenuItem_exit";
            this.MenuItem_exit.Size = new System.Drawing.Size(497, 66);
            this.MenuItem_exit.Text = "Exit";
            // 
            // MenuItem_apperance
            // 
            this.MenuItem_apperance.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.MenuItem_testCallback,
            this.MenuItem_clearCallback,
            this.MenuItem_readonly,
            this.MenuItem_setFont});
            this.MenuItem_apperance.Name = "MenuItem_apperance";
            this.MenuItem_apperance.Size = new System.Drawing.Size(233, 68);
            this.MenuItem_apperance.Text = "&Apperance";
            this.MenuItem_apperance.DropDownOpening += new System.EventHandler(this.MenuItem_apperance_DropDownOpening);
            // 
            // MenuItem_testCallback
            // 
            this.MenuItem_testCallback.Name = "MenuItem_testCallback";
            this.MenuItem_testCallback.Size = new System.Drawing.Size(510, 66);
            this.MenuItem_testCallback.Text = "Test Set Callback";
            this.MenuItem_testCallback.Click += new System.EventHandler(this.MenuItem_testSetCallback_Click);
            // 
            // MenuItem_clearCallback
            // 
            this.MenuItem_clearCallback.Name = "MenuItem_clearCallback";
            this.MenuItem_clearCallback.Size = new System.Drawing.Size(510, 66);
            this.MenuItem_clearCallback.Text = "Clear Callbak";
            this.MenuItem_clearCallback.Click += new System.EventHandler(this.MenuItem_clearCallback_Click);
            // 
            // MenuItem_readonly
            // 
            this.MenuItem_readonly.Name = "MenuItem_readonly";
            this.MenuItem_readonly.Size = new System.Drawing.Size(510, 66);
            this.MenuItem_readonly.Text = "ReadOnly";
            this.MenuItem_readonly.Click += new System.EventHandler(this.MenuItem_readonly_Click);
            // 
            // MenuItem_setFont
            // 
            this.MenuItem_setFont.Name = "MenuItem_setFont";
            this.MenuItem_setFont.Size = new System.Drawing.Size(510, 66);
            this.MenuItem_setFont.Text = "Set Font ...";
            this.MenuItem_setFont.Click += new System.EventHandler(this.MenuItem_setFont_Click);
            // 
            // statusStrip1
            // 
            this.statusStrip1.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.statusStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel_caretFieldRegion,
            this.toolStripStatusLabel_selectionRange,
            this.toolStripStatusLabel_caretOffs});
            this.statusStrip1.Location = new System.Drawing.Point(0, 710);
            this.statusStrip1.Name = "statusStrip1";
            this.statusStrip1.Size = new System.Drawing.Size(1309, 61);
            this.statusStrip1.TabIndex = 1;
            this.statusStrip1.Text = "statusStrip1";
            // 
            // toolStripStatusLabel_caretFieldRegion
            // 
            this.toolStripStatusLabel_caretFieldRegion.Name = "toolStripStatusLabel_caretFieldRegion";
            this.toolStripStatusLabel_caretFieldRegion.Size = new System.Drawing.Size(142, 46);
            this.toolStripStatusLabel_caretFieldRegion.Text = "Region";
            // 
            // toolStrip1
            // 
            this.toolStrip1.ImageScalingSize = new System.Drawing.Size(48, 48);
            this.toolStrip1.Location = new System.Drawing.Point(0, 72);
            this.toolStrip1.Name = "toolStrip1";
            this.toolStrip1.Size = new System.Drawing.Size(1309, 75);
            this.toolStrip1.TabIndex = 2;
            this.toolStrip1.Text = "toolStrip1";
            // 
            // marcControl1
            // 
            this.marcControl1.AutoScroll = true;
            this.marcControl1.AutoScrollMinSize = new System.Drawing.Size(1309, 0);
            this.marcControl1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.marcControl1.CaptionFont = new System.Drawing.Font("楷体", 9F);
            this.marcControl1.Changed = false;
            this.marcControl1.ClientBoundsWidth = 0;
            this.marcControl1.Content = "";
            this.marcControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.marcControl1.FixedSizeFont = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Bold);
            this.marcControl1.Font = new System.Drawing.Font("宋体", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.marcControl1.Location = new System.Drawing.Point(0, 147);
            this.marcControl1.Name = "marcControl1";
            this.marcControl1.PaddingChar = ' ';
            this.marcControl1.PadWhileEditing = false;
            this.marcControl1.ReadOnly = false;
            this.marcControl1.Size = new System.Drawing.Size(1309, 563);
            this.marcControl1.TabIndex = 3;
            // 
            // toolStripStatusLabel_blockRange
            // 
            this.toolStripStatusLabel_selectionRange.Name = "toolStripStatusLabel_blockRange";
            this.toolStripStatusLabel_selectionRange.Size = new System.Drawing.Size(223, 46);
            this.toolStripStatusLabel_selectionRange.Text = "BlockRange";
            // 
            // toolStripStatusLabel_caretOffs
            // 
            this.toolStripStatusLabel_caretOffs.Name = "toolStripStatusLabel_caretOffs";
            this.toolStripStatusLabel_caretOffs.Size = new System.Drawing.Size(181, 46);
            this.toolStripStatusLabel_caretOffs.Text = "CaretOffs";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(288F, 288F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(1309, 771);
            this.Controls.Add(this.marcControl1);
            this.Controls.Add(this.toolStrip1);
            this.Controls.Add(this.statusStrip1);
            this.Controls.Add(this.menuStrip1);
            this.Margin = new System.Windows.Forms.Padding(5);
            this.Name = "MainForm";
            this.Text = "MarcSample";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.MainForm_FormClosed);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            this.statusStrip1.ResumeLayout(false);
            this.statusStrip1.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.StatusStrip statusStrip1;
        private System.Windows.Forms.ToolStrip toolStrip1;
        private LibraryStudio.Forms.MarcControl marcControl1;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_file;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_exit;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_apperance;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_testCallback;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_clearCallback;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_readonly;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_dumpHistory;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_setFont;
        private System.Windows.Forms.ToolStripMenuItem MenuItem_verifyCharCount;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel_caretFieldRegion;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel_selectionRange;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel_caretOffs;
    }
}

