using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using static Vanara.PInvoke.User32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 无激活的候选弹出框（使用 WS_EX_NOACTIVATE）。
    /// ListBox 使用 OwnerDraw，实现左右两列显示 ValueItem 的两个成员。
    /// </summary>
    internal class SuggestionPopup : Form
    {
        readonly ListBox _listBox;

        public event EventHandler<string> ItemChosen;
        public event EventHandler Cancelled;

        public bool AutoResize { get; set; } = true;
        public int MaxVisibleItems { get; set; } = 8;

        bool _has_focus = false;
        // 是否具有键盘输入焦点。如果为 false，表示不具备焦点，处在 floating 状态。
        public bool HasFocus
        {
            get
            {
                return _has_focus;
            }
            set
            {
                if (_has_focus != value)
                {
                    _has_focus = value;
                    _listBox.Invalidate();
                }
            }
        }

        Metrics _metrics = null;
        // 值内容字体
        Font _value_font = null;
        // 注释文字字体
        Font _comment_font = null;
        // 用于替代空格、突出显示的代替字符
        char _hilight_blank_char = ' ';

        // 希望一直拥有输入焦点
        internal MarcControl _focus_owner = null;

        public SuggestionPopup(Metrics metrics,
            Font value_font,
            Font comment_font,
            char hilight_blank_char)
        {
            _metrics = metrics;
            _value_font = value_font;
            _comment_font = comment_font;
            _hilight_blank_char = hilight_blank_char;

            FormBorderStyle = FormBorderStyle.None;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            TopMost = true;
            DoubleBuffered = true;
            BackColor = metrics.BorderColor;
            Padding = new Padding(1, 1, 1, 1);

            _listBox = new NoActivateListBox
            {
                BorderStyle = BorderStyle.None, // .FixedSingle,
                IntegralHeight = false,
                SelectionMode = SelectionMode.One,
                TabStop = false,
                DrawMode = DrawMode.OwnerDrawFixed,
                BackColor = metrics.BackColor,
                Dock = DockStyle.Fill,
            };

            // 固定项高度，留出一点内边距
            // _listBox.ItemHeight = Math.Max(18, this.Font.Height + 6);
            var font_height = Math.Max(
                _value_font == null ? _listBox.Font.Height : _value_font.Height,
                _comment_font == null ? _listBox.Font.Height : _comment_font.Height);
            _listBox.ItemHeight = font_height + 6;

            // 自绘事件
            _listBox.DrawItem += ListBox_DrawItem;
            _listBox.MeasureItem += ListBox_MeasureItem;

            _listBox.MouseClick += (s, e) =>
            {
                AcceptSelected();
            };
            _listBox.DoubleClick += (s, e) =>
            {
                AcceptSelected();
            };
            // 设法让本 Form 获得输入焦点后重新把焦点切换回 MarcControl
            this.GotFocus += (s, e) =>
            {
                // 为了解决在 _listBox 出现卷滚条时点了一下卷滚条之后无法 Esc 关闭小窗口的问题
                this.BeginInvoke(new Action(() =>
                {
                    // _focus_owner?.Focus();
                }));
            };
            /*
            _listBox.KeyDown += (s, e) =>
            {
                switch (e.KeyCode)
                {
                    case Keys.Enter:
                    case Keys.Tab:
                        // 确认当前选中项
                        AcceptSelected();
                        e.Handled = true;
                        return;
                    case Keys.Escape:
                        this.Cancel();
                        e.Handled = true;
                        return;
                }
            };
            _listBox.KeyPress += (s, e) =>
            {
                if (e.KeyChar == (char)Keys.Escape)
                {
                    this.Cancel();
                    e.Handled = true;
                }
            };
            */
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

        public void SetItems(IEnumerable<ValueItem> items,
            string selected_item_text)
        {
            if (AutoResize)
                ResizeToFit(items);

            // var arr = items?.ToArray() ?? new object[0];
            _listBox.BeginUpdate();
            try
            {
                _listBox.Items.Clear();
                _listBox.SelectedIndex = -1;
                int i = 0;
                foreach (var s in items)
                {
                    _listBox.Items.Add(s);
                    if (s.Value == selected_item_text)
                        _listBox.SelectedIndex = i;
                    i++;
                }
                //if (_listBox.SelectedIndex == -1)
                //    _listBox.SelectedIndex = _listBox.Items.Count > 0 ? 0 : -1;
            }
            finally
            {
                _listBox.EndUpdate();
            }
        }

        int _left_width = 0;
        int _right_width = 0;

        int _left_padding = 6;
        int _right_padding = 6;
        int _focus_width = 4;
        int _seperator_width = 8;


        void ResizeToFit(IEnumerable<ValueItem> items)
        {
            int leftColMin = 120;
            int rightColMin = 240;
            int leftWidth = leftColMin;
            int rightWidth = rightColMin;

            Rectangle screenBounds = Screen.FromControl(this).WorkingArea;
            int maxClientWidth = screenBounds.Width / 4;

            int count = 0;
            using (var g = _listBox.CreateGraphics())
            {
                foreach (var obj in items/*_listBox.Items.OfType<ValueItem>()*/)
                {
                    var left = obj.Value;   // GetLeftText(obj);
                    var right = obj.Comment;    // GetRightText(obj);

                    var leftSize = TextRenderer.MeasureText(g, left,
                        _value_font == null ? _listBox.Font : _value_font);
                    var rightSize = TextRenderer.MeasureText(g, right,
                        _comment_font == null ? _listBox.Font : _comment_font);

                    leftWidth = Math.Max(leftWidth, leftSize.Width);
                    rightWidth = Math.Max(rightWidth, rightSize.Width);

                    count++;
                }
            }

            _left_width = leftWidth;
            _right_width = rightWidth;

            _focus_width = _metrics?.GapThickness ?? 4;

            // 增加一些间距与滚动条宽度
            int padding = _left_padding + _right_padding;
            int totalW = _focus_width + leftWidth + _seperator_width + rightWidth + padding + SystemInformation.VerticalScrollBarWidth;
            int visibleCount = Math.Min(MaxVisibleItems, Math.Max(1, count/*_listBox.Items.Count*/));
            int h = visibleCount * _listBox.ItemHeight + 4;


            if (totalW > maxClientWidth)
            {
                _listBox.HorizontalScrollbar = true;
                // 加上滚动条宽度作为余量，避免文本被裁剪
                _listBox.HorizontalExtent = totalW + SystemInformation.VerticalScrollBarWidth;

                h += SystemInformation.HorizontalScrollBarHeight;
            }
            else
            {
                _listBox.HorizontalScrollbar = false;
            }

            // 限制总宽度
            totalW = Math.Min(maxClientWidth, totalW);

            //_listBox.Location = new Point(2, 2);
            //_listBox.Size = new Size(totalW - 4, h);
            this.ClientSize = new Size(totalW /*+ 2*/,
                h + 4);
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

        // 绘制每一项（左右两列）
        void ListBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= _listBox.Items.Count)
            {
                return;
            }

            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            bool selected = (e.State & DrawItemState.Selected) != 0;
            //Color back = selected ? SystemColors.Highlight : SystemColors.Window;
            //Color fore = selected ? SystemColors.HighlightText : SystemColors.WindowText;
            Color back = selected && _has_focus ? _metrics.HighlightBackColor : _metrics.BackColor;
            Color fore = selected && _has_focus ? _metrics.HightlightForeColor : _metrics.ForeColor;

            using (var b = new SolidBrush(back))
            {
                e.Graphics.FillRectangle(b, e.Bounds);
            }

            if (selected)
            {
                var focus_color = _metrics.FocusColor;
                using (var b = new SolidBrush(focus_color))
                {
                    var rect = new Rectangle(e.Bounds.X,
                        e.Bounds.Y,
                        _focus_width,   // _metrics.GapThickness,
                        e.Bounds.Height);
                    e.Graphics.FillRectangle(b, rect);
                }
            }

            var item = _listBox.Items[e.Index] as ValueItem;
            var left = item.Value.Replace(' ', _hilight_blank_char);  // GetLeftText(item);
            var right = item.Comment;   // GetRightText(item);

            // 左列从左边缘的偏移
            int leftX = e.Bounds.Left + _left_padding + _focus_width;

            TextFormatFlags leftFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;
            TextFormatFlags rightFlags = TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis;

            int rightX = leftX + _left_width + _seperator_width;   // 左右列之间的最小间隔 8

            var leftRect = new Rectangle(leftX, e.Bounds.Top, _left_width, e.Bounds.Height);
            var rightRect = new Rectangle(rightX, e.Bounds.Top, _right_width, e.Bounds.Height);

            TextRenderer.DrawText(e.Graphics,
                left,
                _value_font == null ? _listBox.Font : _value_font,
                leftRect,
                fore,
                leftFlags);

            // 右列使用灰色文字以示次要信息
            // var rightColor = selected ? SystemColors.HighlightText : SystemColors.GrayText;
            TextRenderer.DrawText(e.Graphics,
                right,
                _comment_font == null ? _listBox.Font : _comment_font,
                rightRect, fore/*rightColor*/,
                rightFlags);

            /*
            // 焦点矩形（如果需要）
            if ((e.State & DrawItemState.Focus) != 0)
                e.DrawFocusRectangle();
            */
        }

        // 保持固定高度即可，但保留 MeasureItem 以防以后扩展
        void ListBox_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            e.ItemHeight = _listBox.ItemHeight;
        }

