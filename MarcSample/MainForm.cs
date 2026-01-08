using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using LibraryStudio.Forms;

namespace MarcSample
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            this.marcControl1.GetFieldCaption += (field) =>
            {
                if (field.IsHeader)
                    return $"头标区";
                return $"获得 '{field.FieldName}' 的值";
            };
            this.marcControl1.CaretMoved += (s, e) =>
            {
                toolStripStatusLabel_caretOffs.Text = $"Caret:{this.marcControl1.CaretOffset}";
                toolStripStatusLabel_caretFieldRegion.Text = "FieldRegion:" + this.marcControl1.CaretFieldRegion.ToString();
            };
            this.marcControl1.SelectionChanged += (s, e) =>
            {
                toolStripStatusLabel_selectionRange.Text = $"Block:{this.marcControl1.SelectionStart}-{this.marcControl1.SelectionEnd}";
            };
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.marcControl1.ColorThemeName = "simplest";

            this.marcControl1.ClientBoundsWidth = 0;
            // this.marcControl1.ClientBoundsWidth = 800;
            // this.marcControl1.ClientBoundsWidth = -1;



            // this.marcControl1.Content = "012345678901234567890123abc12ABC\u001faAAA\u001fbBBB";
            //this.marcControl1.Content = new string((char)31, 1) + "1";
            // this.marcControl1.Content = "ش12345678901234567890123";
            // this.marcControl1.Content = "012345678901234567890123";
            // this.marcControl1.Content = "012345678901234567890123abc12ABC\u001faAAA\u001fbBBB شلاؤيث ฟิแกำดเ 中文 english\u001e100  \u001fatest333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333";
            this.marcControl1.Content = "01234567890123456789";
            this.marcControl1.PadWhileEditing = false;
            // this.marcControl1.PaddingChar = '*';

            this.marcControl1.HighlightBlankChar = '·';  // '◌'; // '▪';// '▫'; // '□'; // '⸗';

            LoadState();
            LoadMarc();
        }

        private void MenuItem_testSetCallback_Click(object sender, EventArgs e)
        {
            var context = this.marcControl1.GetContext();
            context.GetBackColor = (range, highlight) =>
            {
                if (highlight)
                    return Color.DarkRed;
                return Color.White;
            };
            context.GetForeColor = (o, highlight) =>
            {
                if (highlight)
                    return Color.White;
                var range = o as Range;
                if (range.Tag is bool)
                    return Color.DarkRed; // 子字段名文本为红色
                return Color.Black;
            };
            context.SplitRange = (o, content) =>
            {
                return SimpleText.SegmentSubfields2(content, '\x001f', 2, true);
            };

            // 迫使重新生成结构
            var save = this.marcControl1.Content;
            this.marcControl1.Content = "";
            this.marcControl1.Content = save;
            MessageBox.Show(this, "context GetBackColor() GetForeColor() 已经被设置为定制效果:\r\n普通文字白底黑字(子字段符号为红色)；选择文字红底白字");
        }

        // 将切割好的一个一个子字段字符串，的每一个，进一步切割为 name 和 content 两部分
        string[] SplitNameContent(string[] subfields)
        {
            List<string> results = new List<string>();
            foreach (var subfield in subfields)
            {
                if (subfield.Length > 2
                    && subfield.StartsWith("\x001f")
                    )
                {
                    results.Add(subfield.Substring(0, 2));
                    results.Add(subfield.Substring(2));
                }
                else
                    results.Add(subfield);
            }

            return results.ToArray();
        }

        private void MenuItem_clearCallback_Click(object sender, EventArgs e)
        {
            var context = this.marcControl1.GetContext();
            context.GetBackColor = null;
            context.GetForeColor = null;
            context.SplitRange = null;

            // 迫使重新生成结构
            this.marcControl1.Content = this.marcControl1.Content;
            MessageBox.Show(this, "context GetBackColor() GetForeColor() 已经被设置为 null");
        }

        private void MenuItem_apperance_DropDownOpening(object sender, EventArgs e)
        {
            this.MenuItem_readonly.Checked = this.marcControl1.ReadOnly;
        }

        private void MenuItem_readonly_Click(object sender, EventArgs e)
        {
            this.marcControl1.ReadOnly = !this.marcControl1.ReadOnly;
        }

        private void MenuItem_dumpHistory_Click(object sender, EventArgs e)
        {
            string strFileName = Path.Combine(GetBinDirectory(), "history.txt");
            File.WriteAllText(strFileName, this.marcControl1.DumpHistory());
            Process.Start("notepad.exe", strFileName);
        }

        string GetBinDirectory()
        {
            return Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        public void SetFont()
        {
            FontDialog dlg = new FontDialog();
            dlg.ShowColor = true;
            //dlg.Color = this.marcControl1.ContentTextColor;
            dlg.Font = this.marcControl1.Font;
            dlg.ShowApply = true;
            dlg.ShowHelp = true;
            dlg.AllowVerticalFonts = false;

            //dlg.Apply -= new EventHandler(dlgMarcEditFont_Apply);
            //dlg.Apply += new EventHandler(dlgMarcEditFont_Apply);
            if (dlg.ShowDialog(this) != DialogResult.OK)
                return;

            this.marcControl1.Font = dlg.Font;
            //this.marcControl1.ContentTextColor = dlg.Color;
        }

        private void MenuItem_setFont_Click(object sender, EventArgs e)
        {
            SetFont();
        }

        private void MenuItem_verifyCharCount_Click(object sender, EventArgs e)
        {
            // 按下 Ctrl 键可自动修复
            var fix = (Control.ModifierKeys & Keys.Control) != 0;
            var errors = this.marcControl1.Verify(fix);
            MessageBox.Show(
                this,
                string.Join("\r\n", errors)
                + (fix && errors.Count() > 0 ? "\r\n已经自动修复。" : ""));
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveMarc();
            SaveState();
        }

        private void MainForm_FormClosed(object sender, FormClosedEventArgs e)
        {

        }

        void LoadState()
        {
            try
            {
                var path = GetStateFileName();
                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path);
                    this.marcControl1.UiStateJson = content;
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        void SaveState()
        {
            var path = GetStateFileName();
            File.WriteAllText(path, this.marcControl1.UiStateJson);
        }

        string GetMarcFileName()
        {
            return Path.Combine(GetBinDirectory(), "marc.txt");
        }

        string GetStateFileName()
        {
            return Path.Combine(GetBinDirectory(), "state.txt");
        }

        void LoadMarc()
        {
            //return;
            try
            {
                var path = GetMarcFileName();
                if (File.Exists(path))
                {
                    var content = File.ReadAllText(path);
                    if (string.IsNullOrEmpty(content) == false)
                        this.marcControl1.Content = content;
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        void SaveMarc()
        {
            //return;
            var path = GetMarcFileName();
            File.WriteAllText(path, this.marcControl1.Content);
        }
    }
}
