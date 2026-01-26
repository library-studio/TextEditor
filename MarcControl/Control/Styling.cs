// ""

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryStudio.Forms.MarcControlDialog;
using Newtonsoft.Json;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 定制风格
    /// </summary>
    public partial class MarcControl
    {
        // IContext 是 IBox 提供的通用机制。通过一个参数传递到各个 API 调用中
        Context _context = null;

        // Metrics 是 MarcRecord 和 MarcField 才有的特殊的，针对 MARC 结构的风格参数
        // TODO: 建议将 _fieldProperty 更名为 _marcMetrics
        Metrics _marcMetrics = new Metrics();

        public Metrics Metrics { get { return _marcMetrics; } }

        string _theme_name = null;
        public string ColorThemeName
        {
            get
            {
                return _theme_name;
            }
            set
            {
                if (_theme_name != value)
                {
                    _marcMetrics.UseColorTheme(value);
                    if (value == "default"
                        || string.IsNullOrEmpty(value))
                    {
                        _theme_name = null;
                    }
                    else
                    {
                        _theme_name = value;
                    }

                    OnColorChanged(new EventArgs());
                }
            }
        }

        public const string CUSTOM_THEME_CAPTION = "[定制]";

        // 定制颜色主题被改变，的事件。
        // 通过它可以得知定制颜色主题被首次配置或者被修改
        public EventHandler CustomThemeChanged;

        public bool IsCustomColorTheme()
        {
            if (_theme_name == CUSTOM_THEME_CAPTION)
                return true;
            return false;
        }

        public ColorTheme GetCustomColorTheme()
        {
            return (this._marcMetrics as ColorTheme)?.Clone();
        }

        public void SetCustomColorTheme(ColorTheme theme)
        {
            if (theme != null)
            {
                this._theme_name = "[定制]";
                // 比较新旧两套 Theme 有无实质性变化
                if (this._marcMetrics.Clone().ToJson() != theme.ToJson())
                {
                    this._marcMetrics.ApplyColorTheme(theme);
                    OnColorChanged(new EventArgs());
                    OnCustomColorThemeChanged(new EventArgs());
                }
            }
            else
            {
                // 恢复默认主题
                this._marcMetrics.ApplyColorTheme(ColorTheme.ThemeDefault());
                this._theme_name = "默认";
                OnColorChanged(new EventArgs());
            }
        }

        public virtual void OnCustomColorThemeChanged(EventArgs e)
        {
            this.CustomThemeChanged?.Invoke(this, e);
        }

        public virtual void OnColorChanged(EventArgs e)
        {
            // 确保新的颜色可以显示出来
            // TODO: IBox 实现 ClearCache() 接口，可以清除各个部分缓存的颜色
            Relayout(this._record.MergeText(), false);
            // 底部有部分空白不属于任何字段，也要 Invalidate()
            this.Invalidate();
        }

        public Context GetDefaultContext()
        {
            /*
            return new Context {
                GetFont = (o, t) =>
                {
                    return ContentFontGroup;
                }
            };
            */
            return new Context
            {
                SplitRange = (o, content) =>
                {
                    // return SimpleText.SegmentSubfields(content, '\x001f', true); // 只能让子字段符号一个字符显示为特殊颜色
                    // return SimpleText.SegmentSubfields2(content, '\x001f', 2, true);
                    var segments = MarcField.SegmentSubfields(content, '\x001f', 2);
                    if (HighlightBlankChar == ' ')
                    {
                        return segments;
                    }

                    return MarcField.SplitBlankChar(segments);
                },
                ConvertText = (text) =>
                {
                    // ꞏ▼▒■☻ⱡ⸗ꜜꜤꞁꞏסּ⁞

                    // TODO: 可以考虑把英文空格和中文空格显示为易于辨识的特殊字符
                    if (_highlightBlankChar != ' ')
                    {
                        return text.Replace("\x001f", "▼").Replace("\x001e", "ꜜ").Replace(' ', _highlightBlankChar);
                    }

                    return text.Replace("\x001f", "▼").Replace("\x001e", "ꜜ");
                },
                GetForeColor = (o, highlight) =>
                {
                    if (highlight)
                    {
                        return _marcMetrics?.HightlightForeColor ?? SystemColors.HighlightText;
                    }

                    var range = o as Range;
                    if (range != null
                        && range.Tag is MarcField.Tag tag)
                    {
                        // 子字段名文本为红色
                        if (tag.Delimeter)
                        {
                            return _marcMetrics?.DelimeterForeColor ?? Metrics.DefaultDelimeterForeColor;
                        }

                        if (range.Text == " ")
                        {
                            return _marcMetrics?.BlankCharForeColor ?? Metrics.DefaultBlankCharForeColor;
                        }
                    }
                    if (this._readonly)
                    {
                        return _marcMetrics?.ReadOnlyForeColor ?? SystemColors.ControlText;
                    }

                    return _marcMetrics?.ForeColor ?? SystemColors.WindowText;
                },
                GetBackColor = (o, highlight) =>
                {
                    if (highlight)
                    {
                        return _marcMetrics?.HighlightBackColor ?? SystemColors.Highlight;
                    }

                    var range = o as Range;
                    if (range != null
                        && range.Tag is MarcField.Tag tag
                        && tag.Delimeter)
                    {
                        return _marcMetrics?.DelimeterBackColor ?? Metrics.DefaultDelimeterBackColor; // 子字段名文本为红色
                    }
                    if (range != null)
                    {
                        return Color.Transparent;
                    }

                    if (this._readonly)
                    {
                        return _marcMetrics?.ReadOnlyBackColor ?? SystemColors.Control;
                        // backColor = ControlPaint.Dark(backColor, 0.01F);
                    }
                    var backColor = _marcMetrics?.BackColor ?? SystemColors.Window;
                    return backColor;
                },
                // PaintBack = _paintBackfunc,
                GetFont = (o, t) =>
                {
                    if (t is MarcField.Tag tag
                        && tag.Delimeter)
                    {
                        return FixedFontGroup;
                    }

                    if (o is IBox)
                    {
                        var box = o as IBox;
                        if (Metrics.IsAncestorFixed(o, true)/*line.Name == "name" || line.Name == "indicator"*/)
                        {
                            return FixedFontGroup;
                        }
                        else if (box.Name == "!caption")
                        {
                            return CaptionFontGroup;
                        }
                        else if (box.Name == "!content")
                        {
                            var field = Metrics.GetAncestorField(box, false);
                            if (field.IsHeader || field.IsControlField
                            || field.FieldName.StartsWith("1")) // 1XX 字段是否一定是固定长的子字段，要看具体 MARC 格式的情况，比如 MARC21 要查一下
                            {
                                return FixedFontGroup;
                            }
                            else
                            {
                                return ContentFontGroup;
                            }

#if REMOVED
                            if (box.Parent is MarcField)
                            {
                                var field = box.Parent as MarcField;

                                if (field.IsHeader || field.IsControlField
                                || field.FieldName.StartsWith("1")) // 1XX 字段是否一定是固定长的子字段，要看具体 MARC 格式的情况，比如 MARC21 要查一下
                                {
                                    return FixedFontGroup;
                                }
                                else
                                {
                                    return ContentFontGroup;
                                }
                            }
#endif
                        }
                    }
                    return ContentFontGroup;
                }
            };
        }

        // 设置视觉风格
        // parameters:
        //      active_page 0:颜色主题 1:字体
        public bool SettingVisualStyle(int active_page = 0)
        {
            using (var dlg = new VisualStyleDialog())
            {
                dlg.RefControl = this;
                dlg.LoadAllFont(this);
                dlg.ColorThemeName = this.ColorThemeName;
                if (IsCustomColorTheme())
                    dlg.CustomColorTheme = this.GetCustomColorTheme();
                dlg.SelectedPageIndex = active_page;
                dlg.StartPosition = FormStartPosition.CenterParent;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    dlg.ApplyAllFont(this);
                    if (dlg.CustomColorTheme != null)
                    {
                        this.SetCustomColorTheme(dlg.CustomColorTheme);
                    }
                    else
                    {
                        this.ColorThemeName = dlg.ColorThemeName;
                    }
                    return true;
                }
            }

            return false;
        }

        // 设置字体和字体颜色
        public bool SettingFont()
        {
            return SettingVisualStyle(1);
#if REMOVED
            // TODO: “微软雅黑 light”字体为何在打开的 FontDialog 中不能正确定位字体名
            using (FontDialog dlg = new FontDialog())
            {
                //dlg.ShowColor = true;
                //dlg.Color = this._fieldProperty?.ForeColor ?? Color.Black;
                dlg.Font = this.Font;
                dlg.ShowApply = true;
                dlg.ShowHelp = true;
                dlg.AllowVerticalFonts = false;

                dlg.Apply += (s, e) =>
                {
                    this.Font = dlg.Font;
                    SetColor();
                };
                if (dlg.ShowDialog(this) != DialogResult.OK)
                {
                    return false;
                }

                this.Font = dlg.Font;
                SetColor();
                return true;

                void SetColor()
                {
                    return;
                    if (this._fieldProperty != null)
                    {
                        this._fieldProperty.ForeColor = dlg.Color;
                        this.Relayout(this._record.MergeText(), false);
                    }
                }
            }
#endif
        }

        class UiState
        {
            // 提示区域像素宽度
            public int CaptionPixelWidth { get; set; }

            public char HighlightBlankChar { get; set; } = ' ';

            public string ColorThemeName { get; set; }


            public ColorTheme CustomColorTheme { get; set; }

            public string Font { get; set; }

            public string FixedSizeFont { get; set; }

            public string CaptionFont { get; set; }
        }

        // 用于存储和恢复编辑器 UI 状态的 JSON 字符串
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string UiStateJson
        {
            get
            {
                var state = new UiState
                {
                    CaptionPixelWidth = this.CaptionPixelWidth,
                    HighlightBlankChar = this.HighlightBlankChar,
                    ColorThemeName = this.ColorThemeName,
                    CustomColorTheme = this.IsCustomColorTheme() ? (this.GetCustomColorTheme() ?? null) : null,
                    Font = GetFontString(this.Font),
                    FixedSizeFont = GetFontString(this.FixedSizeFont),
                    CaptionFont = GetFontString(this.CaptionFont),
                };
                return JsonConvert.SerializeObject(state);
            }
            set
            {
                var state = JsonConvert.DeserializeObject<UiState>(value);
                if (state != null)
                {
                    this.BeginUpdate();
                    try
                    {
                        this.CaptionPixelWidth = state.CaptionPixelWidth;
                        this.HighlightBlankChar = state.HighlightBlankChar == 0 ? ' ' : state.HighlightBlankChar;
                        if (state.ColorThemeName == MarcControl.CUSTOM_THEME_CAPTION)
                        {
                            this.SetCustomColorTheme(state.CustomColorTheme);
                        }
                        else
                        {
                            this.ColorThemeName = state.ColorThemeName;
                        }

                        {
                            var font = GetFont(state.Font);
                            if (font != null)
                            {
                                this.Font = font;
                            }
                        }

                        {
                            var font = GetFont(state.FixedSizeFont);
                            if (font != null)
                            {
                                this.FixedSizeFont = font;
                            }
                        }

                        {
                            var font = GetFont(state.CaptionFont);
                            if (font != null)
                            {
                                this.CaptionFont = font;
                            }
                        }
                    }
                    finally
                    {
                        this.EndUpdate();
                    }
                }
            }
        }

        public static Font GetFont(string strFontString)
        {
            if (String.IsNullOrEmpty(strFontString) == false)
            {
                var converter = TypeDescriptor.GetConverter(typeof(Font));

                return (Font)converter.ConvertFromString(strFontString);
            }

            return null;
        }

        public static string GetFontString(Font font)
        {
            var converter = TypeDescriptor.GetConverter(typeof(Font));
            return converter.ConvertToString(font);
        }

