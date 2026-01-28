using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Imm32;
using static Vanara.PInvoke.User32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// MARC 编辑器控件
    /// </summary>
    public partial class MarcControl : UserControl
    {
        public event EventHandler SelectionChanged;

        public event EventHandler CaretMoved;

        // public new event EventHandler TextChanged;

        History _history = new History(10 * 1024);

        public string DumpHistory()
        {
            return _history.ToString();
        }

        public void ClearHistory()
        {
            this._history?.Clear();
        }

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
                    Relayout(_record.MergeText());

                    _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标

                    // this.Invalidate();
                }
            }
        }

        private KeystrokeSpeedDetector _keySpeedDetector = new KeystrokeSpeedDetector(thresholdKeysPerSecond: 10.0, window: TimeSpan.FromSeconds(1));

        public MarcControl()
        {
            /*
            this.SetStyle(ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true);
            this.UpdateStyles();
            this.DoubleBuffered = true; // 备份保证
            */
            _context = GetDefaultContext();

            _marcMetrics = new Metrics()
            {
                GetReadOnly = (o) => this.ReadOnly,
            };
            _record = new MarcRecord(this, _marcMetrics);

            #region 延迟刷新
            CreateTimer();
            #endregion

            InitializeComponent();

            // _line_height = Line.InitialFonts(this.Font);

            _marcMetrics.Refresh(this.Font, this.FixedSizeFont);

            _dpiXY = DpiUtil.GetDpiXY(this);
        }

        void _paintBackfunc(object o,
            SafeHDC hdc,
            Rectangle rect,
            Rectangle clipRect)
        {
            var field = o as MarcField;
            if (o == null)
                return;
            rect.Width = this.AutoScrollMinSize.Width;
            field.PaintBackAndBorder(hdc, rect.X, rect.Y, clipRect);
        }

        FontContext _contentFonts = null;

        IEnumerable<Font> ContentFontGroup
        {
            get
            {
                if (_contentFonts == null)
                    _contentFonts = new FontContext(this.Font);
                return _contentFonts.Fonts;
            }
        }

        public override Font Font
        {
            get => base.Font;
            set
            {
                // 因为 fixed font 和 caption font 都是依赖于 base.Font 的字体尺寸和样式的，
                // 如果有变化，则要令它们重新构建
                if (base.Font.SizeInPoints != value.SizeInPoints)
                {
                    m_fixedSizeFont?.Dispose();
                    m_fixedSizeFont = null;
                    m_captionFont?.Dispose();
                    m_captionFont = null;
                }

                base.Font = value;
                // 这里会自动触发 OnFontChanged(sender, e)
            }
        }

        FontContext _fixedFonts = null;

        IEnumerable<Font> FixedFontGroup
        {
            get
            {
                if (_fixedFonts == null)
                    _fixedFonts = new FontContext(this.FixedSizeFont);
                return _fixedFonts.Fonts;
            }
        }

        // 等宽字体
        Font m_fixedSizeFont = null;

        public Font FixedSizeFont
        {
            get
            {
                if (this.m_fixedSizeFont == null)
                {
                    EnsureFixedSizeFont(this.Font);
#if REMOVED
                    float size = this.Font != null ? this.Font.SizeInPoints : 9f;
                    string familyName = "Courier New";
                    bool found = System.Drawing.FontFamily.Families.Any(f => string.Equals(f.Name, familyName, StringComparison.OrdinalIgnoreCase));
                    try
                    {
                        if (found)
                            this.m_fixedSizeFont = new Font(new System.Drawing.FontFamily(familyName), size, FontStyle.Bold, GraphicsUnit.Point);
                        else
                            this.m_fixedSizeFont = new Font(this.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, size, FontStyle.Bold, GraphicsUnit.Point);
                    }
                    catch
                    {
                        // 最后兜底，保证不抛出
                        this.m_fixedSizeFont = new Font(this.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, size, FontStyle.Bold, GraphicsUnit.Point);
                    }
#endif
                }
                return this.m_fixedSizeFont;
            }
            set
            {
                if (this.m_fixedSizeFont != null)
                    this.m_fixedSizeFont.Dispose();
                // this.m_fixedSizeFont = value;
                this.m_fixedSizeFont = new Font(value?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, value.SizeInPoints, value.Style, GraphicsUnit.Point);

                OnFontChanged();
            }
        }

        void EnsureFixedSizeFont(Font ref_font)
        {
            if (this.m_fixedSizeFont == null)
            {
                float size = ref_font?.SizeInPoints ?? 9f;
                string familyName = "Courier New";
                bool found = System.Drawing.FontFamily.Families.Any(f => string.Equals(f.Name, familyName, StringComparison.OrdinalIgnoreCase));
                try
                {
                    if (found)
                        this.m_fixedSizeFont = new Font(new System.Drawing.FontFamily(familyName), size, FontStyle.Bold, GraphicsUnit.Point);
                    else
                        this.m_fixedSizeFont = new Font(this.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, size, FontStyle.Bold, GraphicsUnit.Point);
                }
                catch
                {
                    // 最后兜底，保证不抛出
                    this.m_fixedSizeFont = new Font(ref_font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, size, FontStyle.Bold, GraphicsUnit.Point);
                }
            }
        }

        FontContext _captionFonts = null;

        IEnumerable<Font> CaptionFontGroup
        {
            get
            {
                if (_captionFonts == null)
                    _captionFonts = new FontContext(this.CaptionFont);
                return _captionFonts.Fonts;
            }
        }

        // 提示区字体
        Font m_captionFont = null;

        public Font CaptionFont
        {
            get
            {
                if (this.m_captionFont == null)
                {
                    EnsureCaptionFont(this.Font);
#if REMOVED
                    float size = this.Font != null ? this.Font.SizeInPoints : 9f;
                    string familyName = "楷体";
                    bool found = System.Drawing.FontFamily.Families.Any(f => string.Equals(f.Name, familyName, StringComparison.OrdinalIgnoreCase));
                    try
                    {
                        if (found)
                            this.m_captionFont = new Font(new System.Drawing.FontFamily(familyName), size, FontStyle.Regular, GraphicsUnit.Point);
                        else
                            this.m_captionFont = new Font(this.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, size, FontStyle.Regular, GraphicsUnit.Point);
                    }
                    catch
                    {
                        // 最后兜底，保证不抛出
                        this.m_captionFont = new Font(this.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, size, FontStyle.Bold, GraphicsUnit.Point);
                    }
#endif
                }
                return this.m_captionFont;
            }
            set
            {
                if (this.m_captionFont != null)
                    this.m_captionFont.Dispose();
                // this.m_captionFont = value;
                this.m_captionFont = new Font(value?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, value.SizeInPoints, value.Style, GraphicsUnit.Point);

                OnFontChanged();
            }
        }

        void EnsureCaptionFont(Font ref_font)
        {
            float size = ref_font?.SizeInPoints ?? 9f;
            string familyName = this.Font.FontFamily.Name;  //  "宋体";
            bool found = System.Drawing.FontFamily.Families.Any(f => string.Equals(f.Name, familyName, StringComparison.OrdinalIgnoreCase));
            try
            {
                if (found)
                    this.m_captionFont = new Font(new System.Drawing.FontFamily(familyName), size, FontStyle.Regular, GraphicsUnit.Point);
                else
                    this.m_captionFont = new Font(this.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, size, FontStyle.Regular, GraphicsUnit.Point);
            }
            catch
            {
                // 最后兜底，保证不抛出
                this.m_captionFont = new Font(this.Font?.FontFamily ?? SystemFonts.DefaultFont.FontFamily, size, FontStyle.Bold, GraphicsUnit.Point);
            }
        }

