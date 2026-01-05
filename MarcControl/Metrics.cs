using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms.VisualStyles;

namespace LibraryStudio.Forms
{
    // 字段的一些共同参数
    public class Metrics
    {
        public GetReadOnlyFunc GetReadOnly { get; set; }

        public GetFieldCaptionFunc GetFieldCaption { get; set; }

        // 字段名称注释的像素宽度
        public int CaptionPixelWidth { get; set; } = DefaultSplitterPixelWidth;

        // 字段名称的像素宽度
        public int NamePixelWidth { get; set; }

        // 指示符的像素宽度
        public int IndicatorPixelWidth { get; set; }

        // Name 区域可编辑部分的像素宽度
        // 注: NamePixelWidth 是 Name 区域包括边框空白部分在内的像素宽度
        public int NameTextWidth
        {
            get
            {
                return NamePixelWidth - BlankUnit * 2 - GapThickness - BorderThickness * 2;
            }
        }

        public int IndicatorTextWidth
        {
            get
            {
                return IndicatorPixelWidth - BlankUnit * 2 - GapThickness - BorderThickness * 2;
            }
        }

        // 基本的空白宽度
        public int BlankUnit { get; set; }

        // 边框厚度
        public int BorderThickness { get; set; }

        // Name 和 Indicator 和 Content 之间的缝隙厚度
        public int GapThickness { get; set; }

        // 字段内容区最小宽度。特别是在文档宽度随窗口 Client 区自动变化时
        public int MinFieldContentWidth { get; set; }

        // 内容的像素宽度
        // public int ContentPixelWidth { get; set; }

        public char FieldEndChar { get; set; } = (char)30; // 字段结束符

        public const char FieldEndCharDefault = (char)30; // 字段结束符
        public const char SubfieldCharDefault = (char)31; // 子字段符号
        public const char RecordEndCharDefault = (char)29;  // 记录结束符

        // 动态适应 client_width 的像素宽度
        public void Refresh(Font default_font)
        {
            var averageCharWidth = (int)FontContext.ComputeAverageWidth(default_font, out _);

            // var averageCharWidth = Line.GetAverageCharWidth();

            // 可编辑区域和边框之间的空白
            BlankUnit = averageCharWidth / 2;
            // 边框厚度
            BorderThickness = Math.Max(2, averageCharWidth / 6);
            // Name 和 Indicator 之间的缝隙
            GapThickness = Math.Max(2, averageCharWidth / 2);

            NamePixelWidth = BorderThickness + BlankUnit
                + averageCharWidth * 3
                + BlankUnit + this.BorderThickness
                + this.GapThickness;
            IndicatorPixelWidth = BorderThickness + BlankUnit
                + averageCharWidth * 2
                + BlankUnit + this.BorderThickness
                + this.GapThickness;
            // 字段内容最小宽度为 5 个平均字符宽度
            MinFieldContentWidth = averageCharWidth * 5;
            CaptionPixelWidth = averageCharWidth * 12;
            /*
            var maxCharWidth = Line.GetMaxCharWidth();
            BlankUnit = maxCharWidth / 6;
            NamePixelWidth = maxCharWidth * 3 + BlankUnit * 2; // 默认 60 像素宽度
            IndicatorPixelWidth = maxCharWidth * 2 + BlankUnit * 2; // 默认 30 像素宽度
            */
        }

        // 检测分割条和 Caption 区域
        // return:
        //      -2  Caption 区域
        //      -1  Splitter 区域
        //      0   其它区域(包括 name indicator 和 content 区域)
        public int TestSplitterArea(int x)
        {
            if (x < this.CaptionPixelWidth - this.SplitterPixelWidth)
            {
                return -2;
            }

            if (x < this.CaptionPixelWidth)
            {
                return -1;
            }

            return 0;
        }

        public static int DefaultSplitterPixelWidth = 8;

        public int SplitterPixelWidth = DefaultSplitterPixelWidth;

        public int CaptionX
        {
            get
            {
                return 0;
            }
        }

        public int NameX
        {
            get
            {
                return this.CaptionPixelWidth + this.BorderThickness + this.GapThickness + this.BorderThickness + this.BlankUnit; // 两侧都准备了空白
            }
        }

        public int NameBorderX
        {
            get
            {
                return this.CaptionPixelWidth + this.BorderThickness + this.GapThickness; // Name 边框左边还有空白，用于绘制 Solid 区的左边线
            }
        }


        public int IndicatorX
        {
            get
            {
                return this.NameX + this.NamePixelWidth;
            }
        }

