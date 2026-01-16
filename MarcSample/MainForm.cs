using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
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
            this.marcControl1.Content = "012345678901234567890123abc12ABC\u001faAAA\u001fbBBB شلاؤيث ฟิแกำดเ 中文 english\u001e100  \u001fatest3333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333333￮﨩\u001d\u001e\u001f33333333";
            // this.marcControl1.Content = "01234567890123456789";
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
            Stop();
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


        private void MenuItem_startStressTest_Click(object sender, EventArgs e)
        {
            StartClipboardStressTest();
        }

        private void MenuItem_stopStressTest_Click(object sender, EventArgs e)
        {
            Stop();
        }

        #region Clipboard Stress Testing

        CancellationTokenSource _cancel = null;

        void StartClipboardStressTest()
        {
            Stop();
            _cancel = new CancellationTokenSource();
            var token = _cancel.Token;
            var task = Task.Factory.StartNew((o) =>
            {
                try
                {
                    for (int i = 0; ; i++)
                    {
                        if (token.IsCancellationRequested)
                        {
                            break;
                        }

                        // TODO: 为了加大压力，是否应该用复制图像进入 Clipboard 来测试
                        var ret = CopyTextToClipboard($"test {i}");
                        /*
                        this.Invoke(new Action(() =>
                        {
                            Clipboard.SetText($"test {i}");
                        }));
                        */
                        Thread.Sleep(20);
                    }
                }
                catch (Exception ex)
                {
                    int k = 0;
                    k++;
                }
            },
            null,
            default,
            TaskCreationOptions.LongRunning,
            TaskScheduler.Default);
        }

        void Stop()
        {
            if (_cancel != null)
            {
                _cancel.Cancel();
                _cancel.Dispose();
                _cancel = null;
            }
        }


        [DllImport("user32.dll")]
        private static extern bool OpenClipboard(IntPtr hWndNewOwner);
        [DllImport("user32.dll")]
        private static extern bool CloseClipboard();
        [DllImport("user32.dll")]
        private static extern bool SetClipboardData(uint uFormat, IntPtr data);
        private const uint CF_UNICODETEXT = 13;
        public static bool CopyTextToClipboard(string text)
        {
            if (!OpenClipboard(IntPtr.Zero))
            {
                return false;
            }
            var global = Marshal.StringToHGlobalUni(text);
            var ret = SetClipboardData(CF_UNICODETEXT, global);
            CloseClipboard();
            return ret;
        }

        #endregion


        // 测试随机发生的字符串
        private void MenuItem_test_randomChars_Click(object sender, EventArgs e)
        {
            char startChar = (char)0x20;
            int count = 1000;

            for (int i = 0; i < 10; i++)
            {
                var text = BuildTestContent(startChar, count);
                this.marcControl1.Content = text.ToString();
                startChar += (char)count;
                if (startChar > 0x9FA5)
                    startChar = (char)0x20;

                Thread.Sleep(500);
            }
        }

        static string BuildTestContent(char startChar, int count)
        {
            var text = new StringBuilder();
            for (int i = 0; i < count; i++)
            {
                var ch = (char)((int)startChar + i);
                text.Append(ch);
                if ((i % 1000) == 0)
                    text.Append((char)30);
            }

            return text.ToString();
        }

        private async void MenuItem_test_loadFromCompactFile_Click(object sender, EventArgs e)
        {
            Stop();
            _cancel = new CancellationTokenSource();
            var token = _cancel.Token;

            using (OpenFileDialog dlg = new OpenFileDialog())
            {
                dlg.Title = $"Open compact file";
                dlg.Filter = "*.compact|*.compact|All files (*.*)|*.*";
                dlg.RestoreDirectory = true;

                if (dlg.ShowDialog() != DialogResult.OK)
                {
                    return;
                }

                await Task.Factory.StartNew((t) =>
                    {
                        foreach (var content in CompactReader(dlg.FileName, Encoding.UTF8))
                        {
                            if (token.IsCancellationRequested)
                                break;
                            this.Invoke(new Action(() => {
                                this.marcControl1.Content = content;
                                this.marcControl1.Update();
                            }));
                            Thread.Sleep(500);
                        }
                    },
                    null,
                    token,
                    TaskCreationOptions.LongRunning,
                    TaskScheduler.Default);
            }
        }

        static IEnumerable<string> CompactReader(string filename,
            Encoding encoding)
        {
            using (var stream = File.OpenRead(filename))
            using (var s = new BufferedStream(stream))
            {
                List<byte> buffer = new List<byte>();
                while (true)
                {
                    var ret = s.ReadByte();
                    if (ret == -1)
                    {
                        if (buffer.Count > 0)
                        {
                            yield return encoding.GetString(buffer.ToArray());
                        }

                        yield break;
                    }
                    buffer.Add((byte)ret);
                    if (ret == 29)
                    {
                        yield return Encoding.UTF8.GetString(buffer.ToArray());
                        buffer.Clear();
                    }
                }
            }
        }
    }
}
