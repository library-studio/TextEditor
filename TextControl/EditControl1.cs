// #define CONTENT_STRING

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Imm32;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Usp10;


namespace LibraryStudio.Forms
{
    /// <summary>
    /// 一个通用的文本编辑器控件，实现了多行(段落)文本编辑的功能
    /// </summary>
    public partial class EditControl1 : UserControl
    {
        public event EventHandler SelectionChanged;

        public new event EventHandler TextChanged;

        History _history = new History(10 * 1024);

        // 客户区限制宽度。
        // -1 表示不限制宽度，也就是说宽度倾向于无限大
        // 0 表示自动跟随控件宽度变化;
        // 其它 具体的数字，表示宽度像素数
        int _clientBoundsWidth = 0;

        public int ClientBoundsWidth
        {
            get { return _clientBoundsWidth; }
            set
            {
                var old_value = _clientBoundsWidth;

                if (old_value != value)
                {
                    _clientBoundsWidth = value;

                    // 迫使重新布局 Layout
                    Relayout(_paragraph.MergeText());

                    _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标

                    // this.Invalidate();
                }
            }
        }


        public EditControl1()
        {
            InitializeComponent();

            // _line_height = Line.InitialFonts(this.Font);

            _context = new Context
            {
                SplitRange = (o, content) =>
                {
                    return SimpleText.SegmentSubfields(content);
                },
                GetFont = (o, t) =>
                {
                    return this.ContentFontGroup;
                }
            };
        }

        bool _initialized = false;

        bool _changed = false;

        public bool Changed
        {
            get { return _changed; }
            set { _changed = value; }
        }

        void SetChanged()
        {
            _changed = true;
            this.TextChanged?.Invoke(this, new EventArgs());
        }


        protected override void OnPaint(PaintEventArgs e)
        {
            var clipRect = e.ClipRectangle;

            using (var brush = new SolidBrush(SystemColors.Window))
            {
                e.Graphics.FillRectangle(brush, clipRect);
            }

            var handle = e.Graphics.GetHdc();
            var dc = new SafeHDC(handle);
            // var families = System.Drawing.FontFamily.Families;

            var old_mode = Gdi32.SetBkMode(dc, Gdi32.BackgroundMode.TRANSPARENT); // 设置背景模式为透明

            // var dc = CreateCompatibleDC(handle);
            /*
            var fnt = CreateFont(20, 
                iQuality: OutputQuality.PROOF_QUALITY,
                iPitchAndFamily: PitchAndFamily.DEFAULT_PITCH | PitchAndFamily.FF_ROMAN, 
                pszFaceName: "Times New Roman");
            //var brush = CreateSolidBrush(new COLORREF(0,0,0));
            */
            //dc.SelectObject(this.Font.ToHfont());
            //SelectBrush(dc, brush);
            try
            {
                /*
                if (_initialized == false)
                {
                    InitializeContent(dc);
                    _initialized = true;
                }
                */
                Debug.Assert(_initialized == true);

                // Gdi32.GetViewportOrgEx(this.Handle, out POINT p);

                int x = -this.HorizontalScroll.Value;  //  this.LeftBlank + this.DocumentOrgX;
                int y = -this.VerticalScroll.Value;  //  this.TopBlank + this.DocumentOrgY;


                _paragraph.Paint(
                    _context,
                    dc,
                    x,
                    y,
                    clipRect,
                    _blockOffs1,
                    _blockOffs2,
                    0);
                _context.ClearFontCache();
#if REMOVED
                int current_start_offs = 0;
                var block_start = Math.Min(_blockOffs1, _blockOffs2);
                var block_end = Math.Max(_blockOffs1, _blockOffs2);
                foreach (var line in _lines)
                {
                    DisplayLine(dc,
                        line,
                        x,
                        y,
                        block_start - current_start_offs,
                        block_end - current_start_offs);
                    y += _line_height;
                    current_start_offs += line.TextLength;
                }
#endif
            }
            finally
            {
                //brush.Dispose();
                // fnt.Dispose();
                e.Graphics.ReleaseHdc(handle);
                // Link.DisposeFonts(_fonts);
                e.Graphics.Dispose();
                dc.Dispose();
            }

            // base.OnPaint(e);
            // Custom painting code can go here
        }

        private int _global_offs = 0; // Caret 全局偏移量。

#if CONTENT_STRING
        private string _content = string.Empty;
#else
        private int _content_length = 0;
#endif

        public string Content
        {
            get
            {
#if CONTENT_STRING
                return _content?.Replace("\r", "\r\n");
#else
                return _paragraph.MergeText();
#endif
            }
            set
            {
#if CONTENT_STRING
                _content = value?.Replace("\r\n", "\r");
#endif

                // _initialized = false;
                Relayout(value?.Replace("\r\n", "\r"));

                // _caretInfo = new HitInfo();
                MoveCaret(HitByGlobalOffs(_global_offs));
                this.Invalidate();
                this._history.Clear();
            }
        }

        SCRIPT_PROPERTIES[] sp = null;
        SCRIPT_DIGITSUBSTITUTE sub;
        SCRIPT_CONTROL sc;
        SCRIPT_STATE ss;

        //SafeSCRIPT_CACHE _cache = null;

        // int _line_height = 20;

        HitInfo _caretInfo = new HitInfo();

        void InitializeEnvironment()
        {
            var result = ScriptGetProperties(out sp);
            result.ThrowIfFailed();

            result = ScriptRecordDigitSubstitution(LCID.LOCALE_CUSTOM_DEFAULT,
                out sub);
            result.ThrowIfFailed();

            result = ScriptApplyDigitSubstitution(sub,
                out sc,
                out ss);
            result.ThrowIfFailed();

            //_cache = new SafeSCRIPT_CACHE();

        }

        // List<Line> _lines = new List<Line>();
        IBox _paragraph = new SimpleText();