        public int IndicatorBorderX
        {
            get
            {
                return this.NameBorderX + this.NamePixelWidth;
            }
        }

        public int ContentX
        {
            get
            {
                return this.IndicatorX + this.IndicatorPixelWidth;
            }
        }

        public int ContentBorderX
        {
            get
            {
                return this.IndicatorBorderX + this.IndicatorPixelWidth;
            }
        }

        public int SolidX
        {
            get
            {
                return this.CaptionPixelWidth;
            }
        }

        public int SolidPixelWidth
        {
            get
            {
                return this.ContentBorderX - this.CaptionPixelWidth;
            }
        }

        public bool DeltaCaptionWidth(int delta)
        {
            var old_value = this.CaptionPixelWidth;
            this.CaptionPixelWidth += delta;
            this.CaptionPixelWidth = Math.Max(this.SplitterPixelWidth, this.CaptionPixelWidth);

            return this.CaptionPixelWidth != old_value;
        }

        // 文字色
        public Color ForeColor { get; set; } = SystemColors.WindowText;

        // 窗口背景色
        public Color BackColor { get; set; } = SystemColors.Window;

        // 只读状态的文字色
        public Color ReadOnlyForeColor { get; set; } = SystemColors.ControlText;

        // 只读状态的背景色
        public Color ReadOnlyBackColor { get; set; } = SystemColors.Control;

        // 选择部分的文字色
        public Color HightlightForeColor { get; set; } = SystemColors.HighlightText;

        // 选择部分的背景色
        public Color HighlightBackColor { get; set; } = SystemColors.Highlight;

        // 分隔字符的文字色
        public Color DelimeterForeColor { get; set; } = DefaultDelimeterForeColor;

        public Color DelimeterBackColor { get; set; } = DefaultDelimeterBackColor;

        // 文字色
        public Color CaptionForeColor { get; set; } = DefaultCaptionForeColor;

        // 窗口背景色
        public Color CaptionBackColor { get; set; } = DefaultCaptionBackColor;

        public static Color DefaultCaptionBackColor = SystemColors.Info;
        public static Color DefaultCaptionForeColor = SystemColors.InfoText;

        public static Color DefaultDelimeterForeColor = Color.DarkGreen;    // Color.DarkRed;
        public static Color DefaultDelimeterBackColor = Color.WhiteSmoke; //  Color.LightYellow;

        // 不可编辑区域的背景色
        public Color SolidColor { get; set; } = DefaultSolidColor;

        public static Color DefaultSolidColor = Color.LightGray;

        public Color BorderColor { get; set; } = DefaultBorderColor;
        public static Color DefaultBorderColor = SystemColors.ControlDark;

        public Color FocusColor { get; set; } = DefaultFocuseColor;
        public static Color DefaultFocuseColor = SystemColors.MenuHighlight;

        public Color BlankCharForeColor { get; set; } = DefaultBlankCharForeColor;
        public static Color DefaultBlankCharForeColor = Color.Blue;

        #region 颜色主题

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

        public void UseColorTheme(string theme)
        {
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
        }

        public void SetTheme(Metrics metrics)
        {
            this.ForeColor = metrics.ForeColor;
            this.BackColor = metrics.BackColor;
            this.ReadOnlyForeColor = metrics.ReadOnlyForeColor;
            this.ReadOnlyBackColor = metrics.ReadOnlyBackColor;
            this.HightlightForeColor = metrics.HightlightForeColor;
            this.HighlightBackColor = metrics.HighlightBackColor;
            this.DelimeterForeColor = metrics.DelimeterForeColor;
            this.DelimeterBackColor = metrics.DelimeterBackColor;
            this.CaptionForeColor = metrics.CaptionForeColor;
            this.CaptionBackColor = metrics.CaptionBackColor;
            this.SolidColor = metrics.SolidColor;
            this.BorderColor = metrics.BorderColor;
            this.FocusColor = metrics.FocusColor;
            this.BlankCharForeColor = metrics.BlankCharForeColor;
        }

        public static Metrics ThemeDefault()
        {
            return new Metrics();
        }

        // 子字段符号和子字段名不要突出显示，和普通字符一样颜色
        public static Metrics ThemeSimple()
        {
            var metrics = new Metrics();
            metrics.DelimeterBackColor = Color.Transparent;
            metrics.DelimeterForeColor = metrics.ForeColor;
            metrics.CaptionForeColor = Color.FromArgb(160, 160, 160);
            metrics.CaptionBackColor = Color.FromArgb(250, 250, 250);
            metrics.SolidColor = /*Color.Transparent; //*/  Color.FromArgb(240, 240, 240);
            metrics.BorderColor = /*Color.Transparent; //*/  Color.FromArgb(220, 220, 220);
            return metrics;
        }

