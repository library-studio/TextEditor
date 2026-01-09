using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using Vanara.PInvoke;
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

        #region 检查 SRIPT_ANALYSIS.eScript 是否适合一个字体显示

        // 字体名字 --> 字体特性 缓冲表
        static Hashtable _font_property_table = new Hashtable();

#if REMOVED
        public static IDictionary<string, int[]> GetDefaultScriptSamples()
        {
            var dict = new Dictionary<string, int[]>(StringComparer.OrdinalIgnoreCase)
            {
                // Basic Latin letters (A-Z, a-z)
                ["Latin (Basic)"] = Concat(Range(0x0041, 0x005A), Range(0x0061, 0x007A)),

                // Latin-1 Supplement (some common diacritics)
                ["Latin-1 Supplement (Common)"] = new[] { 0x00C0, 0x00E0, 0x00C9, 0x00E9, 0x00F1, 0x00FC }, // À à É é ñ ü

                // Cyrillic basic (U+0410..U+044F covers main Russian/Slavic letters)
                ["Cyrillic (Basic)"] = Range(0x0410, 0x044F),

                // Greek basic
                ["Greek (Basic)"] = Range(0x0391, 0x03C9),

                // Arabic: pick several frequent independent letters
                ["Arabic (Sample)"] = new[] { 0x0627, 0x0628, 0x062A, 0x062F, 0x0633, 0x0644 },

                // Hebrew
                ["Hebrew (Basic)"] = Range(0x05D0, 0x05EA),

                // Devanagari sample
                ["Devanagari (Sample)"] = new[] { 0x0905, 0x0915, 0x0930, 0x094D },

                // Hangul syllables (two samples)
                ["Hangul (Samples)"] = new[] { 0xAC00, 0xAC01 },

                // Hiragana / Katakana samples
                ["Hiragana (Samples)"] = new[] { 0x3042, 0x3044 },
                ["Katakana (Samples)"] = new[] { 0x30A2, 0x30A4 },

                // CJK Unified Ideographs - sample common characters
                ["CJK Unified Ideographs (Samples)"] = new[] { 0x4E00, 0x4E8C, 0x9FA5 },

                // Thai samples
                ["Thai (Samples)"] = new[] { 0x0E01, 0x0E2D },

                // Additional: Ukrainian-specific letters (to test "complete" support for Ukrainian)
                ["Ukrainian (Specific)"] = new[] { 0x0406, 0x0456, 0x0490, 0x0491, 0x0404, 0x0454, 0x0407, 0x0457 } // І/і Ґ/ґ Є/є Ї/ї
            };

            return dict;
        }
