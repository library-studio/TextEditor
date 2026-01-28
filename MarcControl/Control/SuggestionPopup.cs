#if NO
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 简单的候选弹出框。基于 ToolStripDropDown + ListBoxHost，
    /// 可在不长期抢占编辑控件焦点的情况下显示候选项。
    /// </summary>
    internal class SuggestionPopup : ToolStripDropDown
    {
        readonly ListBox _listBox;
        readonly ToolStripControlHost _host;

        public event EventHandler<string> ItemChosen;
        public event EventHandler Cancelled;

        public bool AutoResize { get; set; } = true;
        public int MaxVisibleItems { get; set; } = 8;

        public SuggestionPopup()
        {
            DoubleBuffered = true;
            _listBox = new ListBox
            {
                BorderStyle = BorderStyle.None,
                IntegralHeight = false,
                SelectionMode = SelectionMode.One,
                TabStop = false
            };
            _listBox.MouseClick += (s, e) =>
            {
                AcceptSelected();
            };
            _listBox.KeyDown += (s, e) =>
            {
                // 当 ListBox 有焦点且用户按 Enter 时确认
                if (e.KeyCode == Keys.Enter)
                {
                    AcceptSelected();
                    e.Handled = true;
                }
                else if (e.KeyCode == Keys.Escape)
                {
                    Cancel();
                    e.Handled = true;
                }
            };

            _host = new ToolStripControlHost(_listBox)
            {
                Margin = Padding.Empty,
                Padding = Padding.Empty,
                AutoSize = false
            };
            this.Items.Add(_host);

            this.AutoClose = true;
            this.Margin = Padding.Empty;
            this.Padding = Padding.Empty;
            this.RenderMode = ToolStripRenderMode.System;
        }

        public void SetItems(IEnumerable<string> items)
        {
            var arr = items?.ToArray() ?? Array.Empty<string>();
            _listBox.BeginUpdate();
            _listBox.Items.Clear();
            foreach (var s in arr) _listBox.Items.Add(s);
            _listBox.SelectedIndex = _listBox.Items.Count > 0 ? 0 : -1;
            _listBox.EndUpdate();

            if (AutoResize)
            {
                ResizeToFit();
            }
        }

        void ResizeToFit()
        {
            int w = 120;
            using (var g = _listBox.CreateGraphics())
            {
                foreach (var obj in _listBox.Items)
                {
                    var s = obj?.ToString() ?? "";
                    var sz = TextRenderer.MeasureText(g, s, _listBox.Font);
                    w = Math.Max(w, sz.Width + SystemInformation.VerticalScrollBarWidth + 8);
                }
            }
            int visibleCount = Math.Min(MaxVisibleItems, Math.Max(1, _listBox.Items.Count));
            int h = visibleCount * (_listBox.ItemHeight) + 4;
            _host.Size = new Size(w, h);
            this.Size = _host.Size;
        }

        public void ShowAt(Point screenLocation)
        {
            // 显示在屏幕坐标
            this.Show(screenLocation);
        }

        /// <summary>
        /// 选择上一个条目，返回是否改变选择
        /// </summary>
        public bool SelectPrev()
        {
            if (_listBox.Items.Count == 0) return false;
            int i = Math.Max(0, _listBox.SelectedIndex - 1);
            if (i != _listBox.SelectedIndex)
            {
                _listBox.SelectedIndex = i;
                return true;
            }
            return false;
        }

        public bool SelectNext()
        {
            if (_listBox.Items.Count == 0) return false;
            int i = Math.Min(_listBox.Items.Count - 1, _listBox.SelectedIndex + 1);
            if (i != _listBox.SelectedIndex)
            {
                _listBox.SelectedIndex = i;
                return true;
            }
            return false;
        }

        public bool PageUp()
        {
            if (_listBox.Items.Count == 0) return false;
            int page = Math.Max(1, _listBox.Height / Math.Max(1, _listBox.ItemHeight));
            int i = Math.Max(0, _listBox.SelectedIndex - page);
            if (i != _listBox.SelectedIndex)
            {
                _listBox.SelectedIndex = i;
                return true;
            }
            return false;
        }

        public bool PageDown()
        {
            if (_listBox.Items.Count == 0) return false;
            int page = Math.Max(1, _listBox.Height / Math.Max(1, _listBox.ItemHeight));
            int i = Math.Min(_listBox.Items.Count - 1, _listBox.SelectedIndex + page);
            if (i != _listBox.SelectedIndex)
            {
                _listBox.SelectedIndex = i;
                return true;
            }
            return false;
        }

        public string GetSelectedItem()
        {
            return _listBox.SelectedItem?.ToString();
        }

        public void AcceptSelected()
        {
            var item = GetSelectedItem();
            if (item != null)
            {
                ItemChosen?.Invoke(this, item);
            }
            this.Close();
        }

        public void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            this.Close();
        }
    }
}