        // 极简
        public static Metrics ThemeSimplest()
        {
            var metrics = new Metrics();
            metrics.DelimeterBackColor = Color.Transparent;
            metrics.DelimeterForeColor = metrics.ForeColor;
            metrics.CaptionForeColor = Color.FromArgb(160, 160, 160);
            metrics.CaptionBackColor = Color.Transparent; // Color.FromArgb(250, 250, 250);
            metrics.SolidColor = Color.Transparent;
            metrics.BorderColor = Color.Transparent;
            return metrics;
        }

        // TODO: 高对比度

        public static Metrics ThemeDark()
        {
            var metrics = new Metrics();
            metrics.ForeColor = Color.FromArgb(230, 230, 230);
            metrics.BackColor = Color.Black;
            metrics.ReadOnlyForeColor = Color.Gray;
            metrics.ReadOnlyBackColor = Color.Black;
            metrics.HightlightForeColor = Color.Black;
            metrics.HighlightBackColor = Color.FromArgb(180, 180, 180);
            metrics.DelimeterForeColor = Color.White;
            metrics.DelimeterBackColor = Color.Transparent; //  Color.FromArgb(50, 50, 50);
            metrics.CaptionForeColor = Color.FromArgb(180, 180, 180);
            metrics.CaptionBackColor = Color.FromArgb(70, 70, 70);
            metrics.SolidColor = Color.FromArgb(60,60,60);
            metrics.BorderColor = Color.DarkGray;
            metrics.FocusColor = Color.DarkGray;
            metrics.BlankCharForeColor = Color.DarkGray;
            return metrics;
        }

        public static Metrics ThemeDarkBlue()
        {
            var metrics = new Metrics();
            metrics.ForeColor = Color.FromArgb(250, 250, 200);
            metrics.BackColor = Color.FromArgb(0, 0, 60);
            metrics.ReadOnlyForeColor = Color.Gray;
            metrics.ReadOnlyBackColor = Color.Black;
            metrics.HightlightForeColor = Color.White;
            metrics.HighlightBackColor = Color.FromArgb(70, 70, 200);
            metrics.DelimeterForeColor = Color.FromArgb(255, 230, 0);
            metrics.DelimeterBackColor = Color.FromArgb(0, 0, 0);
            metrics.CaptionForeColor = Color.FromArgb(160, 160, 180);
            metrics.CaptionBackColor = Color.FromArgb(70, 70, 100);
            metrics.SolidColor = Color.FromArgb(50, 50, 90);
            metrics.BorderColor = Color.DarkGray;
            metrics.FocusColor = Color.FromArgb(160, 160, 255);
            metrics.BlankCharForeColor = Color.DarkGray;
            return metrics;
        }

        public static Metrics ThemeSpring()
        {
            var metrics = new Metrics();
            metrics.ForeColor = Color.FromArgb(10, 10, 10);
            metrics.BackColor = Color.FromArgb(255, 255, 255);
            metrics.ReadOnlyForeColor = Color.Gray;
            metrics.ReadOnlyBackColor = Color.FromArgb(200, 200, 200);
            metrics.HightlightForeColor = Color.White;
            metrics.HighlightBackColor = Color.FromArgb(200, 0, 0);
            metrics.DelimeterForeColor = Color.FromArgb(30, 80, 0);
            metrics.DelimeterBackColor = Color.Transparent;
            /*
            metrics.CaptionForeColor = Color.FromArgb(190, 190, 210);
            metrics.CaptionBackColor = Color.FromArgb(90, 100, 90);
            */
            metrics.CaptionForeColor = Color.FromArgb(60, 220, 0);
            metrics.CaptionBackColor = Color.FromArgb(240, 255, 240);
            metrics.SolidColor = Color.FromArgb(150, 190, 150);
            metrics.BorderColor = Color.FromArgb(130, 170, 130);
            metrics.FocusColor = Color.FromArgb(160, 255, 160);
            metrics.BlankCharForeColor = Color.DarkGray;
            return metrics;
        }

        #endregion
    }

    public delegate string GetFieldCaptionFunc(MarcField field);

    public delegate bool GetReadOnlyFunc(IBox box);
}
