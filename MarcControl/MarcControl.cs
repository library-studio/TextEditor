using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.AccessControl;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Vanara.PInvoke;
using static LibraryStudio.Forms.MarcRecord;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Imm32;
using static Vanara.PInvoke.Kernel32.FILE_REMOTE_PROTOCOL_INFO;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Usp10;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// MARC 编辑器控件
    /// </summary>
    public partial class MarcControl : UserControl
    {
        public event EventHandler BlockChanged;

        public event EventHandler CaretMoved;

        // public new event EventHandler TextChanged;

        History _history = new History();

        public string DumpHistory()
        {
            return _history.ToString();
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
            _record = new MarcRecord(this, _fieldProperty);

            #region 延迟刷新
            CreateTimer();
            #endregion

            InitializeComponent();

            // _line_height = Line.InitialFonts(this.Font);

            _fieldProperty.Refresh(this.Font);

            _dpiXY = DpiUtil.GetDpiXY(this);

            _context = GetDefaultContext();
        }

        public IContext GetDefaultContext()
        {
            return new Context
            {
                SplitRange = (o, content) =>
                {
                    // return SimpleText.SegmentSubfields(content, '\x001f', true); // 只能让子字段符号一个字符显示为特殊颜色
                    // return SimpleText.SegmentSubfields2(content, '\x001f', 2, true);
                    return MarcField.SegmentSubfields(content, '\x001f', 2);
                },
                ConvertText = (text) =>
                {
                    // ꞏ▼▒■☻ⱡ⸗ꜜꜤꞁꞏסּ⁞

                    // TODO: 可以考虑把英文空格和中文空格显示为易于辨识的特殊字符
                    return text.Replace("\x001f", "▼").Replace("\x001e", "ꜜ");
                },
                GetForeColor = (o, highlight) =>
                {
                    if (highlight)
                        return _fieldProperty?.HightlightForeColor ?? SystemColors.HighlightText;
                    var range = o as Range;
                    if (range != null
                        && range.Tag is MarcField.Tag tag
                        && tag.Delimeter)
                        return _fieldProperty?.DelimeterForeColor ?? Metrics.DefaultDelimeterForeColor; // 子字段名文本为红色
                    if (this._readonly)
                        return _fieldProperty?.ReadOnlyForeColor ?? SystemColors.ControlText;
                    return _fieldProperty?.ForeColor ?? SystemColors.WindowText;
                },
                GetBackColor = (o, highlight) =>
                {
                    if (highlight)
                        return _fieldProperty?.HighlightBackColor ?? SystemColors.Highlight;
                    var range = o as Range;
                    if (range != null
                        && range.Tag is MarcField.Tag tag
                        && tag.Delimeter)
                        return _fieldProperty?.DelimeterBackColor ?? Metrics.DefaultDelimeterBackColor; // 子字段名文本为红色
                    if (range != null)
                        return Color.Transparent;
                    if (this._readonly)
                    {
                        return _fieldProperty?.ReadOnlyBackColor ?? SystemColors.Control;
                        // backColor = ControlPaint.Dark(backColor, 0.01F);
                    }
                    var backColor = _fieldProperty?.BackColor ?? SystemColors.Window;
                    return backColor;
                },
                PaintBack = (o, hdc, rect, clipRect) =>
                {
                    var field = o as MarcField;
                    if (o == null)
                        return;
                    rect.Width = this.AutoScrollMinSize.Width;
                    using (var g = (Graphics.FromHdc((IntPtr)(HDC)hdc)))
                    using (var brush = new SolidBrush(_fieldProperty.SolidColor))
                    {
                        /*
                        var line_height = field.IsHeader ? 0 : Line.GetLineHeight();
                        var solid_height = rect.Height - line_height;
                        if (solid_height > 0)
                        {
                            var solid_rect = new Rectangle(
                            rect.X,
                            rect.Y + line_height,
                            _fieldProperty.NameBorderX + _fieldProperty.NamePixelWidth + _fieldProperty.IndicatorPixelWidth,
                            solid_height);
                            if (clipRect.IntersectsWith(solid_rect))
                                g.FillRectangle(brush, solid_rect);
                        }
                        */

                        field.PaintBackAndBorder(g, rect.X, rect.Y);

                        /*
                        if (field.IsHeader == false)
                        {
                            PaintBackAndBorder(g,
                                rect.X + _fieldProperty.NameBorderX,
                                rect.Y,
                                _fieldProperty.NamePixelWidth,
                                Line.GetLineHeight());
                            if (field.IsControlField == false)
                                PaintBackAndBorder(g,
            rect.X + _fieldProperty.IndicatorBorderX,
            rect.Y,
            _fieldProperty.IndicatorPixelWidth,
            Line.GetLineHeight());
                        }
                        */
                    }
                    /*
                        MarcField.PaintBack(hdc,
                            rect,
                            clipRect,
                            Color.Yellow);
                    */
                },
                GetFont = (o, t) =>
                {
                    if (t is MarcField.Tag tag
                        && tag.Delimeter)
                        return FixedFontGroup;

                    if (o is IBox)
                    {
                        var line = o as IBox;
                        if (line.Name == "name" || line.Name == "indicator")
                            return FixedFontGroup;
                        else if (line.Name == "caption")
                            return CaptionFontGroup;
                        else if (line.Name == "content")
                        {
                            var field = line.Parent as MarcField;
                            if (field.IsHeader)
                                return FixedFontGroup;
                            else
                                return ContentFontGroup;
                        }
                    }
                    return ContentFontGroup;
                }
            };
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
            // 绘制背景色
            {

                var backColor = this._context?.GetBackColor?.Invoke(null, false) ?? SystemColors.Window;
                using (var brush = new SolidBrush(backColor))
                {
                    e.Graphics.FillRectangle(brush, clipRect);
                }

                // 绘制提示文字区的底色
                {
                    var left_rect = new Rectangle(
                            x,
                            y,
                            _fieldProperty.CaptionPixelWidth,
                            this.AutoScrollMinSize.Height);
                    if (clipRect.IntersectsWith(left_rect))
                    {
                        using (var brush = new SolidBrush(_fieldProperty?.CaptionBackColor ?? Metrics.DefaultCaptionBackColor))
                        {
                            e.Graphics.FillRectangle(brush, left_rect);
                        }
                    }
                }

                /*
                // Solid 区左侧 border + sep 竖线
                {
                    var left_rect = new Rectangle(
        x + _fieldProperty.SolidX,
        y,
        _fieldProperty.BorderThickness + _fieldProperty.GapThickness,
        this.AutoScrollMinSize.Height);
                    if (clipRect.IntersectsWith(left_rect))
                    {
                        using (var brush = new SolidBrush(_fieldProperty?.SolidColor ?? Metrics.DefaultSolidColor))
                        {
                            e.Graphics.FillRectangle(brush, left_rect);
                        }
                    }

                }
                */

                // 右侧全高的一根立体竖线
                {
                    MarcField.PaintLeftRightBorder(e.Graphics,
        x + _fieldProperty.SolidX,
        y + 0,
        _fieldProperty.SolidPixelWidth,
        this.AutoScrollMinSize.Height,
        _fieldProperty.BorderThickness);
                }
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

                _record.Paint(
                    _context,
                    dc,
                    x,
                    y,
                    clipRect,
                    _blockOffs1,
                    _blockOffs2,
                    0);
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


#if CONTENT_STRING
        private string _content = string.Empty;
#else
        private int _content_length = 0;
#endif

        public virtual string Content
        {
            get
            {
#if CONTENT_STRING
                return _content;
#else
                return _record.MergeText();
#endif
            }
            set
            {
#if CONTENT_STRING
                _content = value;
#endif

                // _initialized = false;
                Relayout(value);

                // _caretInfo = new HitInfo();
                MoveCaret(HitByGlobalOffs(_global_offs + 1, -1));
                this.Invalidate();
                this._history.Clear();
            }
        }

        //SCRIPT_PROPERTIES[] sp = null;
        //SCRIPT_DIGITSUBSTITUTE sub;
        //SCRIPT_CONTROL sc;
        //SCRIPT_STATE ss;

        //SafeSCRIPT_CACHE _cache = null;

        // int _line_height = 20;

        HitInfo _caretInfo = new HitInfo();

        public HitInfo CaretInfo
        {
            get
            {
                return _caretInfo?.Clone() ?? new HitInfo();
            }
        }

        // 当前插入符所在的字段 index。如果为 -1 表示不在任何字段上(可能是当前 MARC 内容为空)
        public int CaretFieldIndex
        {
            get
            {
                return _caretInfo?.ChildIndex ?? -1;
            }
        }

#if REMOVED
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

            _cache = new SafeSCRIPT_CACHE();

        }
#endif

        Metrics _fieldProperty = new Metrics();

        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public GetFieldCaptionFunc GetFieldCaption
        {
            get { return _fieldProperty.GetFieldCaption; }
            set
            {
                Rectangle update_rect = System.Drawing.Rectangle.Empty;
                _fieldProperty.GetFieldCaption = value;
                using (var g = this.CreateGraphics())
                {
                    var handle = g.GetHdc();
                    using (var dc = new SafeHDC(handle))
                    {
                        _record.UpdateAllCaption(
                            _context,
                            dc,
                            out update_rect);
                    }
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

        // List<Line> _lines = new List<Line>();
        MarcRecord _record = null;  // new MarcRecord(_fieldProperty);

        IContext _context = null;


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
                if (_global_offs > text.Length)
                {
                    // 注意这里没有 MoveCaret();
                    SetGlobalOffs(text.Length);
                }
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
            var max_pixel_width = _record.Initialize(dc,
                text,
                Math.Max(this.ClientSize.Width, 30),
                (content) =>
                {
                    return SimpleText.SplitSubfields(content);
                });
            */
            var ret = _record.ReplaceText(
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
                    User32.ScrollWindowEx(this.Handle,
                        0,
                        scroll_distance,
                        scroll_rect,
                        null,
                        HRGN.NULL,
                        out _,
                        ScrollWindowFlags.SW_INVALIDATE);
            }
            Utility.Offset(ref update_rect, x, y);
            if (update_rect.IsEmpty == false)
            {
                this.Invalidate(update_rect);
                // ScheduleInvalidate(update_rect);
            }

#if CONTENT_STRING
            _content = _paragraph.MergeText();
#else
            _content_length = _record.TextLength;
#endif

            ChangeDocumentSize(max_pixel_width);

            if (true/* this.Focused */)
            {
                /*
                _caretInfo = HitByGlobalOffs(_global_offs);
                User32.HideCaret(this.Handle);
                User32.SetCaretPos(_caretInfo.X, _caretInfo.Y);
                User32.ShowCaret(this.Handle);
                */
                MoveCaret(HitByGlobalOffs(_global_offs + 1, -1), false);
            }
        }

        void ChangeDocumentSize(int max_pixel_width)
        {
            if (max_pixel_width > 0)
            {
                this.SetAutoSizeMode(AutoSizeMode.GrowAndShrink);
                if (_clientBoundsWidth == -1)
                    max_pixel_width = Math.Max(this.AutoScrollMinSize.Width, max_pixel_width);
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

            _fieldProperty.Refresh(this.Font);

            var text = _record.MergeText();
            this._record.Clear();   // 清除所有 IBox 对象，释放所有对原有字体的引用
            // 迫使重新计算行高，重新布局 Layout
            Relayout(text);

            // 重新创建一次 Caret，改变 caret 高度
            RecreateCaret();
        }

        void RecreateCaret()
        {
            // 重新创建一次 Caret，改变 caret 高度
            if (_caretCreated)
            {
                User32.DestroyCaret();
                User32.CreateCaret(this.Handle, new HBITMAP(IntPtr.Zero), 2/*_line_height / 5*/, _caretInfo.LineHeight == 0 ? this.Font.Height : _caretInfo.LineHeight);
                if (this.Focused)
                    User32.ShowCaret();
            }
        }

        int _lastWidth = 0;

        protected override void OnResize(EventArgs e)
        {
            if (_clientBoundsWidth == 0)
            {
                var current_width = this.ClientSize.Width;
                if (current_width != _lastWidth)    // 减少大量无谓的调用
                {
                    Relayout();
                    _lastWidth = this.ClientSize.Width;
                }
            }

            base.OnResize(e);
        }

        public void Relayout()
        {
            // 迫使重新布局 Layout
            Relayout(_record.MergeText());

            _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标

            this.Invalidate();
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

        // parameters:
        //      delay_update    希望延迟更新。方法是先更新一个小的区域，延迟更新原本大的区域
        public ReplaceTextResult ReplaceText(int start,
            int end,
            string text,
            bool delay_update,
            bool auto_adjust_global_offs = true,
            bool add_history = true)
        {
            ReplaceTextResult ret = null;
            using (var g = this.CreateGraphics())
            {
                var handle = g.GetHdc();
                using (var dc = new SafeHDC(handle))
                {
                    ret = _record.ReplaceText(
                        _context,
                        dc,
                        start,
                        end,
                        text,
                        _clientBoundsWidth == 0 ? this.ClientSize.Width : _clientBoundsWidth   // Math.Max(this.ClientSize.Width, 30),
                        );

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
                    User32.ScrollWindowEx(this.Handle,
                        0,
                        scroll_distance,
                        scroll_rect,
                        null,
                        HRGN.NULL,
                        out _,
                        ScrollWindowFlags.SW_INVALIDATE);
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

#if CONTENT_STRING
                        // TODO: 这里值得改进加速速度。可以考虑仅仅保留一个 content length 即可
                        _content = _paragraph.MergeText();
#else
            _content_length = _record.TextLength;
#endif

            /*
            // TODO: SetAutoSizeMode() 放到一个统一的初始化代码位置即可
            this.SetAutoSizeMode(AutoSizeMode.GrowAndShrink);
            this.AutoScrollMinSize = new Size(max_pixel_width, _record.GetPixelHeight());
            */
            ChangeDocumentSize(max_pixel_width);

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
                MoveCaret(HitByGlobalOffs(_global_offs + 1, -1), false);
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
        int _blockOffs1 = 0;    // -1;   // 选中范围开始的偏移量
        int _blockOffs2 = 0;    // -1;   // 选中范围的结束的偏移量

        // 文字块的起始偏移量
        public int BlockStartOffset
        {
            get { return _blockOffs1; }
            //get { return Math.Min(_blockOffs1, _blockOffs2); }
        }

        // 文字块的结束偏移量
        public int BlockEndOffset
        {
            get { return _blockOffs2; }
            //get { return Math.Max(_blockOffs1, _blockOffs2); }
        }

        private int _global_offs = 0; // Caret 全局偏移量。

        // 插入符全局偏移量
        public int CaretOffset
        {
            get { return _global_offs; }
        }

        void SetGlobalOffs(int offs)
        {
            if (_global_offs != offs)
            {
                _global_offs = offs;
                //if (trigger_event)
                //    this.CaretMoved?.Invoke(this, new EventArgs());
            }
        }

        void AdjustGlobalOffs(int delta)
        {
            if (delta != 0)
            {
                _global_offs += delta;
                // this.CaretMoved?.Invoke(this, new EventArgs());
            }
        }

        // 检测分割条和 Caption 区域
        // return:
        //      -2  Caption 区域
        //      -1  Splitter 区域
        //      0   其它区域(包括 name indicator 和 content 区域)
        int TestSplitterArea(int x)
        {
            x += this.HorizontalScroll.Value;   // 窗口坐标变换为内容坐标
            return _fieldProperty.TestSplitterArea(x);
        }

        #region 选择多个字段

        // 开始选择完整字段的开始 index (字段下标)
        // TODO: start 要永久记忆，MouseUp 之后也要记忆。另外用一个 bool 变量表示是否正在拖动之中
        bool _selecting_field = false;   // 是否正在选择字段过程中? 
        int _select_field_start = -1;
        int _select_field_end = -1;

        bool InSelectingField()
        {
            return _selecting_field;
        }

        void BeginFieldSelect(int index)
        {
            // 如果是按住 Ctrl 键进入本函数，则要汇总当前已有的 offs range 对应的 field index start end range，以便开始在此基础上修改选择
            if (_shiftPressed || _controlPressed)
            {
                // Ctrl 键被按下的时候，观察 _select_field_start 是否有以前
                // 残留的值，如果有则说明刚点选过(完整)字段，可以直接利用

                if (_select_field_start == -1)
                    _select_field_start = index;
                _select_field_end = index;  // 尾部则用最新 index 充当
                // Debug.WriteLine($"start={_select_field_start} end={_select_field_end}");
            }
            else
            {
                _select_field_start = index;
                _select_field_end = index;
            }

            _selecting_field = true;
            UpdateFieldSelection();
        }

        void AdjustFieldSelect(int index)
        {
            if (_selecting_field == false)
                return;

            if (_select_field_end == index)
                return; // 没有变化

            if (index < 0 || index > this._record.FieldCount)
                return;

            _select_field_end = index;

            UpdateFieldSelection();
        }

        // 更新 field offs range 和显示
        void UpdateFieldSelection()
        {
            int start_index = Math.Min(_select_field_start, _select_field_end);
            int end_index = Math.Max(_select_field_start, _select_field_end);
            int count = end_index - start_index + 1;

            if (start_index + count > _record.FieldCount)
                count = _record.FieldCount - start_index;

            if (count == 0)
                return;

            var ret = _record.GetContiguousFieldOffsRange(start_index,
                count,
                out int start_offs,
                out int end_offs);
            if (ret == true)
            {
                DetectBlockChange1(_blockOffs1, _blockOffs2);
                _blockOffs1 = start_offs;
                _blockOffs2 = end_offs;
                InvalidateBlockRegion();
            }
        }

        void EndFieldSelect()
        {
            if (_shiftPressed || _controlPressed)
            {
                // 中途不改变 _selecting_field
            }
            else
            {
                _selecting_field = false;
            }
        }

        #endregion

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                {
                    ContextMenuStrip contextMenu = new ContextMenuStrip();
                    var commands = this.Commands;
                    AppendMenu(contextMenu.Items, commands);
                    contextMenu.Show(this, e.Location);
                }
                base.OnMouseDown(e);
                return;
            }
            this.Capture = true;
            this._isMouseDown = true;
            // ChangeCaretPos(e.X, e.Y);
            {
                var result = _record.HitTest(
                    e.X + this.HorizontalScroll.Value,
                    e.Y + this.VerticalScroll.Value);
                /*
                if ((result.Area & Area.LeftBlank) != 0)
                {

                }
                */
                // 点击 Caption 区域
                var splitter_test = TestSplitterArea(e.X);
                if (splitter_test == -2)
                {
                    BeginFieldSelect(result.ChildIndex);
                    base.OnMouseDown(e);
                    return;
                }
                // 点击 Splitter
                if (splitter_test == -1)
                {
                    StartSplitting(e.X);
                    base.OnMouseDown(e);
                    return;
                }

#if DEBUG
                // Debug.Assert(result.Offs == _record.GetGlobalOffs(result));
#endif
                SetGlobalOffs(result.Offs);

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
                InvalidateBlockRegion();

                MoveCaret(result);
            }
            base.OnMouseDown(e);
        }

        // 分割条拖动过程中的 x 位置
        int _splitterX = 0;
        int _splitterStartX = 0;
        bool _splitting = false;

        void StartSplitting(int x)
        {
            _splitterX = x;
            _splitterStartX = x;

            _splitting = true;

            DrawTraker();
        }

        void MoveSplitting(int x)
        {
            Cursor = Cursors.SizeWE;

            // 消上次残余的一根
            DrawTraker();

            _splitterX = x;

            // 绘制本次的一根
            DrawTraker();
        }

        bool FinishSplitting(int x)
        {
            // 消最后残余的一根
            DrawTraker();

            // 计算差额
            var delta = _splitterX - _splitterStartX;

            var changed = _fieldProperty.DeltaCaptionWidth(delta);
            /*
            _fieldProperty.CaptionPixelWidth += delta;
            _fieldProperty.CaptionPixelWidth = Math.Max(_fieldProperty.SplitterPixelWidth, _fieldProperty.CaptionPixelWidth);
            */

            _splitting = false;
            _splitterStartX = 0;
            _splitterX = 0;
            if (changed)
            {
                Relayout();
                return true;
            }
            return false;
        }

        void DrawTraker()
        {
            // Debug.WriteLine($"_splitterX={_splitterX}");

            Point p1 = new Point(_splitterX, 0);
            p1 = this.PointToScreen(p1);

            Point p2 = new Point(_splitterX, this.ClientSize.Height);
            p2 = this.PointToScreen(p2);

            /*
            // 获取当前屏幕的 DPI 缩放因子
            // float dpiScale = this.DeviceDpi / 96f;
            // 逻辑坐标转为物理像素
            p1 = this.PointToScreen(DpiUtil.Get96ScalingPoint(_dpiXY, p1));
            p2 = this.PointToScreen(DpiUtil.Get96ScalingPoint(_dpiXY, p2));
            */

            // 注意，必须用当前屏幕的实际 DPI 来绘制
            ControlPaint.DrawReversibleLine(p1,
                p2,
                SystemColors.Control);
        }

        // parameters:
        //      conditional_trigger   是否有条件地触发事件
        //                          所谓条件就是，和前一次的 global_offs 要有不同才会触发事件。常用于 OnMouseUp() 时，因为 OnMouseDown() 已经触发一次事件了
        void MoveCaret(HitInfo result,
            bool ensure_caret_visible = true,
            bool conditional_trigger = false)
        {
            var old_offs = _caretInfo?.Offs ?? 0;
            /*
            if (result.LineHeight == 0)
                return;
            Debug.Assert(result.LineHeight != 0);
            */
            var old_caret_height = _caretInfo?.LineHeight ?? 0;
            _caretInfo = result;

            if (ensure_caret_visible)
                EnsureCaretVisible();

            if (old_caret_height != _caretInfo?.LineHeight)
            {
                RecreateCaret();
            }

            if (_caretCreated)
            {
                User32.HideCaret(this.Handle);
                User32.SetCaretPos(-this.HorizontalScroll.Value + _caretInfo.X,
                    -this.VerticalScroll.Value + _caretInfo.Y);
                User32.ShowCaret(this.Handle);
            }

            SetCompositionWindowPos();

            if (conditional_trigger && old_offs == (_caretInfo?.Offs ?? 0))
            {

            }
            else
                this.CaretMoved?.Invoke(this, EventArgs.Empty);
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            // 有可能没有经过 OnMouseDown()，一上来就是 OnMouseUp()，这多半是别的窗口操作残留的消息
            if (this._isMouseDown == false)
            {
                base.OnMouseUp(e);
                return;
            }

            this.Capture = false;
            this._isMouseDown = false;

            if (_splitting)
            {
                FinishSplitting(e.X);
                base.OnMouseUp(e);
                return;
            }

            if (InSelectingField())
            {
                EndFieldSelect();
                base.OnMouseUp(e);
                // 立即返回，避免所选的块被破坏
                return;
            }

            // 选中范围结束
            {
                var result = _record.HitTest(
                    e.X + this.HorizontalScroll.Value,
                    e.Y + this.VerticalScroll.Value);

                DetectBlockChange1(_blockOffs1, _blockOffs2);
                var old_offs = _global_offs;

                if (old_offs != result.Offs)
                    SetGlobalOffs(result.Offs); // _record.GetGlobalOffs(result);

                {
                    _blockOffs2 = _global_offs;

                    var changed = DetectBlockChange2(_blockOffs1, _blockOffs2);

                    MoveCaret(result, true, true/*有条件地触发事件*/);

                    _lastX = _caretInfo.X; // 记录最后一次点击鼠标 x 坐标

                    if (changed)
                    {
                        //this.BlockChanged?.Invoke(this, new EventArgs());

                        // TODO: 可以改进为只失效影响到的 Line
                        // this.Invalidate(); // 重绘
                        InvalidateBlockRegion();
                    }
                }
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            if (_isMouseDown && _splitting)
            {
                MoveSplitting(e.X);
                base.OnMouseMove(e);
                return;
            }

            var result = _record.HitTest(
    e.X + this.HorizontalScroll.Value,
    e.Y + this.VerticalScroll.Value);

            if (_isMouseDown)
            {
                // 鼠标选择字段时拖动进入这里。而如果是按住 Ctrl 或 Shift 点选，则不应进入这里
                if (InSelectingField())
                {
                    if ((ModifierKeys & (Keys.Control | Keys.Shift)) == 0)
                    {
                        AdjustFieldSelect(result.ChildIndex);
                    }
                    base.OnMouseMove(e);
                    return;
                }

                SetGlobalOffs(result.Offs);

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
                    SetGlobalOffs(result.Offs);
                    MoveCaret(result);

                    //this.BlockChanged?.Invoke(this, new EventArgs());

                    // TODO: 可以改进为只失效影响到的 Line
                    // this.Invalidate(); // 重绘
                    InvalidateBlockRegion();
                }
            }
            else
            {
                var splitter_test = TestSplitterArea(e.X);

                if (splitter_test == -1)
                    Cursor = Cursors.SizeWE;
                else if (splitter_test >= 0)
                    Cursor = Cursors.IBeam;
                else
                    Cursor = Cursors.Arrow;
            }

            base.OnMouseMove(e);
        }

        bool _shiftPressed = false; // Shift 键是否按下
        bool _controlPressed = false;

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
                            SetGlobalOffs(offs);

                            ret = _record.MoveByOffs(_global_offs + (e.KeyCode == Keys.Left ? 1 : -1),
                                e.KeyCode == Keys.Left ? -1 : 1,
                                out info);
                        }
                        else
                        {
                            // 向右移动，且在头标区内，需要特殊处理
                            if (_shiftPressed == false && e.KeyCode == Keys.Right
                                && _caretInfo.ChildIndex == 0)
                            {
                                // 为了避免向右移动后 caret 处在令人诧异的等同位置，向右移动也需要模仿向左的 -1 特征
                                // 注: 诧异位置比如头标区的右侧末尾，001 字段的字段名末尾，等等
                                ret = _record.MoveByOffs(_global_offs + 2,
                                    -1,
                                    out info);
                            }
                            else
                                ret = _record.MoveByOffs(_global_offs,
                                    e.KeyCode == Keys.Left ? -1 : 1,
                                    out info);
                        }

                        if (ret != 0)
                            break;

                        SetGlobalOffs(info.Offs);
                        MoveCaret(info);

                        if (_shiftPressed)
                            _blockOffs2 = _global_offs;
                        else
                        {
                            _blockOffs1 = _global_offs;
                            _blockOffs2 = _global_offs;
                        }
                        _lastX = _caretInfo.X; // 记录最后一次左右移动的 x 坐标

                        // this.Invalidate();  // TODO: 优化为失效具体的行。失效范围可以根据 offs1 -- offs2 整数可以设法直接提供给 Paint() 函数，用以替代 Rectangle
                        InvalidateBlockRegion();
                    }
                    break;
                case Keys.Up:
                    // 整块选择字段
                    if (_controlPressed)
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
                            SetGlobalOffs(start);
                            MoveCaret(HitByGlobalOffs(start, 0));
                            AdjustFieldSelect(_caretInfo.ChildIndex);
                        }

                        break;
                    }

                    // 向上移动一个行
                    {
                        var ret = _record.CaretMoveUp(_lastX,
                            _caretInfo.Y,
                            out HitInfo temp_info);
                        if (ret == true)
                        {
                            DetectBlockChange1(_blockOffs1, _blockOffs2);

                            SetGlobalOffs(temp_info.Offs);
                            MoveCaret(temp_info);
                            if (_shiftPressed)
                                _blockOffs2 = _global_offs;
                            else
                            {
                                _blockOffs1 = _global_offs;
                                _blockOffs2 = _global_offs;
                            }
                            // this.Invalidate();
                            InvalidateBlockRegion();
                        }
                    }
                    break;
                case Keys.Down:
                    // 整块选择字段
                    if (_controlPressed)
                    {
                        if (InSelectingField() == false)
                        {
                            BeginFieldSelect(_caretInfo.ChildIndex);
                        }
                        else
                        {
                            // 向下移动一个字段
                            this._record.GetContiguousFieldOffsRange(
                                _caretInfo.ChildIndex,
                                1,
                                out _,
                                out int end);
                            SetGlobalOffs(end);
                            MoveCaret(HitByGlobalOffs(end, 0));
                            AdjustFieldSelect(_caretInfo.ChildIndex);
                        }

                        break;
                    }

                    // 向下移动一个行
                    {
                        var ret = _record.CaretMoveDown(_lastX,
                            _caretInfo.Y,
                            out HitInfo temp_info);
                        if (ret == true)
                        {
                            DetectBlockChange1(_blockOffs1, _blockOffs2);

                            SetGlobalOffs(temp_info.Offs);
                            MoveCaret(temp_info);
                            if (_shiftPressed)
                                _blockOffs2 = _global_offs;
                            else
                            {
                                _blockOffs1 = _global_offs;
                                _blockOffs2 = _global_offs;
                            }
                            // this.Invalidate();
                            InvalidateBlockRegion();
                        }
                    }
                    break;
                case Keys.ShiftKey:
                    _shiftPressed = true;
                    break;
                case Keys.ControlKey:
                    _controlPressed = true;
                    break;
                case Keys.Delete:
                    if (this._readonly)
                        return;
                    if (HasBlock())
                        SoftlyRemoveBolckText();
                    else
                    {
                        var delay = _keySpeedDetector.Detect();
                        // TODO: 可以考虑增加一个功能，Ctrl+Delete 删除一个 char。而不是删除一个不可分割的 cluster(cluster 可能包含若干个 char)
                        if (_global_offs < _content_length)
                        {
                            var replace = this._record.GetReplaceMode(_caretInfo,
                                "delete",
                                PaddingChar,
                                out string fill_content);
                            if (string.IsNullOrEmpty(fill_content) == false)
                                ReplaceText(_global_offs,
                                    _global_offs,
                                    fill_content,
                                    delay_update: delay,
                                    false);

                            // 记忆起点 offs
                            var old_offs = _global_offs;
                            // 验证向右移动插入符
                            var ret = _record.MoveByOffs(_global_offs, 1, out HitInfo info);
                            if (ret != 0)
                                break;

                            if (info.Offs > old_offs)
                            {
                                // 删除一个或者多个字符

                                if (replace)
                                    ReplaceText(old_offs,
                                        info.Offs,
                                        new string(' ', info.Offs - old_offs),
                                        delay_update: delay,
                                        false);
                                else
                                    ReplaceText(old_offs,
                                        info.Offs,
                                        "",
                                        delay_update: delay,
                                        false);
                            }

                            // TODO: 严格来说 Delete 时候插入符也需要重新 MoveCaret()
                        }
                    }
                    e.Handled = true;
                    break;
                case Keys.Back:
                    if (this._readonly)
                        return;
                    if (HasBlock())
                        SoftlyRemoveBolckText();
                    else
                    {
                        var delay = _keySpeedDetector.Detect();
                        if (_global_offs > 0)
                        {
                            // TODO: 注意连续的、不可分割的一个整体的多个字符情况
                            var replace = this._record.GetReplaceMode(_caretInfo,
                                "backspace",
                                PaddingChar,
                                out string fill_content);
                            if (string.IsNullOrEmpty(fill_content) == false)
                                ReplaceText(_global_offs,
                                    _global_offs,
                                    fill_content,
                                    delay_update: delay,
                                    false);

                            // 记忆起点 offs
                            var old_offs = _global_offs;
                            // 移动插入符
                            var ret = _record.MoveByOffs(_global_offs, -1, out HitInfo info);
                            if (ret != 0)
                                break;

                            SetGlobalOffs(info.Offs);

                            // 删除一个或者多个字符
                            if (replace)
                                ReplaceText(_global_offs,
                                    old_offs,
                                    new string(PaddingChar, old_offs - _global_offs),
                                    delay_update: delay,
                                    false);
                            else
                                ReplaceText(_global_offs,
                                    old_offs,
                                    "",
                                    delay_update: delay,
                                    false);

                            //this.Invalidate();

                            // 重新调整一次 caret 位置。因为有可能在最后一行删除最后一个字符时突然行数减少
                            _record.MoveByOffs(_global_offs + 1, -1, out info);

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
                    EndFieldSelect();
                    break;
                case Keys.ControlKey:
                    _controlPressed = false;
                    EndFieldSelect();
                    break;
            }
            base.OnKeyUp(e);
        }

        public const char KERNEL_SUBFLD = (char)31; // '▼';	// '‡';  子字段指示符内部代用符号

        protected override void OnKeyPress(KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case (char)Keys.Escape:
                    break;
                /*
            case '\r':
            case '\n':
                break;
                */
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

        // 键盘输入回车时，是否自动填充分离位置左右两侧的文本，以防止出现不足 3 或 5 字符的短字段内容。
        public bool PadWhileReturning = false;
        public char PaddingChar = ' ';

        // 处理输入字符
        public virtual bool ProcessInputChar(char ch, HitInfo info)
        {
            if (!(ch >= 32 || ch == '\r'))
                return false;

            // 检测键盘输入速度
            var delay = _keySpeedDetector.Detect();

            if (HasBlock())
                SoftlyRemoveBolckText();

            var action = "input";
            if (ch == '\r')
            {
                return ProcessInputReturnChar(ch, info, delay);
#if REMOVED
                ch = (char)0x1e;
                action = "return";

                // 如果在头标区的末尾，则调整为下一字符开头，插入一个字段结束符
                if (info.ChildIndex == 0 && info.TextIndex >= 24)
                {
                    // 插入一个字符
                    ReplaceText(_global_offs,
                        _global_offs,
                        ch.ToString(),
                        delay_update: delay,
                        false);
                    // 修改后，插入符定位到头标区下一字段的开头
                    MoveCaret(HitByGlobalOffs(24 + 1, -1));
                }
                else if (info.ChildIndex == 0 && info.TextIndex < 24)
                {
                    // 在头标区内回车，补足空格
                    var fill_char_count = 24 - info.TextIndex;
                    var fragment = new string('_', fill_char_count);
                    // 插入一个字符
                    ReplaceText(_global_offs,
                        _global_offs,
                        fragment,
                        delay_update: delay,
                        false);
                    // 修改后，插入符定位到头标区下一字段的开头
                    MoveCaret(HitByGlobalOffs(24 + 1, -1));

                }
                else
                {
                    // (已经优化) 如果在一个字段末尾 caret 位置插入回车，
                    // 可以优化为先向后移动 1 char，然后插入回车，这样导致更新的数据更少

                    // 插入一个字符
                    ReplaceText(_global_offs,
                        _global_offs,
                        ch.ToString(),
                        delay_update: delay,
                        false);

                    // 向前移动一次 Caret
                    MoveGlobalOffsAndBlock(1);
                }

                return true;
#endif
            }
            else if (ch == '\\')
                ch = KERNEL_SUBFLD;

            // TODO: 检查覆盖输入字符后，当前相关 Region 是否有足够的字符。如果不足则需要补足。
            var replace = this._record.GetReplaceMode(_caretInfo,
                action,
                PaddingChar,
                out string fill_content);
            if (string.IsNullOrEmpty(fill_content) == false)
                ReplaceText(_global_offs,
                    _global_offs,
                    fill_content,
                    delay_update: delay,
                    false);
            if (replace)
                ReplaceText(_global_offs,
                    _global_offs + 1,
                    ch.ToString(),
                    delay_update: delay,
                    false);
            else
                ReplaceText(_global_offs,
                    _global_offs,
                    ch.ToString(),
                    delay_update: delay,
                    false);

            // 向前移动一次 Caret
            MoveGlobalOffsAndBlock(1);
            // e.Handled = true;
            return true;
        }

        public virtual void SplitPadding(
            ref string left,
            ref string right)
        {
            if (left.Length <= 5)
            {
                if (left.Length == 3)
                {
                    var field_name = left.Substring(0, 3);
                    if (MarcField.IsControlFieldName(field_name))
                        goto SKIP;
                }
                left = left.PadRight(5, PaddingChar);
            }

        SKIP:
            if (right.Length <= 5)
            {
                if (right.Length == 3)
                {
                    var field_name = right.Substring(0, 3);
                    if (MarcField.IsControlFieldName(field_name))
                        return;
                }
                right = right.PadRight(5, PaddingChar);
            }
        }

        // 不包含字段结束符
        void GetLeftRight(out string left,
            out string right)
        {
            var infos = this._record.LocateFields(_global_offs, _global_offs);
            if (infos == null || infos.Length == 0)
            {
                left = "";
                right = "";
                return;
            }
            var info = infos[0];
            var index = info.Index;
            var field = this._record.GetField(index);
            //var start_offs = _global_offs - info.StartLength;
            var text = field.MergePureText();
            left = text.Substring(0, info.StartLength);
            right = text.Substring(info.StartLength);
        }

        // 处理输入回车字符
        public virtual bool ProcessInputReturnChar(char ch,
            HitInfo info,
            bool delay)
        {
            if (ch == '\r')
            {
                ch = (char)0x1e;

                // 如果在头标区的末尾，则调整为下一字符开头，插入一个字段结束符
                if (info.ChildIndex == 0 && info.TextIndex >= 24)
                {
                    if (PadWhileReturning)
                    {
                        // 插入一个回车字符后，新的字段就内容为空，需要插入 5 个空格
                        ReplaceText(_global_offs,
    _global_offs,
    new string(PaddingChar, 5) + ch.ToString(),
    delay_update: delay,
    false);
                        // 修改后，插入符定位到头标区下一字段的开头
                        SetGlobalOffs(24);
                        MoveCaret(HitByGlobalOffs(24 + 1, -1));
                    }
                    else
                    {
                        // 插入一个字符
                        ReplaceText(_global_offs,
                            _global_offs,
                            ch.ToString(),
                            delay_update: delay,
                            false);
                        // 修改后，插入符定位到头标区下一字段的开头
                        SetGlobalOffs(24);
                        MoveCaret(HitByGlobalOffs(24 + 1, -1));
                    }
                }
                else if (info.ChildIndex == 0 && info.TextIndex < 24)
                {
                    // 在头标区内回车，补足空格
                    var fill_char_count = 24 - info.TextIndex;
                    // TODO: 头标区是否使用特殊字符填充?
                    var fragment = new string(PaddingChar, fill_char_count);
                    // 插入一个字符
                    ReplaceText(_global_offs,
                        _global_offs,
                        fragment,
                        delay_update: delay,
                        false);

                    if (PadWhileReturning)
                    {
                        // 观察插入点以后的新字段内容，如果不足 3 或 5 字符需要补齐
                        var pad_info = PaddingRight(24);
                        if (string.IsNullOrEmpty(pad_info.Text) == false)
                        {
                            ReplaceText(24 + pad_info.Offs, // 插入在原有字符后面
    24 + pad_info.Offs,
    pad_info.Text,
    delay_update: delay,
    false);
                        }
                    }

                    // 修改后，插入符定位到头标区下一字段的开头
                    // TODO: 使用 MoveGlobalOffsAndBlock(left.Length - old_left.Length + 1);

                    SetGlobalOffs(24);
                    MoveCaret(HitByGlobalOffs(24 + 1, -1));
                }
                else
                {
                    // *** 其它普通字段内插入

                    if (PadWhileReturning)
                    {
                        // 获得插入点以后直到字段结束符这一段的文字内容，
                        // 触发一个函数，让它决定是否要补充字符，如何补充。
                        GetLeftRight(out string left,
                            out string right);
                        var old_left = left;
                        var old_right = right;
                        SplitPadding(
                ref left,
                ref right);
                        // 原来的 text
                        var old_text = old_left + old_right;
                        var new_text = left + Metrics.FieldEndCharDefault + right;

                        // TODO: 优化，从两端向中间寻找，找到中间不一样的一段，只替换不一样的一段

                        ReplaceText(_global_offs - old_left.Length,
        _global_offs + old_right.Length + 1,
        new_text,
        delay_update: delay,
        false);

                        // 向前移动 Caret
                        MoveGlobalOffsAndBlock(left.Length - old_left.Length + 1);
                    }
                    else
                    {
                        // (已经优化) 如果在一个字段末尾 caret 位置插入回车，
                        // 可以优化为先向后移动 1 char，然后插入回车，这样导致更新的数据更少
                        // 插入一个字符
                        ReplaceText(_global_offs,
                            _global_offs,
                            ch.ToString(),
                            delay_update: delay,
                            false);

                        // 向前移动一次 Caret
                        MoveGlobalOffsAndBlock(1);
                    }
                }

                return true;
            }
            return false;
        }

        class PaddingInfo
        {
            // Text 要插入的偏移位置。注意是从测试起点计算
            public int Offs { get; set; }
            // 要插入的文字内容
            public string Text { get; set; }
        }

        PaddingInfo PaddingRight(int offs)
        {
            var test_length = 5;    // 测试用字符串的最小长度
            // right_count 有可能越过最大长度
            var test_string = this._record.MergeText(offs, offs + test_length);
            // 寻找字段结束符
            var index = test_string.IndexOf(Metrics.FieldEndCharDefault);
            if (index == -1 && test_string.Length >= test_length)
                return new PaddingInfo();
            if (index == -1)
                index = 0;
            if (index < 3)
                return new PaddingInfo
                {
                    Offs = index,
                    Text = new string(PaddingChar, 5 - index),
                };
            // 检查字段名是否为控制字段
            if (MarcField.IsControlFieldName(test_string.Substring(0, 3)))
            {
                // 控制字段，3个字符已经足够
                return new PaddingInfo();
            }
            // 否则需要至少 5 字符
            if (index < 5)
                return new PaddingInfo
                {
                    Offs = index,
                    Text = new string(PaddingChar, 5 - index),
                };
            return new PaddingInfo();
        }

#if REMOVED
        // 有几种观察。
        // 第一种，为了插入回车。
        // 1) 如果 offs < 24，左边(一直到整个开头)不足 24 字符，则左边补足空格。注: 中间即便有字段结束符也被当作头标区内容，因为头标区是用位置和长度定义的
        // 2) 右边不足 3 或者 5 字符，则右边补足空格。到底是 3 还是 5 字符，取决于前三字符是否为控制字段名，控制字段只要求 3 字符足够，否则要 5 字符才够
        // 第二种，为了插入其它普通字符
        // 要求插入点左边至少有 5 字符，如果不足，则从 offs 点开始插入足够的字符。到底是 3 或 5 要看字段名
        // 插入之后，注意保留插入的字符数信息，便于调用者调整插入符位置
        public string Padding(int offs, 
            int left_count,
            int right_count)
        {
            int total_length = this.TextLength;
            if (left_count > offs)
                left_count = offs;
            if (offs + right_count > total_length)
                right_count = total_length - offs;
            var start = offs - left_count;
            var end = offs + right_count;
            var text = this.MergeText(start, end);


            // 观察左边是否不足 5 字符
        }
#endif


        // 平移全局偏移量，和平移块范围
        bool MoveGlobalOffsAndBlock(int delta)
        {
            if (_global_offs + delta < 0)
                return false;

            DetectBlockChange1(_blockOffs1, _blockOffs2);

            var start_offs = _global_offs; // 记录开始偏移量

            HitInfo info = null;
            if (delta >= 0)
            {
                // 为了避免向右移动后 caret 处在令人诧异的等同位置，向右移动也需要模仿向左的 -1 特征
                // 注: 诧异位置比如头标区的右侧末尾，001 字段的字段名末尾，等等
                info = HitByGlobalOffs(_global_offs + delta + 1, -1);
            }
            else
                info = HitByGlobalOffs(_global_offs, delta);
            SetGlobalOffs(info.Offs); // 更新 _global_offs
            MoveCaret(info);

            _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标

            // 平移块范围
            if (_blockOffs1 >= start_offs)
                _blockOffs1 += delta;
            if (_blockOffs2 >= start_offs)
                _blockOffs2 += delta;

            // 块定义发生刷新才有必要更新变化的区域
            InvalidateBlockRegion();
            return true;
        }

        HitInfo HitByGlobalOffs(int offs, int delta = 0)
        {
            _record.MoveByOffs(offs, delta, out HitInfo info);
            return info;
        }

        public bool HasBlock()
        {
            return _blockOffs1 != _blockOffs2; // 选中范围不相等，表示有选中范围
        }

        // 柔和地删除块中文字。所谓柔和的意思是，保留固定长内容的字符数(只把这些字符替换为空格)
        public bool SoftlyRemoveBolckText()
        {
            if (_blockOffs1 == _blockOffs2)
                return false; // 不存在选中范围

            var start = Math.Min(_blockOffs1, _blockOffs2);
            var length = Math.Abs(_blockOffs1 - _blockOffs2);

            /*
            text = text.Remove(start, length);

            Relayout(false);
            */
            var mask_text = _record.MergeTextMask(start, start + length);

            ReplaceText(start,
                start + length,
                MarcRecord.CompressMaskText(mask_text),
                delay_update: false,
                false);

            _blockOffs1 = start;
            _blockOffs2 = start;

            if (_global_offs > start)
            {
                // DeltaGlobalOffs(-length); // 调整 _global_offs
                AdjustGlobalOffs(-(length - 1));
                MoveGlobalOffsAndBlock(-1);
            }
            //Invalidate();
            return true;


#if REMOVED
            string process_mask_text(string text)
            {
                // 将 mask_text 中 0x01 字符替换为空格，其余内容丢弃
                var result = new StringBuilder();
                foreach (var ch in text)
                {
                    if (ch == (char)0x01)
                        result.Append(' ');
                }
                return result.ToString();
            }
#endif
        }


        // 删除块中的文字。硬性删除的版本，可能会导致固定长内容的字符数变化
        public bool RawRemoveBolckText()
        {
            if (_blockOffs1 == _blockOffs2)
                return false; // 不存在选中范围

            var start = Math.Min(_blockOffs1, _blockOffs2);
            var length = Math.Abs(_blockOffs1 - _blockOffs2);

            /*
            text = text.Remove(start, length);

            Relayout(false);
            */
            ReplaceText(start,
                start + length,
                "",
                delay_update: false,
                false);

            _blockOffs1 = start;
            _blockOffs2 = start;

            if (_global_offs > start)
            {
                // DeltaGlobalOffs(-length); // 调整 _global_offs
                AdjustGlobalOffs(-(length - 1));
                MoveGlobalOffsAndBlock(-1);
            }
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
            _fieldProperty.Refresh(this.Font);
            SetCompositionWindowPos();
            base.OnLoad(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Imm32.ImmReleaseContext(this.Handle, hIMC);
            // User32.DestroyCaret();

            #region 延迟刷新
            DestroyTimer();
            #endregion

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

                // this.Invalidate();
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

        // 多语种时不完备，即将废弃
        void InvalidateBlock(bool trigger_event = true)
        {
            // 移动插入符的情况，不涉及到块定义和变化
            if (_oldOffs1 == _oldOffs2
                && _blockOffs1 == _blockOffs2)
                return;
            bool changed = false;
            if (InvalidateBlock(_oldOffs1, _blockOffs1))
                changed = true;
            if (InvalidateBlock(_oldOffs2, _blockOffs2))
                changed = true;

            if (trigger_event == true && changed)
            {
                this.BlockChanged?.Invoke(this, new EventArgs());
            }
        }

        void InvalidateBlockRegion(bool trigger_event = true)
        {
            // 移动插入符的情况，不涉及到块定义和变化
            if (_oldOffs1 == _oldOffs2
                && _blockOffs1 == _blockOffs2)
                return;
            bool changed = false;
            if (InvalidateBlockRegion(_oldOffs1, _blockOffs1))
                changed = true;
            if (InvalidateBlockRegion(_oldOffs2, _blockOffs2))
                changed = true;

            if (trigger_event == true && changed)
            {
                this.BlockChanged?.Invoke(this, new EventArgs());
            }
        }


        // 多语种时不完备，即将废弃
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

        bool InvalidateBlockRegion(int offs1, int offs2)
        {
            if (offs1 == offs2)
                return false;
            var region = GetBlockRegion(offs1, offs2);
            if (region != null)
            {
                this.Invalidate(region);
                region.Dispose();
                return true;
            }

            return false;
        }

        // 获得表示块大致范围的 Rectangle
        Region GetBlockRegion(
            int offs1,
            int offs2)
        {
            if (offs1 < 0 || offs2 < 0)
                return null;

            if (offs1 == offs2)
                return null;

            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;

            int start = Math.Min(offs1, offs2);
            int end = Math.Max(offs1, offs2);

            var region = _record.GetRegion(start, end, 1);
            if (region != null)
                region.Offset(x, y);
            return region;
        }


        // 获得表示块大致范围的 Rectangle
        Rectangle GetBlockRectangle(int offs1, int offs2)
        {
            if (offs1 < 0 || offs2 < 0)
                return System.Drawing.Rectangle.Empty;

            if (offs1 == offs2)
                return System.Drawing.Rectangle.Empty;

            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;

            int start = Math.Min(offs1, offs2);
            int end = Math.Max(offs1, offs2);
            _record.MoveByOffs(start, 0, out HitInfo info1);
            _record.MoveByOffs(end, 0, out HitInfo info2);

            // start 或者 end 越出当前合法范围，返回一个巨大的矩形。迫使窗口全部失效
            if (info1.Area != Area.Text
                || info2.Area != Area.Text)
                return new Rectangle(x + 0,
    y + 0,
    this.AutoScrollMinSize.Width,   // document width
    this.AutoScrollMinSize.Height);

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
            Debug.Assert(info1.Y <= info2.Y);
            return new Rectangle(x + 0,
                y + info1.Y,
                this.AutoScrollMinSize.Width,   // document width
                info2.Y + info2.LineHeight - info1.Y);
        }

        public DomRecord GetDomRecord()
        {
            return new DomRecord(this._record);
        }

    }
}