        void InitializeContent(SafeHDC dc,
            string text,
            bool auto_adjust_global_offs = true)
        {
            if (text == null)   // string.IsNullOrEmpty(text)
            {
                _paragraph.Clear();
                return;
            }

            // 自动调整 _global_offs 的范围
            if (auto_adjust_global_offs)
            {
                if (_global_offs > text.Length)
                    _global_offs = text.Length;
                if (_blockOffs1 > text.Length)
                    _blockOffs1 = text.Length;
                if (_blockOffs2 > text.Length)
                    _blockOffs2 = text.Length;
            }

#if REMOVED
            /*
    public static extern HRESULT ScriptItemize([MarshalAs(UnmanagedType.LPWStr)] string pwcInChars,
            int cInChars, int cMaxItems, [Optional][In] IntPtr psControl, [Optional][In] IntPtr psState, [Out][MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 2)] SCRIPT_ITEM[] pItems, out int pcItems);
             * 
             * */

            if (sp == null)
                InitializeEnvironment();

            int cMaxItems = _content.Length + 1;
            var pItems = new SCRIPT_ITEM[cMaxItems + 1];

            /*
             * 

返回值
如果成功，则返回 0。 如果函数不成功，则返回非零 HRESULT 值。

如果 pwcInChars 设置为 NULL、 cInChars 为 0、 pItems 设置为 NULL 或 cMaxItems< 2，则该函数将返回E_INVALIDARG。

如果 cMaxItems 的值不足，函数将返回E_OUTOFMEMORY。 与所有错误情况一样，不会完全处理任何项，并且输出数组中没有任何部分包含定义的值。 如果函数返回E_OUTOFMEMORY，则应用程序可以使用更大的 pItems 缓冲区再次调用它。
            * */
            var result = ScriptItemize(_content,
                _content.Length,
                cMaxItems,
                sc,
                ss,
                pItems,
                out int pcItems);
            result.ThrowIfFailed();

            Array.Resize(ref pItems, pcItems);
            for (int i = 0; i < pcItems; i++)
            {
                var item = pItems[i];
                if (sp[item.a.eScript].fComplex)
                {
                    // requiring glyph shaping
                }
                else
                {

                }
            }


            _lines = SplitLines(dc,// e.Graphics.GetHdc(),
                pItems,
                _content,
                (long)Math.Max(this.ClientSize.Width, 30));
#endif
            /*
            var max_pixel_width = _paragraph.Initialize(dc,
                text,
                Math.Max(this.ClientSize.Width, 30),
                (content) =>
                {
                    return SimpleText.SplitSubfields(content);
                });
            */
            var max_pixel_width = 0;
            var ret = _paragraph.ReplaceText(
                _context,
                dc,
                0,
                -1,
                text,
                _clientBoundsWidth == 0 ? this.ClientSize.Width : _clientBoundsWidth   // Math.Max(this.ClientSize.Width, 30),
                /*,
                out string _,
                out Rectangle update_rect,
                out Rectangle scroll_rect,
                out int scroll_distance*/);

            _context.ClearFontCache();

            max_pixel_width = ret.MaxPixel;
            var update_rect = ret.UpdateRect;
            var scroll_rect = ret.ScrollRect;
            var scroll_distance = ret.ScrolledDistance;

            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;

            if (scroll_distance != 0)
            {
                scroll_rect.Offset(x, y);
                User32.ScrollWindowEx(this.Handle,
                                    0,
                                    scroll_distance,
                                    scroll_rect,
                                    null,
                                    HRGN.NULL,
                                    out _,
                                    ScrollWindowFlags.SW_INVALIDATE);
            }
            if (update_rect.IsEmpty == false)
            {
                update_rect.Offset(x, y);
                this.Invalidate(update_rect);
            }

#if CONTENT_STRING
            _content = _paragraph.MergeText();
#else
            _content_length = _paragraph.TextLength;
#endif

            this.SetAutoSizeMode(AutoSizeMode.GrowAndShrink);
            this.AutoScrollMinSize = new Size(max_pixel_width, _paragraph.GetPixelHeight());


            if (true/* this.Focused */)
            {
                /*
                _caretInfo = HitByGlobalOffs(_global_offs);
                User32.HideCaret(this.Handle);
                User32.SetCaretPos(_caretInfo.X, _caretInfo.Y);
                User32.ShowCaret(this.Handle);
                */
                MoveCaret(HitByGlobalOffs(_global_offs), false);
            }
        }



#if REMOVED
        void ProcessOneItem(
            SCRIPT_ITEM item,
            string text,
            int[] piAdvance,
            long item_width,    // item 的像素宽度
            int line_width, // 行宽像素数
            List<Line> lines,
            ref Line line,
            ref int start_pixel)
        {
            for (; ; )
            {
                if (start_pixel + item_width <= line_width)
                {
                    if (line == null)
                        line = new Line { Ranges = new List<Range>() };

                    line.Ranges.Add(new Range
                    {
                        Item = item,
                        a = item.a,
                        Font = used_font,
                        Text = text,
                        PixelWidth = (int)item_width
                    });
                    start_pixel += (int)item_width;
                    return;
                }

                // 寻找可以 break 的位置
                // return:
                //      返回 str 中可以切割的 index 位置。如果为 -1，表示没有找到可以切割的位置。
                var pos = BreakItem(
                    item.a,
piAdvance,
text,
line_width - start_pixel,
out long left_width);
                if (pos == -1)
                {
                    // 如果本行一个 item 都没有，那就把这个 item 放在本行
                    if (line.Ranges.Count == 0)
                    {
                        line.Ranges.Add(new Range
                        {
                            Item = item,
                            a = item.a,
                            Font = used_font,
                            Text = text.Substring(0, pos),
                            PixelWidth = (int)left_width
                        });
                        start_pixel += (int)item_width;
                        return;
                    }
                    else
                    {
                        // 否则给下一行
                        if (line != null)
                            lines.Add(line);
                        line = new Line { Ranges = new List<Range>() };
                        line.Ranges.Add(new Range
                        {
                            Item = item,
                            a = item.a,
                            Font = used_font,
                            Text = text,
                            PixelWidth = (int)item_width
                        });
                        start_pixel = (int)item_width;
                    }
                }
                else
                {
                    // 从 pos 位置开始切割
                    Debug.Assert(pos >= 0 && pos < text.Length, "pos must be within the bounds of the text string.");

                    // 左边部分
                    if (pos > 0)
                    {
                        if (line == null)
                            line = new Line { Ranges = new List<Range>() };
                        line.Ranges.Add(new Range
                        {
                            Item = item,
                            a = item.a,
                            Font = used_font,
                            Text = text.Substring(0, pos),
                            PixelWidth = (int)left_width
                        });

                        text = text.Substring(pos);
                        piAdvance = piAdvance.Skip(pos).ToArray();
                        item_width = item_width - left_width;
                    }
                    lines.Add(line);
                    start_pixel += (int)item_width;

                    line = new Line { Ranges = new List<Range>() };
                    start_pixel = 0;

                    // 继续循环
                }

            }
        }
#endif




#if REMOVED
        // return:
        //      返回 text 中可以切割的 index 位置。如果为 -1，表示没有找到可以切割的位置。
        int BreakItem(//SCRIPT_ITEM range,
            SCRIPT_ANALYSIS sa,
            int[] piAdvance,
            string str,
            long pixel_width,
            out int left_width)
        {
            left_width = 0;
            sa = new SCRIPT_ANALYSIS();

            SCRIPT_LOGATTR[] array = new SCRIPT_LOGATTR[str.Length];
            ScriptBreak(str, str.Length, sa, array);

            int start = 0;
            for (int i = 0; i < piAdvance.Length; i++)
            {
                int tail = start + piAdvance[i];
                if (i > 0   // 避免 i == 0 时返回。确保至少有一个字符被切割
                    && tail > pixel_width
                    && (array[i].fSoftBreak || array[i].fWhiteSpace || array[i].fCharStop))
                {
                    left_width = start;
                    return i;
                }
                start += piAdvance[i];
            }

            return str.Length;
        }
#endif
        FontContext _fonts = null;
        IEnumerable<Font> ContentFontGroup
        {
            get
            {
                if (_fonts == null)
                    _fonts = new FontContext(this.Font);
                return _fonts.Fonts;
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            // _line_height = Line.InitialFonts(this.Font);
            {
                _fonts?.Dispose();
                _fonts = null;
            }

            // _fieldProperty.Refresh();

            // 迫使重新计算行高，重新布局 Layout
            Relayout(_paragraph.MergeText());

            // 重新创建一次 Caret，改变 caret 高度
            if (_caretCreated)
            {
                User32.DestroyCaret();
                User32.CreateCaret(this.Handle, new HBITMAP(IntPtr.Zero), 2/*_line_height / 5*/, _caretInfo.LineHeight == 0 ? this.Font.Height : _caretInfo.LineHeight);
                if (this.Focused)
                    User32.ShowCaret();
            }

            base.OnFontChanged(e);
        }

        protected override void OnResize(EventArgs e)
        {
            if (_clientBoundsWidth == 0)
            {
                // 迫使重新布局 Layout
                Relayout(_paragraph.MergeText());

                _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标

                this.Invalidate();
            }

            base.OnResize(e);
        }

        void Relayout(
            string text,
            bool auto_adjust_global_offs = true)
        {
            // if (_initialized == false)
            {
                using (var g = this.CreateGraphics())
                {
                    var handle = g.GetHdc();
                    using (var dc = new SafeHDC(handle))
                    {
                        InitializeContent(dc, text, auto_adjust_global_offs);
                    }
                    _initialized = true;
                }
            }
        }

        IContext _context = null;

        void ReplaceText(int start,
            int end,
            string text,
            bool auto_adjust_global_offs = true,
            bool add_history = true)
        {
            // if (_initialized == false)
            {
                using (var g = this.CreateGraphics())
                {
                    var handle = g.GetHdc();
                    using (var dc = new SafeHDC(handle))
                    {
                        var max_pixel_width = 0;
                        var ret = _paragraph.ReplaceText(
                            _context,
                            dc,
                            start,
                            end,
                            text,
                            _clientBoundsWidth == 0 ? this.ClientSize.Width : _clientBoundsWidth   // Math.Max(this.ClientSize.Width, 30),
                            /*,
                            out string replaced_text,
                            out Rectangle update_rect,
            out Rectangle scroll_rect,
            out int scroll_distance*/);

                        _context.ClearFontCache();

                        max_pixel_width = ret.MaxPixel;
                        var replaced_text = ret.ReplacedText;
                        var update_rect = ret.UpdateRect;
                        var scroll_rect = ret.ScrollRect;
                        var scroll_distance = ret.ScrolledDistance;

                        int x = -this.HorizontalScroll.Value;
                        int y = -this.VerticalScroll.Value;

                        if (scroll_distance != 0)
                        {
                            scroll_rect.Offset(x, y);
                            User32.ScrollWindowEx(this.Handle,
                                                0,
                                                scroll_distance,
                                                scroll_rect,
                                                null,
                                                HRGN.NULL,
                                                out _,
                                                ScrollWindowFlags.SW_INVALIDATE);
                        }
                        if (update_rect.IsEmpty == false)
                        {
                            update_rect.Offset(x, y);
                            this.Invalidate(update_rect);
                        }

#if CONTENT_STRING
                        // TODO: 这里值得改进加速速度。可以考虑仅仅保留一个 content length 即可
                        _content = _paragraph.MergeText();
#else
                        _content_length = _paragraph.TextLength;
#endif

                        // TODO: SetAutoSizeMode() 放到一个统一的初始化代码位置即可
                        this.SetAutoSizeMode(AutoSizeMode.GrowAndShrink);
                        this.AutoScrollMinSize = new Size(max_pixel_width, _paragraph.GetPixelHeight());

                        // 自动调整 _global_offs 的范围
                        if (auto_adjust_global_offs)
                        {
                            int delta = text.Length - (end - start);
                            if (_global_offs >= start)
                                _global_offs += delta;
                            if (_blockOffs1 >= start)
                                _blockOffs1 += delta;
                            if (_blockOffs2 >= start)
                                _blockOffs2 += delta;
                        }

                        if (this.Focused)
                        {
                            MoveCaret(HitByGlobalOffs(_global_offs), false);
                        }

                        if (add_history)
                        {
                            _history.Memory(new EditAction
                            {
                                Start = start,
                                End = end,
                                OldText = replaced_text,
                                NewText = text,
                            });
                        }
                    }
                    _initialized = true;

                    SetChanged();
                }
            }
        }

        bool _caretCreated = false;

        protected override void OnGotFocus(EventArgs e)
        {
            if (_caretCreated == false)
            {
                // Create a solid black caret. 
                User32.CreateCaret(this.Handle, new HBITMAP(IntPtr.Zero), 2/*_line_height / 5*/, _caretInfo.LineHeight == 0 ? this.Font.Height : _caretInfo.LineHeight);
                _caretCreated = true;
            }

            // Display the caret. 
            User32.ShowCaret(this.Handle);

            // Adjust the caret position, in client coordinates. 
            User32.SetCaretPos(-this.HorizontalScroll.Value + _caretInfo.X,
                -this.VerticalScroll.Value + _caretInfo.Y);

            SetCompositionWindowPos();

            base.OnGotFocus(e);
        }

        protected override void OnLostFocus(EventArgs e)
        {
            User32.HideCaret(this.Handle);
            if (_caretCreated)
            {
                User32.DestroyCaret();
                _caretCreated = false;
            }
            base.OnLostFocus(e);
        }

        bool _isMouseDown = false;
        int _blockOffs1 = -1;   // 选中范围开始的偏移量
        int _blockOffs2 = -1;   // 选中范围的结束的偏移量

        public int BlockStartOffset
        {
            get { return _blockOffs1; }
            //get { return Math.Min(_blockOffs1, _blockOffs2); }
        }

        public int BlockEndOffset
        {
            get { return _blockOffs2; }
            //get { return Math.Max(_blockOffs1, _blockOffs2); }
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            this.Capture = true;
            this._isMouseDown = true;
            // ChangeCaretPos(e.X, e.Y);
            {
                var result = _paragraph.HitTest(
                    e.X + this.HorizontalScroll.Value,
                    e.Y + this.VerticalScroll.Value);
#if DEBUG
                // Debug.Assert(result.Offs == _paragraph.GetGlobalOffs(result));
#endif
                _global_offs = result.Offs;

#if DEBUG
                Debug.Assert(_global_offs <= _content_length);
#endif

                /*
                var save_offs1 = _blockOffs1;
                var save_offs2 = _blockOffs2;
                */
                DetectBlockChange1(_blockOffs1, _blockOffs2);

                _blockOffs1 = _global_offs;
                _blockOffs2 = _global_offs;

                // 块定义发生了变化，刷新显示
                /*
                if (save_offs1 != _blockOffs1
                    || save_offs2 != _blockOffs2)
                {
                    // this.Invalidate();
                    InvalidateBlock(save_offs1, _blockOffs1);
                    InvalidateBlock(save_offs2, _blockOffs2);
                }
                */
                InvalidateBlock();
                /*
                _caretInfo = result;

                User32.HideCaret(this.Handle);
                User32.SetCaretPos(_caretInfo.X, _caretInfo.Y);
                User32.ShowCaret(this.Handle);
                */
                MoveCaret(result);
            }
            base.OnMouseDown(e);
        }

        void MoveCaret(HitInfo result,
            bool ensure_caret_visible = true)
        {
            /*
            if (result.LineHeight == 0)
                return;
            Debug.Assert(result.LineHeight != 0);
            */
            _caretInfo = result;

            if (ensure_caret_visible)
                EnsureCaretVisible();

            if (_caretCreated)
            {
                User32.HideCaret(this.Handle);
                User32.SetCaretPos(-this.HorizontalScroll.Value + _caretInfo.X,
                    -this.VerticalScroll.Value + _caretInfo.Y);
                User32.ShowCaret(this.Handle);
            }

            SetCompositionWindowPos();
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            // 有可能没有经过 OnMouseDown()，一上来就是 OnMouseUp()，这多半是别的窗口操作残留的消息
            if (this._isMouseDown == false)
            {
                base.OnMouseDown(e);
                return;
            }

            this.Capture = false;
            this._isMouseDown = false;

            // 选中范围结束
            {
                DetectBlockChange1(_blockOffs1, _blockOffs2);
                var old_offs = _global_offs;
                var result = _paragraph.HitTest(
                    e.X + this.HorizontalScroll.Value,
                    e.Y + this.VerticalScroll.Value);
                if (old_offs != result.Offs)
                    _global_offs = result.Offs; // _paragraph.GetGlobalOffs(result);

                {
                    _blockOffs2 = _global_offs;

                    var changed = DetectBlockChange2(_blockOffs1, _blockOffs2);

                    MoveCaret(result);

                    _lastX = _caretInfo.X; // 记录最后一次点击鼠标 x 坐标

                    if (changed)
                    {
                        //this.BlockChanged?.Invoke(this, new EventArgs());

                        // TODO: 可以改进为只失效影响到的 Line
                        // this.Invalidate(); // 重绘
                        InvalidateBlock();
                    }
                }
            }
            base.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isMouseDown)
            {
                var result = _paragraph.HitTest(
                    e.X + this.HorizontalScroll.Value,
                    e.Y + this.VerticalScroll.Value);
                // _global_offs = _paragraph.GetGlobalOffs(result);
                _global_offs = result.Offs;

                if (_blockOffs2 != _global_offs)
                {
                    /*
                    var save_offs1 = _blockOffs1;
                    var save_offs2 = _blockOffs2;
                    */
                    DetectBlockChange1(_blockOffs1, _blockOffs2);

                    _blockOffs2 = _global_offs;
                    /*
                    _caretInfo = result;

                    User32.HideCaret(this.Handle);
                    User32.SetCaretPos(_caretInfo.X, _caretInfo.Y);
                    User32.ShowCaret(this.Handle);
                    */
                    MoveCaret(result);
                    //this.BlockChanged?.Invoke(this, new EventArgs());

                    // TODO: 可以改进为只失效影响到的 Line
                    // this.Invalidate(); // 重绘
                    InvalidateBlock();
                }
            }

            base.OnMouseMove(e);
        }