#endif

        // Helpers to build representative sample lists
        static int[] Range(int startInclusive, int endInclusive)
        {
            if (endInclusive < startInclusive) return Array.Empty<int>();
            var len = endInclusive - startInclusive + 1;
            var arr = new int[len];
            for (int i = 0; i < len; i++)
            {
                arr[i] = startInclusive + i;
            }

            return arr;
        }

        static int[] Concat(params int[][] arrays)
        {
            return arrays.SelectMany(a => a).ToArray();
        }

        // 用于检查基里尔字符集的样本 code point
        static int[] _cyrillic_sample = Range(0x0410, 0x044F);
        // 用于检查乌克兰文的样本 code point
        static int[] _Ukrainian_sample = new[] { 0x0406, 0x0456, 0x0490, 0x0491, 0x0404, 0x0454, 0x0407, 0x0457 };

        // 检查一个字体是否同时支持 Cyrillic 和 Ukrainian
        // return:
        //      0   两者都不支持
        //      1   只支持一种
        //      2   两种都支持
        public static int CheckUkrainianSupporting(Font font)
        {
            var ret1 = CheckSamples(font, _cyrillic_sample);
            var ret2 = CheckSamples(font, _Ukrainian_sample);
            if (ret1 == true && ret2 == true)
                return 2;
            if (ret1 == false && ret2 == false)
                return 0;
            return 1;
        }

        // 检查一个字体是否被某个样本 codepoint 序列支持
        public static bool CheckSamples(Font font, int[] samples)
        {
            var ranges = GetFontUnicodeRanges(font);

            //var missing = new List<int>();
            foreach (int cp in samples)
            {
                if (!IsCodepointSupportedByRanges(ranges, cp))
                {
                    //missing.Add(cp);
                    return false;
                }
            }


            return true;
        }

        // Get the Unicode ranges supported by the given font (Windows/GDI only)
        public static List<(int Low, int High)> GetFontUnicodeRanges(Font font)
        {
            if (font == null) throw new ArgumentNullException(nameof(font));

            using (var g = Graphics.FromHwnd(IntPtr.Zero))
            {
                IntPtr hdc = g.GetHdc();
                IntPtr hFont = IntPtr.Zero;
                HGDIOBJ oldObj = IntPtr.Zero;
                try
                {
                    hFont = font.ToHfont();
                    oldObj = SelectObject((HDC)hdc, (HGDIOBJ)hFont);

                    uint needed = Gdi32.GetFontUnicodeRanges(hdc, IntPtr.Zero);
                    if (needed == 0)
                    {
                        return new List<(int, int)>();
                    }

                    IntPtr buffer = Marshal.AllocHGlobal((int)needed);
                    try
                    {
                        uint got = Gdi32.GetFontUnicodeRanges(hdc, buffer);
                        if (got == 0)
                            throw new InvalidOperationException("GetFontUnicodeRanges failed on second call.");

                        GLYPHSET gs = Marshal.PtrToStructure<GLYPHSET>(buffer);
                        int headerSize = Marshal.SizeOf<GLYPHSET>();
                        IntPtr rangesPtr = IntPtr.Add(buffer, headerSize);

                        var list = new List<(int, int)>();
                        int wcrangeSize = Marshal.SizeOf<WCRANGE>();
                        for (int i = 0; i < gs.cRanges; i++)
                        {
                            IntPtr thisRangePtr = IntPtr.Add(rangesPtr, i * wcrangeSize);
                            WCRANGE wr = Marshal.PtrToStructure<WCRANGE>(thisRangePtr);
                            list.Add((wr.wcLow, wr.wcLow + wr.cGlyphs));
                        }
                        return list;
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(buffer);
                    }
                }
                finally
                {
                    if (oldObj != IntPtr.Zero)
                        SelectObject(hdc, oldObj);
                    if (hFont != IntPtr.Zero)
                        DeleteObject(hFont);
                    g.ReleaseHdc(hdc);
                }
            }
        }


        // Check whether a single code point is supported (search ranges)
        public static bool IsCodepointSupportedByRanges(List<(int Low, int High)> ranges, int codepoint)
        {
            foreach (var r in ranges)
            {
                if (codepoint >= r.Low && codepoint <= r.High) return true;
            }
            return false;
        }

        // 检查一个字体对某些特殊 script 是否支持。如果不支持，就要跳过这种字体，用后面的其它字体
        public static bool CheckSupporting(Font font, int script)
        {
            if (script == 8)
            {
                var prop = _font_property_table[font.Name] as FontProperty;
                if (prop == null)
                {
                    var ret = CheckUkrainianSupporting(font);
                    prop = new FontProperty(font.Name, script, ret == 2);
                    // 防止尺寸失控
                    // (系统中用了超过两千种字体？)
                    if (_font_property_table.Count > 2000)
                    {
#if DEBUG
                        throw new ArgumentException("_font_property_table 耗用空间失控，超过 2000 个元素");
#else
                        _font_property_table.Clear();
#endif
                    }
                    _font_property_table[font.Name] = prop;
                }

                return prop.CanUse(script);
            }

            return true;
        }

        // 字体属性。目前用于表达某些特殊 script 不允许使用的信息
        class FontProperty
        {
            int[] _disallow_scripts = new int[0];
            readonly string _font_name;

            public FontProperty(string font_name)
            {
                _font_name = font_name;
            }

            public FontProperty(string font_name, int script, bool can_use)
            {
                _font_name = font_name;
                if (can_use == false)
                    SetCant(script);
            }

            public bool CanUse(int script)
            {
                if (_disallow_scripts == null)
                    return true;
                return !_disallow_scripts.Contains(script);
            }

            public void SetCant(int script)
            {
                if (_disallow_scripts.Where(i => i == script).Any())
                    return;
                var list = new List<int>(_disallow_scripts);

                // 防止尺寸失控
                if (list.Count > 1000)
                {
#if DEBUG
                    throw new ArgumentException("_disallow_scripts 耗用空间失控");
#else
                    list.Clear();
#endif
                }

                list.Add(script);
                _disallow_scripts = list.ToArray();
            }
        }

#endregion
    }
}
