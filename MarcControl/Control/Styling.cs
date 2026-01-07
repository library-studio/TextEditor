// ""

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using LibraryStudio.Forms.MarcControlDialog;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 定制风格
    /// </summary>
    public partial class MarcControl
    {
        // IContext 是 IBox 提供的通用机制。通过一个参数传递到各个 API 调用中
        IContext _context = null;

        // Metrics 是 MarcRecord 和 MarcField 才有的特殊的，针对 MARC 结构的风格参数
        // TODO: 建议将 _fieldProperty 更名为 _marcMetrics
        Metrics _fieldProperty = new Metrics();

        public Metrics Metrics { get { return _fieldProperty; } }

        string _theme_name = null;
        public string ColorThemeName
        {
            get
            {
                return _theme_name;
            }
            set
            {
                _fieldProperty.UseColorTheme(value);
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
            return (this._fieldProperty as ColorTheme)?.Clone();
        }

        public void SetCustomColorTheme(ColorTheme theme)
        {
            if (theme != null)
            {
                this._fieldProperty.ApplyColorTheme(theme);
                this._theme_name = "[定制]";
                OnColorChanged(new EventArgs());
                OnCustomColorThemeChanged(new EventArgs());
            }
            else
            {
                // 恢复默认主题
                this._fieldProperty.ApplyColorTheme(ColorTheme.ThemeDefault());
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

        public IContext GetDefaultContext()
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
                        return text.Replace("\x001f", "▼").Replace("\x001e", "ꜜ").Replace(' ', _highlightBlankChar);
                    return text.Replace("\x001f", "▼").Replace("\x001e", "ꜜ");
                },
                GetForeColor = (o, highlight) =>
                {
                    if (highlight)
                        return _fieldProperty?.HightlightForeColor ?? SystemColors.HighlightText;
                    var range = o as Range;
                    if (range != null
                        && range.Tag is MarcField.Tag tag)
                    {
                        // 子字段名文本为红色
                        if (tag.Delimeter)
                            return _fieldProperty?.DelimeterForeColor ?? Metrics.DefaultDelimeterForeColor;
                        if (range.Text == " ")
                            return _fieldProperty?.BlankCharForeColor ?? Metrics.DefaultBlankCharForeColor;
                    }
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
                    {
                        return _fieldProperty?.DelimeterBackColor ?? Metrics.DefaultDelimeterBackColor; // 子字段名文本为红色
                    }
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
                // PaintBack = _paintBackfunc,
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
                            if (field.IsHeader || field.IsControlField
                            || field.FieldName.StartsWith("1")) // 1XX 字段是否一定是固定长的子字段，要看具体 MARC 格式的情况，比如 MARC21 要查一下
                                return FixedFontGroup;
                            else
                                return ContentFontGroup;
                        }
                    }
                    return ContentFontGroup;
                }
            };
        }

        // 设置视觉风格
        public bool SettingVisualStyle()
        {
            using (var dlg = new VisualStyleDialog())
            {
                dlg.RefControl = this;
                dlg.ColorThemeName = this.ColorThemeName;
                if (IsCustomColorTheme())
                    dlg.CustomColorTheme = this.Metrics;
                dlg.StartPosition = FormStartPosition.CenterParent;
                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
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
        }
    }
}
