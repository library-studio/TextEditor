using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    public class FontContext : IDisposable
    {
        // 当前字体的联结关系
        Link _fontLink = null;

        // 候选的字体列表
        List<Font> _fonts = new List<Font>();

        internal Font _default_font = null;
        internal int _line_height = 0;
        internal int _average_char_width = 0;
        internal int _max_char_width = 0;

        public FontContext(Font default_font)
        {
            InitialFonts(default_font);
        }

        public IEnumerable<Font> Fonts
        {
            get
            {
                return _fonts;
            }
        }

        public int GetLineHeight()
        {
            return _line_height;
        }

        public int GetAverageCharWidth()
        {
            return _average_char_width;
        }

        public int GetMaxCharWidth()
        {
            return _max_char_width;
        }

        // return:
        //      返回字体高度
        public int InitialFonts(Font default_font)
        {
            _default_font = default_font;
            _line_height = default_font.Height;
            var fontName = default_font.FontFamily.GetName(0);
            _fontLink = FontLink.GetLink(fontName, FontLink.FirstLink);

            Link.DisposeFonts(_fonts, _default_font);
            _fonts = _fontLink.BuildFonts(default_font);

            if (_average_char_width == 0)
            {
                _average_char_width = (int)ComputeAverageWidth(_default_font, out float max_value);
                _max_char_width = (int)max_value;

                DefaultReturnWidth = _average_char_width;
                DefaultFontHeight = _default_font.Height;
            }

            return _line_height;
        }

        // 全局使用
        public static int DefaultFontHeight = 0;
        public static int DefaultReturnWidth = 0;

        public void DisposeFonts()
        {
            // 注意不要释放 _default_font
            // 因为它是引用的外部对象，自然有管理它生存周期的地方。
            // 如果 FontContext 这里释放了，别的地方还在继续用就麻烦了
            Link.DisposeFonts(_fonts, _default_font);
        }

        public static float ComputeAverageWidth(Font font,
            out float maxCharWidth)
        {
            maxCharWidth = 0F;
            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))    // Graphics.FromHdc(dc.DangerousGetHandle())
            {
                {
                    // TODO: 循环测试一组可能的最宽字符
                    string sample = "中国人民";
                    SizeF size = g.MeasureString(sample, font);
                    maxCharWidth = size.Width / 4;
                }
                {
                    string sample = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    SizeF size = g.MeasureString(sample, font);
                    return size.Width / sample.Length;
                }
            }
        }

        public static float ComputeAverageWidth(SafeHDC dc, Font font)
        {
            using (var g = Graphics.FromHdc(dc.DangerousGetHandle()))
            {
                string sample = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                SizeF size = g.MeasureString(sample, font);
                return size.Width / sample.Length;
            }
        }

        public void Dispose()
        {
            DisposeFonts();
        }
    }
}
