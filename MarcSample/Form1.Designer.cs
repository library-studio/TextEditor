namespace MarcSample
{
    partial class Form1
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
            this.marcControl1 = new LibraryStudio.Forms.MarcControl();
            this.SuspendLayout();
            // 
            // marcControl1
            // 
            this.marcControl1.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.marcControl1.AutoScroll = true;
            this.marcControl1.AutoScrollMinSize = new System.Drawing.Size(776, 24);
            this.marcControl1.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
            this.marcControl1.Changed = false;
            this.marcControl1.ClientBoundsWidth = 0;
            this.marcControl1.Content = "";
            this.marcControl1.Location = new System.Drawing.Point(12, 12);
            this.marcControl1.Name = "marcControl1";
            this.marcControl1.Size = new System.Drawing.Size(776, 371);
            this.marcControl1.TabIndex = 0;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(11F, 21F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.marcControl1);
            this.Name = "Form1";
            this.Text = "MarcSample";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.ResumeLayout(false);

        }

        #endregion

        private LibraryStudio.Forms.MarcControl marcControl1;
    }
}

