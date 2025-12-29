using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibraryStudio.Forms.MarcControlDialog
{
    public partial class DeletingFieldDialog : Form
    {
        public PaintEventHandler PaintPreview { get; set; }

        public DeletingFieldDialog()
        {
            InitializeComponent();
        }

        private void DeletingFieldDialog_Load(object sender, EventArgs e)
        {

        }

        private void button_ok_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void button_cancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void panel_preview_Paint(object sender, PaintEventArgs e)
        {
            this.PaintPreview?.Invoke(sender, e);
        }

        protected override bool ProcessDialogKey(
    Keys keyData)
        {
            if (keyData == Keys.Delete)
            {
                button_ok_Click(this, EventArgs.Empty);
                return true;
            }

            return base.ProcessDialogKey(keyData);
        }

        public string MessageText
        {
            get
            {
                return this.label_message.Text;
            }
            set
            {
                this.label_message.Text = value;
            }
        }
    }
}
