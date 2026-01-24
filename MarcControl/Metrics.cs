using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Xml.Linq;

namespace LibraryStudio.Forms
{
    // 字段的一些共同参数
    public class Metrics : ColorTheme
    {
        public GetReadOnlyFunc GetReadOnly { get; set; }

        // public GetFieldCaptionFunc GetFieldCaption { get; set; }

        public GetStructureFunc GetStructure { get; set; }

        // 字段名称注释的像素宽度
        public int CaptionPixelWidth { get; set; } = DefaultSplitterPixelWidth + DefaultCaptionPixelWidth;

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

        // 展开/收缩按钮的宽度
        public int ButtonWidth { get; set; }

        // 字段内容区最小宽度。特别是在文档宽度随窗口 Client 区自动变化时
        public int MinFieldContentWidth { get; set; }

        // 内容的像素宽度
        // public int ContentPixelWidth { get; set; }

        public char FieldEndChar { get; set; } = (char)30; // 字段结束符

        public const char FieldEndCharDefault = (char)30; // 字段结束符
        public const char SubfieldCharDefault = (char)31; // 子字段符号
        public const char RecordEndCharDefault = (char)29;  // 记录结束符

        // 动态适应 client_width 的像素宽度
        public void Refresh(Font default_font,
            Font fixed_font = null)
        {
            var averageContentCharWidth = (int)FontContext.ComputeAverageWidth(default_font, out _);

            // 如果用了 fixed_font，则要考虑两者较大的一个
            var averageFixedCharWidth = fixed_font == null ?
                averageContentCharWidth :
                (int)FontContext.ComputeAverageWidth(fixed_font, out _);
            averageContentCharWidth = Math.Max(averageFixedCharWidth, averageContentCharWidth);

            var averageCharWidth = averageFixedCharWidth;

            // 可编辑区域和边框之间的空白
            BlankUnit = averageCharWidth / 2;
            // 边框厚度
            BorderThickness = Math.Max(2, averageCharWidth / 6);
            // Name 和 Indicator 之间的缝隙
            GapThickness = Math.Max(2, averageCharWidth / 2);

            ButtonWidth = averageCharWidth;

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

            // CaptionPixelWidth = averageCharWidth * 12;
        }

        // 检测分割条和 Caption 区域
        // return:
        //      -3  Button 区域
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

            if (x < this.CaptionPixelWidth + this.ButtonWidth)
            {
                return -3;
            }

            return 0;
        }

        public static int DefaultSplitterPixelWidth = 8;

        public static int DefaultCaptionPixelWidth = 100;

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
                // return this.CaptionPixelWidth + this.BorderThickness + this.GapThickness + this.BorderThickness + this.BlankUnit; // 两侧都准备了空白
                return NameBorderX + this.BorderThickness + this.BlankUnit;
            }
        }

        public int NameBorderX
        {
            get
            {
                return this.CaptionPixelWidth + this.BorderThickness + this.GapThickness
                    + this.ButtonWidth;
                // Name 边框左边还有空白，用于绘制 Solid 区的左边线
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

    }

    // public delegate string GetFieldCaptionFunc(MarcField field);

    public delegate bool GetReadOnlyFunc(IBox box);

    // 获得 box 下的结构信息
    public delegate UnitInfo GetStructureFunc(IBox parent, string name, int level);

    public class UnitInfo
    {
        public string Name { get; set; }
        public string Caption { get; set; }

        public UnitType Type { get; set; } = UnitType.Unkown;

        // 0 表示不确定长度
        public int Length { get; set; } = 0;

        // 下级单元
        public List<UnitInfo> SubUnits { get; set; } = new List<UnitInfo>();

        public static UnitInfo FromChars(
            UnitType type,
            string name,
            int[] length_list)
        {
            var result = new UnitInfo
            {
                Name = name,
                Caption = $"caption of {name}",
                Type = type
            };
            int offs = 0;
            foreach (var l in length_list)
            {
                result.SubUnits.Add(new UnitInfo
                {
                    Caption = $"({offs}/{l})",
                    Length = l,
                    Type = UnitType.Chars
                });
                offs += l;
            }

            return result;
        }

        public static UnitInfo FromSubfields(string name)
        {
            List<string> names = new List<string>();
            for (int i = 0; i < 26; i++)
            {
                names.Add($"{(char)((int)'a' + i)}");
            }
            var result = new UnitInfo { Name = name,
                Caption = $"caption of {name}",
                Type = UnitType.Field};
            {
                foreach (var n in names)
                {
                    result.SubUnits.Add(new UnitInfo
                    {
                        Caption = $"子字段 ${n}",
                        Name = n,
                        Length = 0,
                        Type = UnitType.Subfield
                    });
                }
            }

            return result;
        }

        public bool IsType(UnitType type)
        {
            if (SubUnits == null || SubUnits.Count == 0)
            {
                return false;
            }

            return SubUnits[0].Type == type;
        }

        public bool IsUnknown()
        {
            return IsType(UnitType.Unkown);
        }

        public bool IsChars()
        {
            return IsType(UnitType.Chars);
        }

        public bool IsField()
        {
            return IsType(UnitType.Field);
        }

        public bool IsSubfield()
        {
            return IsType(UnitType.Subfield);
        }
    }

    public enum UnitType
    {
        Unkown = 0,
        Field = 1,
        Subfield = 2,
        Chars = 3,
    }
}