// csharp MarcControl\Control\SuggestionPopup.cs
#endif
using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 无激活的候选弹出框（使用 WS_EX_NOACTIVATE）。
    /// 不会激活窗口，保证输入焦点仍留在编辑控件（MarcControl）。
    /// </summary>
    internal class SuggestionPopup : Form
    {
        readonly ListBox _listBox;

        public event EventHandler<string> ItemChosen;
        public event EventHandler Cancelled;

        public bool AutoResize { get; set; } = true;
        public int MaxVisibleItems { get; set; } = 8;

        public SuggestionPopup()
        {
            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;

            // 建立 ListBox
            _listBox = new ListBox
            {
                BorderStyle = BorderStyle.FixedSingle,
                IntegralHeight = false,
                SelectionMode = SelectionMode.One,
                TabStop = false
            };

            _listBox.MouseClick += (s, e) =>
            {
                AcceptSelected();
            };
            // 鼠标双击也确认
            _listBox.DoubleClick += (s, e) =>
            {
                AcceptSelected();
            };

            // 即便该窗体不激活，也可以响应鼠标、滚轮等
            this.Controls.Add(_listBox);
        }

        // 确保窗体窗口样式包含 WS_EX_NOACTIVATE & WS_EX_TOOLWINDOW
        protected override CreateParams CreateParams
        {
            get
            {
                const int WS_EX_NOACTIVATE = 0x08000000;
                const int WS_EX_TOOLWINDOW = 0x00000080;
                var cp = base.CreateParams;
                cp.ExStyle |= WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW;
                return cp;
            }
        }

        public void SetItems(System.Collections.Generic.IEnumerable<string> items)
        {
            var arr = items?.ToArray() ?? new string[0];
            _listBox.BeginUpdate();
            _listBox.Items.Clear();
            foreach (var s in arr)
            {
                _listBox.Items.Add(s);
            }

            _listBox.SelectedIndex = _listBox.Items.Count > 0 ? 0 : -1;
            _listBox.EndUpdate();

            if (AutoResize)
                ResizeToFit();
        }

        void ResizeToFit()
        {
            int w = 120;
            using (var g = _listBox.CreateGraphics())
            {
                foreach (var obj in _listBox.Items.OfType<object>())
                {
                    var s = obj?.ToString() ?? "";
                    var sz = TextRenderer.MeasureText(g, s, _listBox.Font);
                    w = Math.Max(w, sz.Width + SystemInformation.VerticalScrollBarWidth + 12);
                }
            }
            int visibleCount = Math.Min(MaxVisibleItems, Math.Max(1, _listBox.Items.Count));
            int h = visibleCount * Math.Max(16, _listBox.ItemHeight) + 6;
            _listBox.Location = new Point(2, 2);
            _listBox.Size = new Size(w, h);
            this.ClientSize = new Size(w + 4, h + 4);
        }

        /// <summary>
        /// 在屏幕坐标显示（不会激活）。
        /// </summary>
        public void ShowAt(Point screenLocation)
        {
            this.Location = screenLocation;
            if (!this.Visible)
                this.Show();   // Show 不会激活（CreateParams 指定了 NOACTIVATE）
            else
                this.Refresh();
        }

        public bool SelectPrev()
        {
            if (_listBox.Items.Count == 0) return false;
            int i = Math.Max(0, _listBox.SelectedIndex - 1);
            if (i != _listBox.SelectedIndex)
            {
                _listBox.SelectedIndex = i;
                EnsureVisible(i);
                return true;
            }
            return false;
        }

        public bool SelectNext()
        {
            if (_listBox.Items.Count == 0) return false;
            int i = Math.Min(_listBox.Items.Count - 1, _listBox.SelectedIndex + 1);
            if (i != _listBox.SelectedIndex)
            {
                _listBox.SelectedIndex = i;
                EnsureVisible(i);
                return true;
            }
            return false;
        }

        public bool PageUp()
        {
            if (_listBox.Items.Count == 0) return false;
            int page = Math.Max(1, _listBox.Height / Math.Max(1, _listBox.ItemHeight));
            int i = Math.Max(0, _listBox.SelectedIndex - page);
            if (i != _listBox.SelectedIndex)
            {
                _listBox.SelectedIndex = i;
                EnsureVisible(i);
                return true;
            }
            return false;
        }

        public bool PageDown()
        {
            if (_listBox.Items.Count == 0) return false;
            int page = Math.Max(1, _listBox.Height / Math.Max(1, _listBox.ItemHeight));
            int i = Math.Min(_listBox.Items.Count - 1, _listBox.SelectedIndex + page);
            if (i != _listBox.SelectedIndex)
            {
                _listBox.SelectedIndex = i;
                EnsureVisible(i);
                return true;
            }
            return false;
        }

        void EnsureVisible(int index)
        {
            if (index >= 0 && index < _listBox.Items.Count)
                _listBox.TopIndex = Math.Max(0, index - Math.Max(0, _listBox.Height / _listBox.ItemHeight / 2));
        }

        public string GetSelectedItem()
        {
            return _listBox.SelectedItem?.ToString();
        }

        public void AcceptSelected()
        {
            var item = GetSelectedItem();
            if (item != null)
            {
                ItemChosen?.Invoke(this, item);
            }
            else
            {
                // 关闭窗体但不要尝试激活其它窗口（调用 Close 会触发 Closed；MarcControl 会处理 IME）
                try { this.Hide(); } catch { }
            }
        }

        public void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            try { this.Hide(); } catch { }
        }

        // Visible 包装
        public new bool Visible => base.Visible;
    }
}