#if REMOVED
        class UiState
        {
            public int CaptionPixelWidth { get; set; }

            public char HighlightBlankChar { get; set; } = ' ';

            public string ColorThemeName { get; set; }


            public string CustomColorThemeJson { get; set; }

            public string Font { get; set; }

            public static Font GetFont(string strFontString)
            {
                if (String.IsNullOrEmpty(strFontString) == false)
                {
                    var converter = TypeDescriptor.GetConverter(typeof(Font));

                    return (Font)converter.ConvertFromString(strFontString);
                }

                return null;
            }

            public static string GetFontString(Font font)
            {
                var converter = TypeDescriptor.GetConverter(typeof(Font));
                return converter.ConvertToString(font);
            }
        }

        // 用于存储和恢复编辑器 UI 状态的 JSON 字符串
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public string UiStateJson
        {
            get
            {
                var state = new UiState
                {
                    CaptionPixelWidth = this.CaptionPixelWidth,
                    HighlightBlankChar = this.HighlightBlankChar,
                    ColorThemeName = this.ColorThemeName,
                    CustomColorThemeJson = this.IsCustomColorTheme() ? (this.GetCustomColorTheme()?.ToJson() ?? string.Empty) : string.Empty,
                    Font = UiState.GetFontString(this.Font),
                };
                return JsonConvert.SerializeObject(state);
            }
            set
            {
                var state = JsonConvert.DeserializeObject<UiState>(value);
                if (state != null)
                {
                    this.BeginUpdate();
                    try
                    {
                        this.CaptionPixelWidth = state.CaptionPixelWidth;
                        this.HighlightBlankChar = state.HighlightBlankChar == 0 ? ' ' : state.HighlightBlankChar;
                        if (state.ColorThemeName == MarcControl.CUSTOM_THEME_CAPTION)
                        {
                            try
                            {
                                var theme = ColorTheme.FromJson(state.CustomColorThemeJson);
                                this.SetCustomColorTheme(theme);
                            }
                            catch
                            {

                            }
                        }
                        else
                        {
                            this.ColorThemeName = state.ColorThemeName;
                        }
                        var font = UiState.GetFont(state.Font);
                        if (font != null)
                        {
                            this.Font = font;
                        }
                    }
                    finally
                    {
                        this.EndUpdate();
                    }
                }
            }
        }
#endif

    }
}