        bool _shiftPressed = false; // Shift 键是否按下

        int _lastX = 0;  // 最后一次左右移动，点击设置插入符的位置信息。用于确定上下移动的初始 x 值

        // 第一次上下左右键，设置 _blockOffs1
        // 后继的上下左右键，设置 _blockOffs2

        protected override void OnKeyDown(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                    {

                        /*
                        _global_offs = Math.Max(0, _global_offs - 1);
                        MoveCaret(_paragraph.HitByGlobalOffs(_global_offs, false));
                        */
                        DetectBlockChange1(_blockOffs1, _blockOffs2);

                        HitInfo info = null;
                        int ret = 0;
                        if (HasBlock() && _shiftPressed == false)
                        {
                            int offs;
                            if (e.KeyCode == Keys.Left)
                                offs = Math.Min(_blockOffs1, _blockOffs2);  // 到块的头部
                            else
                                offs = Math.Max(_blockOffs1, _blockOffs2);

                            _blockOffs1 = offs;
                            _blockOffs2 = offs;
                            _global_offs = offs;

                            ret = _paragraph.MoveByOffs(_global_offs + (e.KeyCode == Keys.Left ? 1 : -1),
                                e.KeyCode == Keys.Left ? -1 : 1,
                                out info);
                        }
                        else
                        {
                            ret = _paragraph.MoveByOffs(_global_offs,
                                e.KeyCode == Keys.Left ? -1 : 1,
                                out info);
                        }

                        if (ret != 0)
                            break;
                        MoveCaret(info);


                        _global_offs = info.Offs;

                        if (_shiftPressed)
                            _blockOffs2 = _global_offs;
                        else
                        {
                            _blockOffs1 = _global_offs;
                            _blockOffs2 = _global_offs;
                        }
                        _lastX = _caretInfo.X; // 记录最后一次左右移动的 x 坐标

                        // this.Invalidate();  // TODO: 优化为失效具体的行。失效范围可以根据 offs1 -- offs2 整数可以设法直接提供给 Paint() 函数，用以替代 Rectangle
                        InvalidateBlock();
                    }
                    break;
#if REMOVED
                case Keys.Right:
                    {
                        /*
                        _global_offs = Math.Min(text.Length, _global_offs + 1);
                        MoveCaret(_paragraph.HitByGlobalOffs(_global_offs, false));
                        */
                        DetectBlockChange1(_blockOffs1, _blockOffs2);

                        var ret = _paragraph.MoveByOffs(_global_offs, 1, out HitInfo info);
                        if (ret != 0)
                            break;
                        MoveCaret(info);


                        _global_offs = info.Offs;

                        if (_shiftPressed)
                            _blockOffs2 = _global_offs;
                        else
                        {
                            _blockOffs1 = _global_offs;
                            _blockOffs2 = _global_offs;
                        }
                        _lastX = _caretInfo.X; // 记录最后一次左右移动的 x 坐标

                        /*
                        if (DetectBlockChange2(_blockOffs1, _blockOffs2))
                            this.Invalidate();
                        */
                        InvalidateBlock();
                    }
                    break;

#endif
                case Keys.Up:
                    // 向上移动一个行
                    {
                        var ret = _paragraph.CaretMoveUp(_lastX,
                            _caretInfo.Y,
                            out HitInfo temp_info);
                        if (ret == true)
                        {
                            DetectBlockChange1(_blockOffs1, _blockOffs2);

                            _global_offs = temp_info.Offs;
                            MoveCaret(temp_info);
                            if (_shiftPressed)
                                _blockOffs2 = _global_offs;
                            else
                            {
                                _blockOffs1 = _global_offs;
                                _blockOffs2 = _global_offs;
                            }
                            // this.Invalidate();
                            InvalidateBlock();
                        }
                    }
                    break;
                case Keys.Down:
                    // 向下移动一个行
                    {
                        var ret = _paragraph.CaretMoveDown(_lastX,
                            _caretInfo.Y,
                            out HitInfo temp_info);
                        if (ret == true)
                        {
                            DetectBlockChange1(_blockOffs1, _blockOffs2);

                            _global_offs = temp_info.Offs;
                            MoveCaret(temp_info);
                            if (_shiftPressed)
                                _blockOffs2 = _global_offs;
                            else
                            {
                                _blockOffs1 = _global_offs;
                                _blockOffs2 = _global_offs;
                            }
                            // this.Invalidate();
                            InvalidateBlock();
                        }
                    }
#if REMOVED
                    if (_paragraph.CanDown(_caretInfo)
                        /*_caretInfo.ChildIndex < _paragraph.LineCount*/)
                    {
                        var x = _lastX;
                        var y = _caretInfo.Y + _line_height;
                        var result = _paragraph.HitTest(x, y, _line_height);
                        _global_offs = _paragraph.GetGlobalOffs(result);
                        MoveCaret(result);

                        if (_shiftPressed)
                            _blockOffs2 = _global_offs;
                        else
                        {
                            _blockOffs1 = _global_offs;
                            _blockOffs2 = _global_offs;
                        }
                        this.Invalidate();
                    }
#endif
                    break;
                case Keys.ShiftKey:
                    _shiftPressed = true;
                    break;
                case Keys.Delete:
                    if (HasBlock())
                        RemoveBolckText();
                    else
                    {
                        // TODO: 可以考虑增加一个功能，Ctrl+Delete 删除一个 char。而不是删除一个不可分割的 cluster(cluster 可能包含若干个 char)
                        if (_global_offs < _content_length)
                        {
                            // 记忆起点 offs
                            var old_offs = _global_offs;
                            // 验证向右移动插入符
                            var ret = _paragraph.MoveByOffs(_global_offs, 1, out HitInfo info);
                            if (ret != 0)
                                break;

                            if (info.Offs > old_offs)
                            {
                                /*
                                // 删除一个或者多个字符
                                text = text.Remove(_global_offs, info.Offs - old_offs);
                                // 迫使重新计算行高，重新布局 Layout
                                Relayout(false); // 不改变 _global_offs 的值
                                */
                                ReplaceText(old_offs, info.Offs, "", false);
                                //this.Invalidate();
                            }
#if REMOVED
                            // 删除当前字符
                            _content = _content.Remove(_global_offs, 1);
                            // 迫使重新计算行高，重新布局 Layout
                            Relayout(false); // 不改变 _global_offs 的值

                            this.Invalidate();
#endif
                        }
                    }
                    e.Handled = true;
                    break;
                case Keys.Back:
                    if (HasBlock())
                        RemoveBolckText();
                    else
                    {
                        if (_global_offs > 0)
                        {
                            // 记忆起点 offs
                            var old_offs = _global_offs;
                            // 移动插入符
                            var ret = _paragraph.MoveByOffs(_global_offs, -1, out HitInfo info);
                            if (ret != 0)
                                break;
                            _global_offs = info.Offs;

                            /*
                            // 删除一个或者多个字符
                            text = text.Remove(_global_offs, old_offs - _global_offs);
                            // 迫使重新计算行高，重新布局 Layout
                            Relayout(false); // 不改变 _global_offs 的值
                            */
                            ReplaceText(_global_offs, old_offs, "", false);

                            //this.Invalidate();

                            // 重新调整一次 caret 位置。因为有可能在最后一行删除最后一个字符时突然行数减少
                            _paragraph.MoveByOffs(_global_offs, 0, out info);

                            MoveCaret(info);
                            _lastX = _caretInfo.X; // 记录最后一次左右移动的 x 坐标

#if REMOVED
                            // 删除前一个字符
                            _content = _content.Remove(_global_offs - 1, 1);
                            // 迫使重新计算行高，重新布局 Layout
                            Relayout(false); // 不改变 _global_offs 的值

                            DeltaGlobalOffs(-1);
                            this.Invalidate();
#endif
                        }
                    }
                    e.Handled = true;
                    return;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.ShiftKey:
                    _shiftPressed = false;
                    break;
            }
            base.OnKeyUp(e);
        }

