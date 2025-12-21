using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace EditorSample
{
    public partial class MainForm : Form
    {
        string _fileName = string.Empty;

        public string FileName
        {
            get { return _fileName; }
            set
            {
                _fileName = value;
                SetTitle();
            }
        }

        bool _autoWrap = true;

        public MainForm()
        {
            InitializeComponent();

            this.editControl11.TextChanged += (s, e) =>
            {
                this.SetTitle();    // Changed == true 会出现星号
            };
        }

        private void MenuItem_openFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = $"Open text file";
                // dlg.FileName = this.textBox_filename.Text;

                dlg.Filter = $"Text files (*.txt)|*.txt|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                FileName = dlg.FileName;
                this.editControl11.Content = File.ReadAllText(FileName);
                this.editControl11.Changed = false;
                this.SetTitle();
                this.editControl11.Focus();
            }
        }

        private void MenuItem_exit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void MenuItem_save_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(FileName))
            {
                MenuItem_saveAs_Click(sender, e);
                return;
            }

            File.WriteAllText(FileName, this.editControl11.Content);
            this.editControl11.Changed = false;
            this.SetTitle();
        }

        private void MenuItem_saveAs_Click(object sender, EventArgs e)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Title = $"Save text file";
                // dlg.FileName = this.textBox_filename.Text;

                dlg.Filter = $"Text files (*.txt)|*.txt|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() != DialogResult.OK)
                    return;

                FileName = dlg.FileName;
                File.WriteAllText(FileName, this.editControl11.Content);
                this.editControl11.Changed = false;
                this.SetTitle();
                this.editControl11.Focus();
            }
        }

        void SetTitle()
        {
            var title = "SampleEditor - "
                + (this.editControl11.Changed ? "*" : "")
                + Path.GetFileName(_fileName);
            this.Text = title;
        }

        private void MenuItem_cut_Click(object sender, EventArgs e)
        {
            this.editControl11.Cut();
        }

        private void MenuItem_copy_Click(object sender, EventArgs e)
        {
            this.editControl11.Copy();
        }

        private void MenuItem_paste_Click(object sender, EventArgs e)
        {
            this.editControl11.Paste();
        }

        private void MenuItem_selectAll_Click(object sender, EventArgs e)
        {
            this.editControl11.SelectAll();
        }

        private void MenuItem_edit_DropDownOpening(object sender, EventArgs e)
        {
            if (Clipboard.ContainsText())
            {
                this.MenuItem_paste.Enabled = true;
            }
            else
            {
                this.MenuItem_paste.Enabled = false;
            }

            if (this.editControl11.HasBlock())
            {
                this.MenuItem_cut.Enabled = true;
                this.MenuItem_copy.Enabled = true;
            }
            else
            {
                this.MenuItem_cut.Enabled = false;
                this.MenuItem_copy.Enabled = false;
            }

            this.MenuItem_undo.Enabled = this.editControl11.CanUndo();
            this.MenuItem_redo.Enabled = this.editControl11.CanRedo();
        }

        private void MenuItem_undo_Click(object sender, EventArgs e)
        {
            this.editControl11.Undo();
        }

        private void MenuItem_redo_Click(object sender, EventArgs e)
        {
            this.editControl11.Redo();
        }

        private void MenuItem_autoWrap_Click(object sender, EventArgs e)
        {
            _autoWrap = !_autoWrap;
            if (_autoWrap)
                this.editControl11.ClientBoundsWidth = 0; // 自动换行
            else
                this.editControl11.ClientBoundsWidth = -1; // 不自动换行
        }

        private void MenuItem_view_DropDownOpening(object sender, EventArgs e)
        {
            this.MenuItem_autoWrap.Checked = _autoWrap;
        }

        private void MenuItem_font_Click(object sender, EventArgs e)
        {
            using (FontDialog dlg = new FontDialog())
            {
                // dlg.ShowColor = true;
                // dlg.Color = this.DcEditor.ForeColor;
                dlg.Font = this.editControl11.Font;
                dlg.ShowApply = true;
                //dlg.ShowHelp = true;
                // dlg.AllowVerticalFonts = false;

                dlg.Apply += (s1, e1) =>
                {
                    this.editControl11.Font = dlg.Font;
                };
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    // TODO: 是否要还原最初的字体?
                    return;
                }
                this.editControl11.Font = dlg.Font;
            }
        }
    }
}