#if REMOVED
        // 辅助：尽量从对象读取左列文本（优先 Value / Key / Code），否则 ToString()
        static string GetLeftText(object obj)
        {
            if (obj == null) return "";
            var t = obj.GetType();

            var candidates = new[] { "Value", "Key", "Code", "Name" };
            foreach (var n in candidates)
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    var v = p.GetValue(obj);
                    if (v != null) return v.ToString();
                }
            }

            // fallback
            return obj.ToString();
        }

        // 辅助：尽量从对象读取右列文本（优先 Caption / Text / Label / Description / SubValue）
        static string GetRightText(object obj)
        {
            if (obj == null) return "";
            var t = obj.GetType();

            var candidates = new[] { "Comment", "Caption", "Text", "Label", "Description", "SubValue", "Detail" };
            foreach (var n in candidates)
            {
                var p = t.GetProperty(n, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                if (p != null)
                {
                    var v = p.GetValue(obj);
                    if (v != null) return v.ToString();
                }
            }

            // fallback: if there's a Value property and ToString() returns that, try other properties
            // 最后退回空字符串（避免重复显示主文本）
            return "";
        }

#endif
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
            {
                _listBox.TopIndex = Math.Max(0, index - Math.Max(0, _listBox.Height / _listBox.ItemHeight / 2));
            }
        }

        public string GetSelectedItem()
        {
            return (_listBox.SelectedItem as dynamic)?.Value;
        }

        public void AcceptSelected()
        {
            var item = GetSelectedItem();
            if (item != null)
            {
                ItemChosen?.Invoke(this, item);
            }
            try { this.Hide(); } catch { }
        }

        public void Cancel()
        {
            Cancelled?.Invoke(this, EventArgs.Empty);
            try { this.Hide(); } catch { }
        }

        // Visible 包装
        public new bool Visible => base.Visible;
    }


    // 自定义 ListBox：在点击/滚动条时不激活窗口，但仍处理鼠标事件
    internal class NoActivateListBox : ListBox
    {
        const int WM_MOUSEACTIVATE = 0x0021;
        const int MA_NOACTIVATE = 3; // 不激活窗口并且不吃掉鼠标消息

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                // 返回 MA_NOACTIVATE：不激活窗口，但鼠标消息会继续被处理（滚动条可用）
                m.Result = new IntPtr(MA_NOACTIVATE);
                return;
            }
            base.WndProc(ref m);
        }
    }
}