        public const char KERNEL_SUBFLD = '▼';	// '‡';  子字段指示符内部代用符号

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
#if REMOVED
                case (char)Keys.Delete:
                    if (HasBlock())
                        RemoveBolckText();
                    else
                    {
                        if (_global_offs < _content.Length)
                        {
                            // 删除当前字符
                            _content = _content.Remove(_global_offs, 1);
                            // 迫使重新计算行高，重新布局 Layout
                            Relayout(false); // 不改变 _global_offs 的值

                            this.Invalidate();
                        }
                    }
                    e.Handled = true;
                    return;
#endif
                case (char)Keys.Escape:
                    break;
                /*
            case '\r':
            case '\n':
                break;
                */
                default:
                    {
                        // if (Imm32.ImmGetOpenStatus(hIMC))
                        {
                            if (e.KeyChar >= 32 || e.KeyChar == '\r')
                            {
                                if (HasBlock())
                                    RemoveBolckText();

                                if (e.KeyChar == '\\')
                                    e.KeyChar = KERNEL_SUBFLD;

                                /*
                                text = text.Insert(_global_offs, e.KeyChar.ToString());

                                // 迫使重新计算行高，重新布局 Layout
                                Relayout(false); // 不改变 _global_offs 的值
                                */
                                ReplaceText(_global_offs, _global_offs, e.KeyChar.ToString(), false);

                                // 向前移动一次 Caret
                                DeltaGlobalOffs(1);
                                e.Handled = true;
                            }
                        }
                    }
                    break;
            }

            base.OnKeyPress(e);
        }

        // 平移全局偏移量，和平移块范围
        bool DeltaGlobalOffs(int delta)
        {
            if (_global_offs + delta < 0)
                return false;

            DetectBlockChange1(_blockOffs1, _blockOffs2);

            var start_offs = _global_offs; // 记录开始偏移量

            _global_offs = Math.Max(0, Math.Min(
#if CONTENT_STRING
                _content.Length,
#else
                _content_length,
#endif
                _global_offs + delta));
            MoveCaret(HitByGlobalOffs(_global_offs));
            _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标

            // 平移块范围
            if (_blockOffs1 >= start_offs)
                _blockOffs1 += delta;
            if (_blockOffs2 >= start_offs)
                _blockOffs2 += delta;

            // 块定义发生刷新才有必要更新变化的区域
            InvalidateBlock();
            return true;
        }

        HitInfo HitByGlobalOffs(int offs)
        {
            _paragraph.MoveByOffs(offs, 0, out HitInfo info);
            return info;
        }

        public bool HasBlock()
        {
            return _blockOffs1 != _blockOffs2; // 选中范围不相等，表示有选中范围
        }

        // 删除块中的文字
        public bool RemoveBolckText()
        {
            if (_blockOffs1 == _blockOffs2)
                return false; // 不存在选中范围

            var start = Math.Min(_blockOffs1, _blockOffs2);
            var length = Math.Abs(_blockOffs1 - _blockOffs2);

            /*
            text = text.Remove(start, length);

            Relayout(false);
            */
            ReplaceText(start, start + length, "", false);

            _blockOffs1 = start;
            _blockOffs2 = start;

            if (_global_offs > start)
                DeltaGlobalOffs(-length); // 调整 _global_offs
            //Invalidate();
            return true;
        }

        public const int DLGC_WANTARROWS = 0x0001;      /* Control wants arrow keys         */
        public const int DLGC_WANTTAB = 0x0002;      /* Control wants tab keys           */
        public const int DLGC_WANTALLKEYS = 0x0004;      /* Control wants all keys           */
        public const int DLGC_WANTMESSAGE = 0x0004;      /* Pass message to control          */
        public const int DLGC_HASSETSEL = 0x0008;      /* Understands EM_SETSEL message    */
        public const int DLGC_DEFPUSHBUTTON = 0x0010;      /* Default pushbutton               */
        public const int DLGC_UNDEFPUSHBUTTON = 0x0020;     /* Non-default pushbutton           */
        public const int DLGC_RADIOBUTTON = 0x0040;      /* Radio button                     */
        public const int DLGC_WANTCHARS = 0x0080;      /* Want WM_CHAR messages            */
        public const int DLGC_STATIC = 0x0100;      /* Static range: don't include       */
        public const int DLGC_BUTTON = 0x2000;      /* Button range: can be checked      */

        public const int WM_IME_SETCONTEXT = 0x0281;
        /*
        IntPtr m_hImc;


        [DllImport("Imm32.dll")]
        public static extern IntPtr ImmGetContext(IntPtr hWnd);

        [DllImport("Imm32.dll")]
        public static extern IntPtr ImmAssociateContext(IntPtr hWnd, IntPtr hIMC);
        */

        protected override void DefWndProc(ref Message m)
        {
            if (this.DesignMode)
            {
                base.DefWndProc(ref m);
                return;
            }

            switch (m.Msg)
            {
                /*
                case (int)WindowMessage.WM_VSCROLL:
                    {
                        switch (Macros.LOWORD((uint)m.WParam.ToInt32()))
                        {
                            case (int)SBCMD.SB_BOTTOM:
                                //MessageBox.Show("SB_BOTTOM");
                                break;
                            case (int)SBCMD.SB_TOP:
                                //MessageBox.Show("SB_TOP");
                                break;
                            case (int)SBCMD.SB_THUMBTRACK:
                                this.Update();
                                DocumentOrgY = -Macros.HIWORD((uint)m.WParam.ToInt32());
                                break;
                            case (int)SBCMD.SB_LINEDOWN:
                                {
                                    DocumentOrgY -= _line_height;
                                }
                                break;
                            case (int)SBCMD.SB_LINEUP:
                                {
                                    DocumentOrgY += _line_height;
                                }
                                break;
                            case (int)SBCMD.SB_PAGEDOWN:
                                DocumentOrgY -= this.ClientHeight;
                                break;
                            case (int)SBCMD.SB_PAGEUP:
                                DocumentOrgY += this.ClientHeight;
                                break;
                        }
                    }
                    break;

                case (int)WindowMessage.WM_HSCROLL:
                    {
                        switch (Macros.LOWORD((uint)m.WParam.ToInt32()))
                        {
                            case (int)SBCMD.SB_THUMBPOSITION:
                            case (int)SBCMD.SB_THUMBTRACK:
                                DocumentOrgX = -Macros.HIWORD((uint)m.WParam.ToInt32());
                                break;
                            case (int)SBCMD.SB_LINEDOWN:
                                DocumentOrgX -= 20;
                                break;
                            case (int)SBCMD.SB_LINEUP:
                                DocumentOrgX += 20;
                                break;
                            case (int)SBCMD.SB_PAGEDOWN:
                                DocumentOrgX -= this.ClientSize.Width;
                                break;
                            case (int)SBCMD.SB_PAGEUP:
                                DocumentOrgX += this.ClientSize.Width;
                                break;
                        }
                    }
                    break;
                */
                // 要求容器 Form 把所有方向键发给本控件
                case (int)WindowMessage.WM_GETDLGCODE:
                    m.Result = new IntPtr(DLGC_WANTALLKEYS | DLGC_WANTARROWS | DLGC_WANTCHARS);
                    return;
                case WM_IME_SETCONTEXT:
                    {
                        // https://post.bytes.com/forum/topic/net/500566-how-to-enable-ime-on-a-custom-usercontrol
                        // the usercontrol will receive a WM_IME_SETCONTEXT message when it gets focused and loses focus respectively
                        // when the usercontrol gets focused, the m.WParam is 1
                        // when the usercontrol loses focus, the m.WParam is 0
                        // only when the usercontrol gets focused, we need to call the IMM function to associate itself to the default input context
                        if (m.WParam.ToInt32() == 1)
                        {
                            ImmAssociateContext(this.Handle, hIMC);
                        }
                    }
                    break;
                    /*
                case WM_IME_CONTROL:



                    ImeControlHandler(m);
                    break;
                    */
            }
            base.DefWndProc(ref m);
        }

        protected override void OnScroll(ScrollEventArgs se)
        {
            base.OnScroll(se);
            // this.Invalidate();
        }


        HIMC hIMC = IntPtr.Zero; // 输入法上下文句柄

        protected override void OnLoad(EventArgs e)
        {
            hIMC = Imm32.ImmGetContext(this.Handle);
            // _line_height = Line.InitialFonts(this.Font);
            SetCompositionWindowPos();
            base.OnLoad(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Imm32.ImmReleaseContext(this.Handle, hIMC);
            // User32.DestroyCaret();
            base.OnHandleDestroyed(e);
        }

        /*
        protected override bool CanEnableIme
        {
            get
            {
                return true;
            }
        }
        */

        #region Caret and hittest


        #endregion

        #region Content processing

        #endregion

        #region IME

        public void SetCompositionWindowPos()
        {
            if (hIMC != IntPtr.Zero)
            {
                // 只需要设置一次字体即可。字体改变时也要重新设置
                // 获取 LOGFONT 结构体指针
                var logFont = new LOGFONT();
                this.Font.ToLogFont(logFont);
                logFont.lfFaceName = this.Font.Name;
                logFont.lfHeight = (int)(this.Font.Height);
                /*
                var dpi_xy = GetDpiXY(this);
                logFont.lfHeight = GetScalingSize(dpi_xy, 0, logFont.lfHeight).Height;
                */
                // 设置输入法的合成字体
                Imm32.ImmSetCompositionFont(hIMC, logFont);

                var cf = new Imm32.COMPOSITIONFORM();
                cf.ptCurrentPos.x = _caretInfo.X;
                cf.ptCurrentPos.y = _caretInfo.Y;
                cf.dwStyle = CFS.CFS_POINT;

                Imm32.ImmSetCompositionWindow(hIMC, cf);
            }
        }

        public static SizeF GetDpiXY(Control control)
        {
            // testing
            // return new SizeF(192, 192);

            using (Graphics g = control.CreateGraphics())
            {
                return new SizeF(g.DpiX, g.DpiY);
            }
        }

        // 将 96DPI 下的长宽数字转换为指定 DPI 下的长宽值
        public static Size GetScalingSize(SizeF dpi_xy, int x, int y)
        {
            int width = Convert.ToInt32(x * (dpi_xy.Width / 96F));
            int height = Convert.ToInt32(y * (dpi_xy.Height / 96F));
            return new Size(width, height);
        }

        #endregion

        public void EnsureCaretVisible()
        {
            int x_delta = 0;
            int y_delta = 0;
            // 可见区域 左右边界
            var left = this.HorizontalScroll.Value;
            var right = this.HorizontalScroll.Value + this.ClientSize.Width;
            right -= 10;
            if (_caretInfo.X < left)
                x_delta = _caretInfo.X - left;
            else if (_caretInfo.X >= right)
                x_delta = _caretInfo.X - right;
            // 可见区域 上下边界
            var top = this.VerticalScroll.Value;
            var bottom = this.VerticalScroll.Value + this.ClientSize.Height;
            if (_caretInfo.Y < top)
                y_delta = _caretInfo.Y - top;
            else if (_caretInfo.Y + FontContext.DefaultFontHeight >= bottom)
                y_delta = _caretInfo.Y + FontContext.DefaultFontHeight - bottom;

            if (x_delta != 0 || y_delta != 0)
            {
                this.AutoScrollPosition = new Point(
                    this.HorizontalScroll.Value + x_delta,
                    this.VerticalScroll.Value + y_delta);

                if (_caretCreated)
                {
                    User32.HideCaret(this.Handle);
                    User32.SetCaretPos(-this.HorizontalScroll.Value + _caretInfo.X,
                        -this.VerticalScroll.Value + _caretInfo.Y);
                    User32.ShowCaret(this.Handle);
                }
            }
        }

        int _oldOffs1 = 0;
        int _oldOffs2 = 0;
        // 为了比较块定义是否发生变化，从而决定是否刷新显示，第一步，记忆信息
        void DetectBlockChange1(int offs1, int offs2)
        {
            _oldOffs1 = offs1;
            _oldOffs2 = offs2;
        }

        // 为了比较块定义是否发生变化，从而决定是否刷新显示，第二步，进行比较
        bool DetectBlockChange2(int offs1, int offs2)
        {
            // 新旧两种，都是无块定义
            if (_oldOffs1 == _oldOffs2
                && offs1 == offs2)
                return false;
            // 接上，此时说明新旧对比，至少有一个有快定义。
            // 那么头尾有任何变化，都算作块定义发生了变化
            if (_oldOffs1 != offs1 || _oldOffs2 != offs2)
                return true;
            return false;
        }

        void InvalidateBlock(bool trigger_event = true)
        {
            bool changed = false;
            if (InvalidateBlock(_oldOffs1, _blockOffs1))
                changed = true;
            if (InvalidateBlock(_oldOffs2, _blockOffs2))
                changed = true;

            if (trigger_event == true && changed)
            {
                this.SelectionChanged?.Invoke(this, new EventArgs());
            }
        }

        bool InvalidateBlock(int offs1, int offs2)
        {
            if (offs1 == offs2)
                return false;
            var rect = GetBlockRectangle(offs1, offs2);
            if (rect.IsEmpty == false)
            {
                this.Invalidate(rect);
                return true;
            }

            return false;
        }

        // 获得表示块大致范围的 Rectangle
        Rectangle GetBlockRectangle(int offs1, int offs2)
        {
            if (offs1 < 0 || offs2 < 0)
                return new Rectangle();

            if (offs1 == offs2)
                return new Rectangle();

            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;

            int start = Math.Min(offs1, offs2);
            int end = Math.Max(offs1, offs2);
            _paragraph.MoveByOffs(start, 0, out HitInfo info1);
            _paragraph.MoveByOffs(end, 0, out HitInfo info2);
            // return new Rectangle(0, 24, 5000, 24);
            if (info1.Y == info2.Y)
            {
                var left = Math.Min(info1.X, info2.X);
                var right = Math.Max(info1.X, info2.X);
                return new Rectangle(x + left,
                y + info1.Y,
                right - left,
                info2.Y + info2.LineHeight - info1.Y);
            }
            return new Rectangle(x + 0,
                y + info1.Y,
                this.ClientSize.Width,
                info2.Y + info2.LineHeight - info1.Y);
        }

        #region Edit Commands

        public bool Cut()
        {
            if (HasBlock() == false)
                return false;
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
            var text = this.Content.Substring(start, length).Replace("\r", "\r\n");
            Clipboard.SetText(text);
            RemoveBolckText();
            return true;
        }

        public bool Copy()
        {
            if (HasBlock() == false)
                return false;
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
            var text = this.Content.Substring(start, length).Replace("\r", "\r\n");
            Clipboard.SetText(text);
            // RemoveBolckText();
            return true;
        }

        public bool Paste()
        {
            var text = Clipboard.GetText()?.Replace("\r\n", "\r");
            if (string.IsNullOrEmpty(text))
                return false;
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
            this.ReplaceText(start, start + length, text);
            this.Select(start, start + text.Length, start);
            return true;
        }

        public void SelectAll()
        {
            _blockOffs1 = 0;
            _blockOffs2 = this._paragraph.TextLength;
            this.Invalidate();
        }

        // 选择一段文字
        public void Select(int start, int end, int caret_offs)
        {
            DetectBlockChange1(_blockOffs1, _blockOffs2);

            this._blockOffs1 = start;
            this._blockOffs2 = end;

            InvalidateBlock();

            if (caret_offs != _global_offs)
            {
                _global_offs = caret_offs;
                MoveCaret(HitByGlobalOffs(_global_offs), false);
            }
        }

        public bool CanUndo()
        {
            return _history.CanUndo();
        }

        public bool CanRedo()
        {
            return _history.CanRedo();
        }

        public bool Undo()
        {
            var action = _history.Back();
            if (action == null)
                return false;
            var start = Math.Min(action.Start, action.End);
            // var end = Math.Max(action.End, action.Start);
            var end = start + action.NewText.Length;
            ReplaceText(start,
                end,
                action.OldText,
                false,
                false);
            Select(start, start + action.OldText.Length, start);
            return true;
        }

        public bool Redo()
        {
            var action = _history.Forward();
            if (action == null)
                return false;
            var start = Math.Min(action.Start, action.End);
            var end = start + action.OldText.Length;
            ReplaceText(start,
                end,
                action.NewText,
                false,
                false);
            Select(start, start + action.NewText.Length, start);
            return true;
        }

        #endregion
    }




#if REMOVED
    class CaretInfo
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int LineIndex { get; set; } // 当前行
        public int RangeIndex { get; set; } // 当前 Range 的 index
        public int TextIndex { get; set; } // 当前文字的 index
    }
#endif
}