#if REMOVED
        void PaintBackAndBorder(Graphics g,
    int x,
    int y,
    int width,
    int height)
        {
            using (var brush = new SolidBrush(_fieldProperty?.BackColor ?? SystemColors.Window))
            {
                g.FillRectangle(brush, x, y, width, height);
            }
            PaintBorder(g, x, y, width, height);
        }
#endif


        public IContext GetContext()
        {
            return _context;
        }

        SizeF _dpiXY = new SizeF(96, 96);

        bool _initialized = false;

        bool _changed = false;

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public bool Changed
        {
            get { return _changed; }
            set { _changed = value; }
        }

        void SetChanged()
        {
            _changed = true;
            // this.TextChanged?.Invoke(this, new EventArgs());
            OnTextChanged(EventArgs.Empty);
        }

        bool _readonly = false;
        public bool ReadOnly
        {
            get
            {
                return _readonly;
            }
            set
            {
                var old_value = _readonly;
                _readonly = value;
                if (value != old_value)
                {
                    // 清除 DOM 结构中的所有 ColorCache，迫使重新获取颜色显示
                    this._record.ClearCache();
                    this.Invalidate();
                }
            }
        }

        #region 各种颜色

        /*
        private Color _backColor;

        public Color BackColor
        {
            get
            {
                return _backColor;
            }
            set
            {
                if (_backColor != value)
                {
                    _backColor = value;
                    this.Invalidate();
                }
        }
        }
        */

        #endregion


        protected override void OnPaint(PaintEventArgs e)
        {
            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;

            var clipRect = e.ClipRectangle;


#if REMOVED
            // 绘制背景色
            {

                var backColor = this._context?.GetBackColor?.Invoke(null, false) ?? SystemColors.Window;
                using (var brush = new SolidBrush(backColor))
                {
                    e.Graphics.FillRectangle(brush, clipRect);
                }

                var height = this.AutoScrollMinSize.Height - FontContext.DefaultFontHeight;
                // 绘制提示文字区的底色
                {
                    var left_rect = new Rectangle(
                            x,
                            y,
                            _fieldProperty.CaptionPixelWidth,
                            height);
                    if (clipRect.IntersectsWith(left_rect))
                    {
                        using (var brush = new SolidBrush(_fieldProperty?.CaptionBackColor ?? Metrics.DefaultCaptionBackColor))
                        {
                            e.Graphics.FillRectangle(brush, left_rect);
                        }
                    }
                }

                // Solid 区
                {
                    var left_rect = new Rectangle(
        x + _fieldProperty.CaptionPixelWidth,
        y,
        _fieldProperty.ContentBorderX - _fieldProperty.CaptionPixelWidth,
        height);
                    if (clipRect.IntersectsWith(left_rect))
                    {
                        using (var brush = new SolidBrush(_fieldProperty?.SolidColor ?? Metrics.DefaultSolidColor))
                        {
                            e.Graphics.FillRectangle(brush, left_rect);
                        }
                    }

                }

                // 右侧全高的一根立体竖线
                {
                    MarcField.PaintLeftRightBorder(e.Graphics,
        x + _fieldProperty.SolidX,
        y + 0,
        _fieldProperty.SolidPixelWidth,
        height,
        _fieldProperty.BorderThickness);
                }
            }
#endif

            var handle = e.Graphics.GetHdc();
            var dc = new SafeHDC(handle);
            // var families = System.Drawing.FontFamily.Families;

            this._record.PaintBack(
                _context,
                dc,
                x,
                y,
                clipRect,
                _caretInfo.ChildIndex);

            // var old_mode = Gdi32.SetBkMode(dc, Gdi32.BackgroundMode.TRANSPARENT); // 设置背景模式为透明

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

                _record.Paint(
                    _context,
                    dc,
                    x,
                    y,
                    clipRect,
                    _selectOffs1,
                    _selectOffs2,
                    0);
                _context.ClearFontCache();
            }
            finally
            {
                e.Graphics.ReleaseHdc(handle);
                // e.Graphics.Dispose();    // bug。不用 Dispose()。如果用了 Dispose()，则当 this.DoubleBuffer = true 时会抛出异常
                dc.Dispose();
            }

            // base.OnPaint(e);
            // Custom painting code can go here
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            // base.OnPaintBackground(e);
        }

        Rectangle GetFocusRect()
        {
            if (_caretInfo.ChildIndex >= this._record.FieldCount)
            {
                var index = this._record.FieldCount;
                return new Rectangle(_marcMetrics.NameBorderX,
                this._record.GetFieldY(index),
    AutoScrollMinSize.Width - _marcMetrics.NameBorderX,
    FontContext.DefaultFontHeight);
            }
            var field = this._record.GetField(_caretInfo.ChildIndex);
            return new Rectangle(_marcMetrics.NameBorderX,
                this._record.GetFieldY(_caretInfo.ChildIndex),
                AutoScrollMinSize.Width - _marcMetrics.NameBorderX,
                field.GetPixelHeight());
        }


        // private int _content_length = 0;

        public virtual string Content
        {
            get
            {
                return _record.MergeText();
            }
            set
            {

#if REMOVED
                // _initialized = false;
                Relayout(value, true);

                // _caretInfo = new HitInfo();
                MoveCaret(HitByGlobalOffs(_global_offs + 1, -1));
                this.Invalidate();
                this._history.Clear();
#endif
                // 不改变 Changed; 清除编辑历史
                SetContent(value,
                    set_changed: false,
                    clear_history: true);
            }
        }

        public void SetContent(string value,
            bool set_changed = false,
            bool clear_history = false)
        {
            string replaced_text = this._record.MergeText();
            value = CleanContent(value);
            Relayout(value, true);
            if (set_changed)
                SetChanged();
            MoveCaret(HitByCaretOffs(_caret_offs + 1, -1));
            this.Invalidate();
            if (clear_history)
            {
                this._history.Clear();
            }
            else
            {
                // 简化编辑历史
                var result = Utility.CompareTwoContent(value, replaced_text);
                if (result.StartLength == 0 && result.EndLength == 0)
                {
                    _history.Memory(new EditAction
                    {
                        Start = 0,
                        End = replaced_text.Length,
                        OldText = replaced_text,
                        NewText = value,
                    });
                }
                else
                {
                    _history.Memory(new EditAction
                    {
                        Start = result.StartLength,
                        End = replaced_text.Length - result.EndLength,
                        OldText = replaced_text.Substring(result.StartLength, replaced_text.Length - result.EndLength - result.StartLength),
                        NewText = value.Substring(result.StartLength, value.Length - result.EndLength - result.StartLength),
                    });
                }
            }
        }

        static string CleanContent(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";
            var index = text.IndexOf(Metrics.RecordEndCharDefault);
            if (index != -1)
                return text.Substring(0, index);
            return text;
        }

        /*
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public GetFieldCaptionFunc GetFieldCaption
        {
            get { return _marcMetrics.GetFieldCaption; }
            set
            {
                _marcMetrics.GetFieldCaption = value;
                InvalidateCaptionArea();
            }
        }
        */

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public GetStructureFunc GetStructure
        {
            get { return _marcMetrics.GetStructure; }
            set
            {
                _marcMetrics.GetStructure = value;
                // TODO: 不仅仅更新第一层 caption
                InvalidateCaptionArea();
            }
        }

        public void InvalidateCaptionArea()
        {
            if (_disableUpdate == 0)
            {
                // _context.ViewModeTree = _record.GetViewModeTree();
                using (var g = this.CreateGraphics())
                {
                    Rectangle update_rect = System.Drawing.Rectangle.Empty;

                    var handle = g.GetHdc();
                    using (var dc = new SafeHDC(handle))
                    {
                        _record.UpdateAllCaption(
                            _context,
                            dc,
                            out update_rect);
                    }
                    int x = -this.HorizontalScroll.Value;
                    int y = -this.VerticalScroll.Value;
                    if (update_rect != System.Drawing.Rectangle.Empty)
                    {
                        update_rect.Offset(x, y);
                        this.Invalidate(update_rect);
                    }
                }
            }
            else
            {
                _invalidateCount++;
            }
        }


        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public GetValueListFunc GetValueList
        {
            get { return _marcMetrics.GetValueList; }
            set
            {
                _marcMetrics.GetValueList = value;
            }
        }


        // List<Line> _lines = new List<Line>();
        MarcRecord _record = null;  // new MarcRecord(_fieldProperty);



        void InitializeContent(SafeHDC dc,
            string text,
            bool auto_adjust_global_offs = true)
        {
            if (text == null)   // string.IsNullOrEmpty(text)
            {
                _record.Clear();
                return;
            }

            // 当可能越出合法范围时，自动调整 _global_offs 的范围
            if (auto_adjust_global_offs)
            {
                if (_caret_offs > text.Length)
                {
                    // 注意这里没有 MoveCaret();
                    // SetCaretOffs(text.Length);
                    MoveCaret(HitByCaretOffs(text.Length), false);  // 2026/1/4
                }
                if (_selectOffs1 > text.Length)
                    _selectOffs1 = text.Length;
                if (_selectOffs2 > text.Length)
                    _selectOffs2 = text.Length;
            }

            var ret = _record.ReplaceText(
                _record.GetViewModeTree(),
                _context,
                dc,
                0,
                -1,
                text,
                GetLimitWidth()    //_clientBoundsWidth == 0 ? this.ClientSize.Width : _clientBoundsWidth   // Math.Max(this.ClientSize.Width, 30),
                );
            _context?.ClearFontCache();

            var max_pixel_width = ret.MaxPixel;
            var update_rect = ret.UpdateRect;
            var scroll_rect = ret.ScrollRect;
            var scroll_distance = ret.ScrolledDistance;

            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;

            if (scroll_distance != 0)
            {
                Utility.Offset(ref scroll_rect, x, y);
                if (scroll_rect.IsEmpty == false)
                {
                    User32.ScrollWindowEx(this.Handle,
                        0,
                        scroll_distance,
                        scroll_rect,
                        null,
                        HRGN.NULL,
                        out _,
                        ScrollWindowFlags.SW_INVALIDATE);
                    _invalidateCount++;
                }
            }
            Utility.Offset(ref update_rect, x, y);
            if (update_rect.IsEmpty == false)
            {
                this.Invalidate(update_rect);
                // ScheduleInvalidate(update_rect);
            }

            // _content_length = _record.TextLength;

            ChangeDocumentSize(max_pixel_width);

            if (true/* this.Focused */)
            {
                /*
                _caretInfo = HitByGlobalOffs(_global_offs);
                User32.HideCaret(this.Handle);
                User32.SetCaretPos(_caretInfo.X, _caretInfo.Y);
                User32.ShowCaret(this.Handle);
                */
                MoveCaret(HitByCaretOffs(_caret_offs + 1, -1), false);
            }
        }

        void ChangeDocumentSize(int max_pixel_width)
        {
            if (max_pixel_width > 0)
            {
                this.SetAutoSizeMode(AutoSizeMode.GrowAndShrink);
                if (_clientBoundsWidth == -1)
                    max_pixel_width = Math.Max(this.AutoScrollMinSize.Width, max_pixel_width);

                // .Height 设置后，可能会改变垂直卷滚条是否出现的状态，改变 AutoScrollMinSize.Width，会再次触发(OnResize() 中的) Relayout()
                this.AutoScrollMinSize = new Size(max_pixel_width, _record.GetPixelHeight());
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

        void DisposeFonts()
        {
            {
                _contentFonts?.Dispose();
                _contentFonts = null;

                m_fixedSizeFont?.Dispose();
                m_fixedSizeFont = null;

                _fixedFonts?.Dispose();
                _fixedFonts = null;

                m_captionFont?.Dispose();
                m_captionFont = null;

                _captionFonts?.Dispose();
                _captionFonts = null;
            }
        }

        void ClearFontGroups()
        {
            {
                _contentFonts?.Dispose();
                _contentFonts = null;

                _fixedFonts?.Dispose();
                _fixedFonts = null;

                _captionFonts?.Dispose();
                _captionFonts = null;
            }
        }

        protected override void OnFontChanged(EventArgs e)
        {
            OnFontChanged();

            base.OnFontChanged(e);
        }

        void OnFontChanged()
        {
            ClearFontGroups();

            _marcMetrics.Refresh(this.Font, this.FixedSizeFont);

            var text = _record.MergeText();
            this._record.Clear();   // 清除所有 IBox 对象，释放所有对原有字体的引用
            // 迫使重新计算行高，重新布局 Layout
            Relayout(text);

            // 重新创建一次 Caret，改变 caret 高度
            RecreateCaret();
            // TODO: 根据 _global_offs 重新定位一次 Caret。但遗憾的是 _lastX 应该改变，但尚无方法
        }

        // 大于 0 则表示禁止响应 OnResize()
        // 可以用于中途禁止响应 OnResize()，等一切确定以后再专门 Relayout() 一次
        int _disableResize = 0;

        int _lastWidth = 0;

        protected override void OnResize(EventArgs e)
        {
            if (_clientBoundsWidth == 0
                && _disableResize == 0)
            {
                var current_width = this.ClientSize.Width;
                if (current_width != _lastWidth)    // 减少大量无谓的调用
                {
                    Relayout(this._record.MergeText(), false);
                    _lastWidth = this.ClientSize.Width;
                }
            }

            base.OnResize(e);
        }

        /*
        public void Relayout()
        {
            // 迫使重新布局 Layout
            Relayout(_record.MergeText());

            _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标

            this.Invalidate();
        }
        */

        // 重新布局。
        // 注意本函数默认不会自动调整 Caret 和 Block Start End 数值
        void Relayout(
            string text,
            bool auto_adjust_global_offs = false)
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

        int GetLimitWidth()
        {
            return _clientBoundsWidth == 0 ? Math.Max(this.ClientSize.Width - _marcMetrics.GapThickness, 0) : _clientBoundsWidth;   // Math.Max(this.ClientSize.Width, 30),
        }

        // parameters:
        //      delay_update    希望延迟更新。方法是先更新一个小的区域，延迟更新原本大的区域
        public ReplaceTextResult ReplaceText(int start,
            int end,
            string text,
            bool delay_update,
            bool auto_adjust_caret_and_selection = true,
            bool add_history = true)
        {
            ReplaceTextResult ret = null;
            using (var g = this.CreateGraphics())
            {
                var handle = g.GetHdc();
                using (var dc = new SafeHDC(handle))
                {
                    ret = _record.ReplaceText(
                        _record.GetViewModeTree(),
                        _context,
                        dc,
                        start,
                        end,
                        text,
                        GetLimitWidth()
                        );
                    _context.ClearFontCache();
                }
            }

            var max_pixel_width = ret.MaxPixel;
            var replaced_text = ret.ReplacedText;
            if (ret.NewText != null)
                text = ret.NewText;
            var update_rect = ret.UpdateRect;
            var scroll_rect = ret.ScrollRect;
            var scroll_distance = ret.ScrolledDistance;

            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;

            if (scroll_distance != 0)
            {
                Utility.Offset(ref scroll_rect, x, y);
                if (scroll_rect.IsEmpty == false)
                {
                    User32.ScrollWindowEx(this.Handle,
                        0,
                        scroll_distance,
                        scroll_rect,
                        null,
                        HRGN.NULL,
                        out _,
                        ScrollWindowFlags.SW_INVALIDATE);
                    _invalidateCount++;
                }
            }

            if (text == null)
                text = "";

            // 输入一个字符时先快速更新
            var quick = false;
            if (delay_update
                && start == end && text.Length == 1)
            {
                var region = this._record.GetRegion(start, start + text.Length);
                if (region != null)
                {
                    region.Translate(x, y);
                    this.Invalidate(region);
                    region.Dispose();
                    quick = true;
                }
            }

            if (update_rect != System.Drawing.Rectangle.Empty)
            {
                update_rect.Offset(x, y);
                if (quick == false)
                    this.Invalidate(update_rect);
                else
                    ScheduleInvalidate(update_rect);
            }

            // _content_length = _record.TextLength;

            /*
            // TODO: SetAutoSizeMode() 放到一个统一的初始化代码位置即可
            this.SetAutoSizeMode(AutoSizeMode.GrowAndShrink);
            this.AutoScrollMinSize = new Size(max_pixel_width, _record.GetPixelHeight());
            */

            int new_caret_offs = -1;

            // 自动调整 _global_offs 的范围
            if (auto_adjust_caret_and_selection)
            {
                int e = start + replaced_text.Length;
                int delta = text.Length - (replaced_text.Length/*end - start*/);
                // SetCaretOffs(Adjust(_caret_offs, start, e, delta));
                new_caret_offs = Adjust(_caret_offs, start, e, delta);
                _selectOffs1 = Adjust(_selectOffs1, start, e, delta);
                _selectOffs2 = Adjust(_selectOffs2, start, e, delta);
            }

            // 会改变 _selectOffs2 的 bug 已经解决。临时禁用了 OnResize()
            ChangeDocumentSize(max_pixel_width);


            int Adjust(int v, int s, int e, int delta)
            {
                if (v >= e)
                    v += delta;  // 插入符在块尾部以右的，向左移动
                else if (v >= s)
                    v = s;   // 插入符在块中间的，归为块首
#if DEBUG
                Debug.Assert(v >= 0 && v <= _record.TextLength);
#endif
                return v;
            }

            if (/*this.Focused ||*/ new_caret_offs != -1)
            {
                if (new_caret_offs != -1)
                    MoveCaret(HitByCaretOffs(new_caret_offs + 1, -1), false);
                else
                    MoveCaret(HitByCaretOffs(_caret_offs + 1, -1), false);
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

            _initialized = true;

            SetChanged();

            return ret;
        }


        protected override void OnGotFocus(EventArgs e)
        {
            if (_caretCreated == false)
            {
#if REMOVED
                // Create a solid black caret. 
                User32.CreateCaret(this.Handle, new HBITMAP(IntPtr.Zero), 2/*_line_height / 5*/, _caretInfo.LineHeight == 0 ? this.Font.Height : _caretInfo.LineHeight);
#endif
                CreateCaret();
                _caretCreated = true;
            }

            // Display the caret. 
            User32.ShowCaret(this.Handle);

            /*
            // Adjust the caret position, in client coordinates. 
            User32.SetCaretPos(-this.HorizontalScroll.Value + _caretInfo.X,
                -this.VerticalScroll.Value + _caretInfo.Y);
            */
            OnFocusedIndexChanged();
            RefreshCaret();

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
            OnFocusedIndexChanged();
            base.OnLostFocus(e);
        }


        #region 当前焦点所在的字段，显示一种醒目的视觉状态

        int _lastFocusedFieldIndex = -1;

        // 当焦点 field index 变化时，失效需要刷新的标志区域
        void OnFocusedIndexChanged()
        {
            var current_index = _caretInfo.ChildIndex;
            if (_caretCreated == false)
                current_index = -1; // 表示需要消除 focued 标志显示
            else if (current_index != _lastFocusedFieldIndex)
            {
                int x = -this.HorizontalScroll.Value;
                int y = -this.VerticalScroll.Value;

                var values = new List<int>();
                if (current_index >= 0)
                    values.Add(current_index);
                if (_lastFocusedFieldIndex >= 0)
                    values.Add(_lastFocusedFieldIndex);
                var rects = this._record.GetFocusedRect(
            x,
            y,
            values.ToArray());
                foreach (var rect in rects)
                {
                    if (rect != System.Drawing.Rectangle.Empty)
                        this.Invalidate(rect);
                }
                _lastFocusedFieldIndex = current_index;
            }
        }

        #endregion


        bool shiftPressed
        {
            get
            {
                return (Control.ModifierKeys & Keys.Shift) != 0;
            }
        }

        bool controlPressed
        {
            get
            {
                return (Control.ModifierKeys & Keys.Control) != 0;
            }
        }


        // 第一次上下左右键，设置 _blockOffs1
        // 后继的上下左右键，设置 _blockOffs2

        protected override void OnKeyDown(KeyEventArgs e)
        {
            //Debug.WriteLine($"OnKeyDown() e.KeyCode={e.KeyCode}");

            // 如果候选弹窗存在并需要按键处理，优先交给弹窗
            if (_suggestionPopup?.Visible == true && HandlePopupKeyDown(e))
            {
                //Debug.WriteLine($"key {e.KeyCode.ToString()} OnKeyDown");
                e.Handled = true;
                e.SuppressKeyPress = true;
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Left:
                case Keys.Right:
                    {
                        ResetFieldSelect();

                        if (controlPressed)
                        {
                            // 如果是向左，并且 caret 正好在块头部，需要先向左移动一个字符
                            if (
                                HasSelection()
                                && _caret_offs == _selectOffs1 || _caret_offs == _selectOffs2)
                            {
                                var ret = _record.MoveByOffs(_caret_offs + (e.KeyCode == Keys.Left ? 1 : -1),
    e.KeyCode == Keys.Left ? -2 : 2,
    out HitInfo info);
                                //SetCaretOffs(info.Offs);
                                MoveCaret(info);
                            }

                            SelectCaretSubfield(false);

                            int offs;
                            if (e.KeyCode == Keys.Left)
                            {
                                offs = Math.Min(_selectOffs1, _selectOffs2);  // 到块的头部
                            }
                            else
                            {
                                offs = Math.Max(_selectOffs1, _selectOffs2);
                            }

                            //SetCaretOffs(offs);
                            MoveCaret(HitByCaretOffs(offs, 0));
                            /*
                            var ret = _record.MoveByOffs(_global_offs + (e.KeyCode == Keys.Left ? 1 : -1),
                                e.KeyCode == Keys.Left ? -1 : 1,
                                out info);
                            */
                            break;
                        }

                        {
                            DetectSelectionChange1(_selectOffs1, _selectOffs2);

                            HitInfo info = null;
                            int ret = 0;
                            if (HasSelection() && shiftPressed == false)
                            {
                                int offs;
                                if (e.KeyCode == Keys.Left)
                                    offs = Math.Min(_selectOffs1, _selectOffs2);  // 到块的头部
                                else
                                    offs = Math.Max(_selectOffs1, _selectOffs2);

                                _selectOffs1 = offs;
                                _selectOffs2 = offs;
                                //SetCaretOffs(offs);

                                ret = _record.MoveByOffs(offs + (e.KeyCode == Keys.Left ? 1 : -1),
                                    e.KeyCode == Keys.Left ? -1 : 1,
                                    out info);
                            }
                            else
                            {
                                // 向右移动，且在头标区内，需要特殊处理
                                if (shiftPressed == false && e.KeyCode == Keys.Right
                                    && _caretInfo.ChildIndex == 0)
                                {
                                    // 为了避免向右移动后 caret 处在令人诧异的等同位置，向右移动也需要模仿向左的 -1 特征
                                    // 注: 诧异位置比如头标区的右侧末尾，001 字段的字段名末尾，等等
                                    ret = _record.MoveByOffs(_caret_offs + 2,
                                        -1,
                                        out info);
                                }
                                else
                                    ret = _record.MoveByOffs(_caret_offs,
                                        e.KeyCode == Keys.Left ? -1 : 1,
                                        out info);
                            }

                            if (ret != 0)
                                break;

                            //SetCaretOffs(info.Offs);
                            MoveCaret(info);

                            if (shiftPressed)
                                _selectOffs2 = _caret_offs;
                            else
                            {
                                _selectOffs1 = _caret_offs;
                                _selectOffs2 = _caret_offs;
                            }
                            _lastX = _caretInfo.X; // 记录最后一次左右移动的 x 坐标

                            // this.Invalidate();  // TODO: 优化为失效具体的行。失效范围可以根据 offs1 -- offs2 整数可以设法直接提供给 Paint() 函数，用以替代 Rectangle
                            InvalidateSelectionRegion();
                        }
                    }
                    break;
                case Keys.Up:
                    // 整块选择字段
                    if (controlPressed)
                    {
                        if (InSelectingField() == false)
                        {
                            BeginFieldSelect(_caretInfo.ChildIndex);
                        }
                        else
                        {
                            if (_caretInfo.ChildIndex == 0)
                                break;  // 不能再上移了
                            // 向上移动一个字段
                            this._record.GetContiguousFieldOffsRange(
                                _caretInfo.ChildIndex - 1,
                                1,
                                out int start,
                                out _);
                            //SetCaretOffs(start);
                            MoveCaret(HitByCaretOffs(start, 0));
                            AdjustFieldSelect(_caretInfo.ChildIndex);
                        }

                        break;
                    }
                    else
                    {
                        ResetFieldSelect();
                    }

                    // 向上移动一个行
                    {
                        var ret = _record.CaretMoveUp(_lastX,
                            _caretInfo.Y,
                            out HitInfo temp_info);
                        if (ret == true)
                        {
                            //SetCaretOffs(temp_info.Offs);
                            MoveCaret(temp_info);

                            ChangeSelection(() =>
                            {
                                if (shiftPressed)
                                    _selectOffs2 = _caret_offs;
                                else
                                {
                                    _selectOffs1 = _caret_offs;
                                    _selectOffs2 = _caret_offs;
                                }
                            });
                            /*
                            DetectBlockChange1(_blockOffs1, _blockOffs2);
                            if (_shiftPressed)
                                _blockOffs2 = _global_offs;
                            else
                            {
                                _blockOffs1 = _global_offs;
                                _blockOffs2 = _global_offs;
                            }
                            // this.Invalidate();
                            InvalidateBlockRegion();
                            */
                        }
                        else
                        {
                            if (shiftPressed == false && HasSelection())
                            {
                                Select(_caret_offs, _caret_offs, _caret_offs);
                            }
                        }
                    }
                    break;
                case Keys.Down:
                    // 整块选择字段
                    if (controlPressed)
                    {
                        if (OpenValueListWindow(_caretInfo) == true)
                        {
                            e.Handled = true;
                            break;
                        }

                        if (InSelectingField() == false)
                        {
                            BeginFieldSelect(_caretInfo.ChildIndex);
                        }
                        else
                        {
                            // 向下移动一个字段
                            // 注: 这种方法获得的 end，位于字段结束符右边，本来就是下一个字段的范围了。但头标区比较特殊，没有字段结束符，需要另外判断处理
                            this._record.GetContiguousFieldOffsRange(
                                _caretInfo.ChildIndex,
                                1,
                                out _,
                                out int end);
                            //SetCaretOffs(end);
                            if (end == 24)
                            {
                                MoveCaret(HitByCaretOffs(end + 1, -1));
                            }
                            else
                            {
                                MoveCaret(HitByCaretOffs(end, 0));
                            }

                            AdjustFieldSelect(_caretInfo.ChildIndex);
                        }

                        break;
                    }
                    else
                    {
                        ResetFieldSelect();
                    }

                    // 向下移动一个行
                    {
                        var ret = _record.CaretMoveDown(_lastX,
                            _caretInfo.Y,
                            out HitInfo temp_info);
                        if (ret == true)
                        {
                            //SetCaretOffs(temp_info.Offs);
                            MoveCaret(temp_info);

                            ChangeSelection(() =>
                            {
                                if (shiftPressed)
                                    _selectOffs2 = _caret_offs;
                                else
                                {
                                    _selectOffs1 = _caret_offs;
                                    _selectOffs2 = _caret_offs;
                                }
                            });
                            /*
                            DetectBlockChange1(_blockOffs1, _blockOffs2);
                            if (_shiftPressed)
                                _blockOffs2 = _global_offs;
                            else
                            {
                                _blockOffs1 = _global_offs;
                                _blockOffs2 = _global_offs;
                            }
                            // this.Invalidate();
                            InvalidateBlockRegion();
                            */
                        }
                        else
                        {
                            if (shiftPressed == false && HasSelection())
                            {
                                Select(_caret_offs, _caret_offs, _caret_offs);
                            }
                        }
                    }
                    break;
                case Keys.Home:
                case Keys.End:
                    {
                        DetectSelectionChange1(_selectOffs1, _selectOffs2);

                        HitInfo info = null;
                        int ret = 0;
                        if (HasSelection() && shiftPressed == false)
                        {
                            int offs;
                            if (e.KeyCode == Keys.Home)
                                offs = Math.Min(_selectOffs1, _selectOffs2);  // 到块的头部
                            else
                                offs = Math.Max(_selectOffs1, _selectOffs2);

                            _selectOffs1 = offs;
                            _selectOffs2 = offs;
                            //SetCaretOffs(offs);

                            ret = _record.MoveByOffs(offs + (e.KeyCode == Keys.Home ? 1 : -1),
                                e.KeyCode == Keys.Home ? -1 : 1,
                                out info);
                        }
                        else
                        {
                            if (controlPressed)
                            {
                                ret = _record.MoveByOffs(e.KeyCode == Keys.Home ? 0 : this._record.TextLength,
    0,
    out info);
                            }
                            else
                            {
                                var field_index = _caretInfo.ChildIndex;
                                int start = -1;
                                if (e.KeyCode == Keys.Home)
                                {
                                    if (this._record.GetFieldOffsRange(field_index, out int field_start, out int field_end) == true)
                                    {
                                        start = field_start;
                                        bool is_control_field = this._record.GetField(field_index)?.IsControlField ?? false;
                                        // 如果已经在字段名第一字符
                                        if (_caret_offs == field_start && field_index != 0)
                                        {
                                            if (is_control_field)
                                                start = Math.Min(field_end, field_start + 3);
                                            else
                                                start = Math.Min(field_end, field_start + 5);
                                        }
                                        else if (
                                            (_caret_offs <= field_start + 3 && is_control_field)
                                            || (_caret_offs <= field_start + 5 && is_control_field == false)
                                            )
                                        {
                                            // 如果在字段名或指示符区域内，则要到字段第一字符
                                            start = field_start;
                                        }
                                        else
                                        {
                                            start = -1;
                                        }
                                    }

                                }

                                if (start != -1)
                                {
                                    ret = _record.MoveByOffs(start, 0, out info);
                                }
                                else
                                {
                                    int x0 = this.HorizontalScroll.Value;
                                    info = _record.HitTest(
                                        e.KeyCode == Keys.Home ?
                                        x0 + _marcMetrics.ContentX
                                        : this.ClientSize.Width,
                                        _caretInfo.Y);
                                }
#if REMOVED
                                var field_index = _caretInfo.ChildIndex;
                                if (this._record.GetFieldOffsRange(field_index, out int field_start, out int field_end) == true)
                                {
                                    int start = field_start;
                                    // 如果已经在字段名第一字符
                                    if (e.KeyCode == Keys.Home
                                        && _caret_offs == field_start)
                                    {
                                        if (this._record.GetField(field_index)?.IsControlField ?? false)
                                            start = Math.Min(field_end, field_start + 3);
                                        else
                                            start = Math.Min(field_end, field_start + 5);
                                    }
                                    ret = _record.MoveByOffs(e.KeyCode == Keys.Home ? start : Math.Max(field_end - 1, field_start),
                                        0,
                                        out info);
                                }
                                else
                                {
                                    ret = -1;
                                }
#endif
                            }
                        }

                        if (ret != 0)
                            break;

                        //SetCaretOffs(info.Offs);
                        MoveCaret(info);

                        if (shiftPressed)
                            _selectOffs2 = _caret_offs;
                        else
                        {
                            _selectOffs1 = _caret_offs;
                            _selectOffs2 = _caret_offs;
                        }
                        _lastX = _caretInfo.X; // 记录最后一次左右移动的 x 坐标

                        InvalidateSelectionRegion();
                    }
                    break;
                case Keys.PageUp:
                case Keys.PageDown:
                    {
                        var old_value = VerticalScroll.Value;
                        var value = VerticalScroll.Value;
                        if (e.KeyCode == Keys.PageUp)
                        {
                            value -= this.ClientSize.Height;
                            value = Math.Max(VerticalScroll.Minimum, value);
                        }
                        else
                        {
                            value += this.ClientSize.Height;
                            value = Math.Min(VerticalScroll.Maximum, value);
                        }

                        var delta = value - old_value;

                        if (delta != 0)
                        {
                            this.AutoScrollPosition = new Point(
    this.HorizontalScroll.Value,
    value);

                            var caret_y = _caretInfo.Y + delta;
                            var caret_x = Math.Max(_marcMetrics.ContentX + 1, _lastX);

                            var hit_info = this._record.HitTest(caret_x, caret_y);
                            //SetCaretOffs(hit_info.Offs);
                            MoveCaret(hit_info);

                            ChangeSelection(() =>
                            {
                                if (shiftPressed)
                                    _selectOffs2 = _caret_offs;
                                else
                                {
                                    _selectOffs1 = _caret_offs;
                                    _selectOffs2 = _caret_offs;
                                }
                            });
                            /*
                            DetectBlockChange1(_blockOffs1, _blockOffs2);

                            if (_shiftPressed)
                                _blockOffs2 = _global_offs;
                            else
                            {
                                _blockOffs1 = _global_offs;
                                _blockOffs2 = _global_offs;
                            }
                            // this.Invalidate();
                            InvalidateBlockRegion();
                            */
                        }

                    }
                    break;
                case Keys.Delete:
                    if (ProcessDeleteKey(_caretInfo, _deleteKeyStyle) == true)
                    {
                        e.Handled = true;
                    }

                    break;
                case Keys.Back:
                    if (ProcessBackspaceKey(_caretInfo) == true)
                    {
                        e.Handled = true;
                    }

                    break;

                case Keys.Tab:
                    {
                        var offs = GetNextSubfieldOffs(shiftPressed, out int delta);
                        if (offs != -1)
                        {
                            SetCaret(HitByCaretOffs(offs - delta, delta));
                        }
                    }
                    break;
            }
            base.OnKeyDown(e);
        }

        protected override void OnKeyUp(KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.ShiftKey:
                    EndFieldSelect();
                    break;
                case Keys.ControlKey:
                    EndFieldSelect();
                    break;
            }
            base.OnKeyUp(e);
        }

        public const char KERNEL_SUBFLD = (char)31; // '▼';	// '‡';  子字段指示符内部代用符号

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            //Debug.WriteLine($"OnKeyPress() e.KeyChar={e.KeyChar}");
            // 弹窗可能需要处理 Escape 等字符，优先处理
            if (_suggestionPopup?.Visible == true && HandlePopupKeyPress(e))
            {
                //Debug.WriteLine($"key {e.KeyChar.ToString()} OnKeyPress");
                e.Handled = true;
                return;
            }

            switch (e.KeyChar)
            {
                case (char)Keys.Escape:
                    break;
                case '\b':
                    break;
                default:
                    {
                        if (this._readonly)
                            return;

                        var ret = ProcessInputChar(e.KeyChar, _caretInfo);
                        if (ret)
                            e.Handled = true;
                    }
                    break;
            }

            base.OnKeyPress(e);
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

        // 打开或者关闭输入法
        public void OpenIME(bool open)
        {
            if (hIMC == IntPtr.Zero)
                return;

            // 打开或关闭 IME（保持简单，具体 conversion flags 可按需设置）
            ImmSetOpenStatus(hIMC, open);
            // 如需设置转换/模式，可调用 ImmSetConversionStatus(hIMC, conversion, sentence);
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
            _marcMetrics.Refresh(this.Font, this.FixedSizeFont);
            SetCompositionWindowPos();
            base.OnLoad(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            DisposeSelectionRegion();

            Imm32.ImmReleaseContext(this.Handle, hIMC);
            // User32.DestroyCaret();

            #region 延迟刷新
            DestroyTimer();
            #endregion

            DestroyMouseTimer();

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

                int x = -this.HorizontalScroll.Value;
                int y = -this.VerticalScroll.Value;

                cf.ptCurrentPos.x = x + _caretInfo.X;
                cf.ptCurrentPos.y = y + _caretInfo.Y;
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

        public DomRecord GetDomRecord()
        {
            return new DomRecord(this._record);
        }

        public MarcRecord MarcRecord
        {
            get
            {
                return _record;
            }
        }

        int _disableUpdate = 0;
        int _invalidateCount = 0;

        public void BeginUpdate()
        {
            if (_disableUpdate == 0)
                _invalidateCount = 0;
            _disableUpdate++;
            _disableResize++;
        }

        new void Invalidate(Rectangle rect)
        {
            if (_disableUpdate == 0)
                base.Invalidate(rect);
            else
                _invalidateCount++;
        }

        new void Invalidate(Region region)
        {
            if (_disableUpdate == 0)
                base.Invalidate(region);
            else
                _invalidateCount++;
        }

        new void Invalidate()
        {
            if (_disableUpdate == 0)
                base.Invalidate();
            else
                _invalidateCount++;
        }

        // TODO: 禁止期间，搜集 Invalidate 的次数。如果一次也没有，最后也就不要 Invalidate()
        public void EndUpdate(bool invalidate = true)
        {
            _disableResize--;
            _disableUpdate--;
            if (_disableUpdate == 0 && invalidate)
            {
                if (_invalidateCount > 0)
                    base.Invalidate();
                // 注: disable update 期间可能有些 BlockChanged 时间没有触发，需要补触发
                // this.BlockChanged?.Invoke(this, new EventArgs());
            }

            if (_disableUpdate == 0)
                _invalidateCount = 0;
        }

        char _highlightBlankChar = ' ';
        public char HighlightBlankChar
        {
            get
            {
                return _highlightBlankChar;
            }
            set
            {
                if (_highlightBlankChar != value)
                {
                    _highlightBlankChar = value;
                    Relayout(this._record.MergeText());
                }
            }
        }
    }
}
