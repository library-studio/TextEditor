using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 颜色主题
    /// </summary>
    public class ColorTheme
    {
        // 文字色
        [ColorNameAttribute("文字")]
        public Color ForeColor { get; set; } = SystemColors.WindowText;

        // 窗口背景色
        [ColorNameAttribute("窗口背景")]
        public Color BackColor { get; set; } = SystemColors.Window;

        // 只读状态的文字色
        [ColorNameAttribute("只读状态文字")]
        public Color ReadOnlyForeColor { get; set; } = SystemColors.ControlText;

        // 只读状态的背景色
        [ColorNameAttribute("只读状态背景")]
        public Color ReadOnlyBackColor { get; set; } = SystemColors.Control;

        // 选择部分的文字色
        [ColorNameAttribute("选择文字")]
        public Color HightlightForeColor { get; set; } = SystemColors.HighlightText;

        // 选择部分的背景色
        [ColorNameAttribute("选择背景")]
        public Color HighlightBackColor { get; set; } = SystemColors.Highlight;

        // 分隔字符的文字色
        [ColorNameAttribute("分隔字符文字")]
        public Color DelimeterForeColor { get; set; } = DefaultDelimeterForeColor;

        [ColorNameAttribute("分隔字符背景")]
        public Color DelimeterBackColor { get; set; } = DefaultDelimeterBackColor;

        // 文字色
        [ColorNameAttribute("提示文字")]
        public Color CaptionForeColor { get; set; } = DefaultCaptionForeColor;

        // 窗口背景色
        [ColorNameAttribute("提示背景")]
        public Color CaptionBackColor { get; set; } = DefaultCaptionBackColor;

        public static Color DefaultCaptionBackColor = SystemColors.Info;
        public static Color DefaultCaptionForeColor = SystemColors.InfoText;

        public static Color DefaultDelimeterForeColor = Color.DarkGreen;    // Color.DarkRed;
        public static Color DefaultDelimeterBackColor = Color.WhiteSmoke; //  Color.LightYellow;

        // 不可编辑区域的背景色
        [ColorNameAttribute("不可编辑背景")]
        public Color SolidColor { get; set; } = DefaultSolidColor;

        public static Color DefaultSolidColor = Color.LightGray;

        [ColorNameAttribute("边框")]
        public Color BorderColor { get; set; } = DefaultBorderColor;
        public static Color DefaultBorderColor = SystemColors.ControlDark;

        [ColorNameAttribute("输入焦点标记")]
        public Color FocusColor { get; set; } = DefaultFocuseColor;
        public static Color DefaultFocuseColor = SystemColors.MenuHighlight;

        [ColorName("空白标志字符")]
        public Color BlankCharForeColor { get; set; } = DefaultBlankCharForeColor;
        public static Color DefaultBlankCharForeColor = Color.Blue;

        #region 颜色主题

        /*
        public static IEnumerable<string> ColorThemeNames
        {
            get
            {
                return new string[] {
                    "默认",
                    "简约",
                    "暗黑",
                    "深蓝",
                    "绿色",
                    "极简",
                };
            }
        }
        */

        public void UseColorTheme(string theme_name, bool throw_exception = false)
        {
            var theme = GetTheme(theme_name);
            if (theme == null)
            {
                if (throw_exception == false)
                    return;
                throw new ArgumentException($"主题 '{theme_name}' 没有找到");
            }
            SetTheme(theme);
            /*
            switch(theme)
            {
                case null:
                case "":
                case "default":
                case "默认":
                    SetTheme(ThemeDefault());
                    return;
                case "simple":
                case "简约":
                    SetTheme(ThemeSimple());
                    return;
                case "dark":
                case "暗黑":
                    SetTheme(ThemeDark());
                    return;
                case "dark_blue":
                case "深蓝":
                    SetTheme(ThemeDarkBlue());
                    return;
                case "spring":
                case "绿色":
                case "春天":
                    SetTheme(ThemeSpring());
                    return;
                case "simplest":
                case "极简":
                    SetTheme(ThemeSimplest());
                    return;
            }
            */
        }


        public void SetTheme(ColorTheme theme)
        {
            this.ForeColor = theme.ForeColor;
            this.BackColor = theme.BackColor;
            this.ReadOnlyForeColor = theme.ReadOnlyForeColor;
            this.ReadOnlyBackColor = theme.ReadOnlyBackColor;
            this.HightlightForeColor = theme.HightlightForeColor;
            this.HighlightBackColor = theme.HighlightBackColor;
            this.DelimeterForeColor = theme.DelimeterForeColor;
            this.DelimeterBackColor = theme.DelimeterBackColor;
            this.CaptionForeColor = theme.CaptionForeColor;
            this.CaptionBackColor = theme.CaptionBackColor;
            this.SolidColor = theme.SolidColor;
            this.BorderColor = theme.BorderColor;
            this.FocusColor = theme.FocusColor;
            this.BlankCharForeColor = theme.BlankCharForeColor;
        }

        [ThemeName("default", "en")]
        [ThemeName("默认", "zh")]
        public static ColorTheme ThemeDefault()
        {
            return new ColorTheme();
        }

        // 子字段符号和子字段名不要突出显示，和普通字符一样颜色
        [ThemeName("simple", "en")]
        [ThemeName("简约", "zh")]
        public static ColorTheme ThemeSimple()
        {
            var theme = new ColorTheme();
            theme.DelimeterBackColor = Color.Transparent;
            theme.DelimeterForeColor = theme.ForeColor;
            theme.CaptionForeColor = Color.FromArgb(160, 160, 160);
            theme.CaptionBackColor = Color.FromArgb(250, 250, 250);
            theme.SolidColor = /*Color.Transparent; //*/  Color.FromArgb(240, 240, 240);
            theme.BorderColor = /*Color.Transparent; //*/  Color.FromArgb(220, 220, 220);
            return theme;
        }

        // 极简
        [ThemeName("simplest", "en")]
        [ThemeName("极简", "zh")]
        public static ColorTheme ThemeSimplest()
        {
            var theme = new ColorTheme();
            theme.HightlightForeColor = Color.White;
            theme.HighlightBackColor = Color.FromArgb(80, 80, 80);
            theme.DelimeterBackColor = Color.Transparent;
            theme.DelimeterForeColor = theme.ForeColor;
            theme.CaptionForeColor = Color.FromArgb(160, 160, 160);
            theme.CaptionBackColor = Color.Transparent; // Color.FromArgb(250, 250, 250);
            theme.SolidColor = Color.Transparent;
            theme.BorderColor = Color.Transparent;
            theme.FocusColor = Color.DarkGray;
            return theme;
        }

        // TODO: 高对比度

        [ThemeName("dark", "en")]
        [ThemeName("暗黑", "zh")]
        public static ColorTheme ThemeDark()
        {
            var theme = new ColorTheme();
            theme.ForeColor = Color.FromArgb(230, 230, 230);
            theme.BackColor = Color.Black;
            theme.ReadOnlyForeColor = Color.Gray;
            theme.ReadOnlyBackColor = Color.Black;
            theme.HightlightForeColor = Color.Black;
            theme.HighlightBackColor = Color.FromArgb(180, 180, 180);
            theme.DelimeterForeColor = Color.White;
            theme.DelimeterBackColor = Color.Transparent; //  Color.FromArgb(50, 50, 50);
            theme.CaptionForeColor = Color.FromArgb(180, 180, 180);
            theme.CaptionBackColor = Color.FromArgb(70, 70, 70);
            theme.SolidColor = Color.FromArgb(60, 60, 60);
            theme.BorderColor = Color.DarkGray;
            theme.FocusColor = Color.DarkGray;
            theme.BlankCharForeColor = Color.DarkGray;
            return theme;
        }

        [ThemeName("dark_blue", "en")]
        [ThemeName("深蓝", "zh")]
        public static ColorTheme ThemeDarkBlue()
        {
            var theme = new ColorTheme();
            theme.ForeColor = Color.FromArgb(250, 250, 200);
            theme.BackColor = Color.FromArgb(0, 0, 80);
            theme.ReadOnlyForeColor = Color.Gray;
            theme.ReadOnlyBackColor = Color.Black;
            theme.HightlightForeColor = Color.White;
            theme.HighlightBackColor = Color.FromArgb(70, 70, 200);
            theme.DelimeterForeColor = Color.FromArgb(255, 230, 0);
            theme.DelimeterBackColor = Color.FromArgb(0, 0, 0);
            theme.CaptionForeColor = Color.FromArgb(160, 160, 180);
            theme.CaptionBackColor = Color.FromArgb(70, 70, 100);
            theme.SolidColor = Color.FromArgb(50, 50, 90);
            theme.BorderColor = Color.DarkGray;
            theme.FocusColor = Color.FromArgb(160, 160, 255);
            theme.BlankCharForeColor = Color.DarkGray;
            return theme;
        }

        [ThemeName("spring", "en")]
        [ThemeName("绿色", "zh")]
        [ThemeName("春天", "zh")]
        public static ColorTheme ThemeSpring()
        {
            var theme = new ColorTheme();
            theme.ForeColor = Color.FromArgb(10, 10, 10);
            theme.BackColor = Color.FromArgb(255, 255, 255);
            theme.ReadOnlyForeColor = Color.Gray;
            theme.ReadOnlyBackColor = Color.FromArgb(200, 200, 200);
            theme.HightlightForeColor = Color.White;
            theme.HighlightBackColor = Color.FromArgb(0, 120, 120);
            theme.DelimeterForeColor = Color.FromArgb(30, 80, 0);
            theme.DelimeterBackColor = Color.Transparent;
            theme.CaptionForeColor = Color.FromArgb(60, 220, 0);
            theme.CaptionBackColor = Color.FromArgb(240, 255, 240);
            theme.SolidColor = Color.FromArgb(150, 190, 150);
            theme.BorderColor = Color.FromArgb(130, 170, 130);
            theme.FocusColor = Color.FromArgb(160, 255, 160);
            theme.BlankCharForeColor = Color.DarkGray;
            return theme;
        }

        // 获得若干颜色属性的 PropertyInfo
        public IEnumerable<PropertyInfo> GetColorProperties()
        {
            foreach (var info in typeof(ColorTheme).GetProperties())
            {
                var attrs = info.GetCustomAttributes<ColorNameAttribute>();
                if (attrs.Count() > 0)
                    yield return info;
            }
        }

        // 个数和主题数相同
        public static IEnumerable<string> Captions(string lang = null)
        {
            foreach (var info in typeof(ColorTheme).GetMethods())
            {
                var attrs = info.GetCustomAttributes<ThemeNameAttribute>();
                string caption = null;
                if (string.IsNullOrEmpty(lang) == false)
                    caption = attrs.Where(a => a.Lang.StartsWith(lang))
                        .Select(a => a.Caption)
                        .FirstOrDefault();
                if (caption != null)
                    yield return caption;
                else
                {
                    caption = attrs.Select(a => a.Caption).FirstOrDefault();
                    if (caption != null)
                        yield return caption;
                }
            }
        }

        // 数量要多于主题数。也就是说一个主题可能有好多个名字
        public static IEnumerable<string> AllLangCaptions()
        {
            foreach (var info in typeof(ColorTheme).GetMethods())
            {
                var attrs = info.GetCustomAttributes<ThemeNameAttribute>();
                foreach (var caption in attrs.Select(a => a.Caption))
                {
                    yield return caption;
                }
            }
        }

        public static ColorTheme GetTheme(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                name = "default";
            }
            // 获得所有带 ThemeName 属性的方法
            // 一般是 public static ColorTheme ThemeSpring() 这样的
            // 注意返回的 ColorTheme 是克隆后的对象。克隆的原因是避免修改这种对象影响到 Control 中正在使用的 ColorTheme 对象
            foreach (var info in typeof(ColorTheme).GetMethods())
            {
                var attrs = info.GetCustomAttributes<ThemeNameAttribute>();
                if (attrs.Where(a => a.Caption == name).Any())
                    return info.Invoke(null,
                        null) as ColorTheme;
            }

            return null;
        }

        public void ApplyColorTheme(ColorTheme theme)
        {
            this.ForeColor = theme.ForeColor;
            this.BackColor = theme.BackColor;
            this.ReadOnlyForeColor = theme.ReadOnlyForeColor;
            this.ReadOnlyBackColor = theme.ReadOnlyBackColor;
            this.HightlightForeColor = theme.HightlightForeColor;
            this.HighlightBackColor = theme.HighlightBackColor;
            this.DelimeterForeColor = theme.DelimeterForeColor;
            this.DelimeterBackColor = theme.DelimeterBackColor;
            this.CaptionForeColor = theme.CaptionForeColor;
            this.CaptionBackColor = theme.CaptionBackColor;
            this.SolidColor = theme.SolidColor;
            this.BorderColor = theme.BorderColor;
            this.FocusColor = theme.FocusColor;
            this.BlankCharForeColor = theme.BlankCharForeColor;
        }

        public ColorTheme Clone()
        {
            var result = new ColorTheme();
            result.ForeColor = this.ForeColor;
            result.BackColor = this.BackColor;
            result.ReadOnlyForeColor = this.ReadOnlyForeColor;
            result.ReadOnlyBackColor = this.ReadOnlyBackColor;
            result.HightlightForeColor = this.HightlightForeColor;
            result.HighlightBackColor = this.HighlightBackColor;
            result.DelimeterForeColor = this.DelimeterForeColor;
            result.DelimeterBackColor = this.DelimeterBackColor;
            result.CaptionForeColor = this.CaptionForeColor;
            result.CaptionBackColor = this.CaptionBackColor;
            result.SolidColor = this.SolidColor;
            result.BorderColor = this.BorderColor;
            result.FocusColor = this.FocusColor;
            result.BlankCharForeColor = this.BlankCharForeColor;
            return result;
        }

        public static string GetNameCaption(PropertyInfo info, string lang)
        {
            var attrs = info.GetCustomAttributes<ColorNameAttribute>();
            var result = attrs.Where(o => o.Lang.StartsWith(lang))
                .Select(o => o.Caption).FirstOrDefault();
            if (result != null)
                return result;
            return attrs.Select(o => o.Caption).FirstOrDefault();
        }

        #endregion

        public string ToJson()
        {
            return JsonConvert.SerializeObject((ColorTheme)this, Formatting.Indented);
        }

        public static ColorTheme FromJson(string json)
        {
            return JsonConvert.DeserializeObject<ColorTheme>(json);
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class NameAttribute : Attribute
    {
        public string Caption { get; set; }

        public string Lang { get; set; }

        public NameAttribute(string caption, string lang = "zh")
        {
            Caption = caption;
            Lang = lang;
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    public class ColorNameAttribute : NameAttribute
    {
        public ColorNameAttribute(string caption, string lang = "zh")
            : base(caption, lang)
        {
        }
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = true)]
    public class ThemeNameAttribute : NameAttribute
    {
        public ThemeNameAttribute(string caption, string lang = "zh")
            : base(caption, lang)
        {
        }
    }

}
