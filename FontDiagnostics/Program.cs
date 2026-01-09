using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// 验证如何用 Windows API GetGlyphIndices() 获得一个字体中具体的 glyph
/// </summary>
class FontUnicodeDiagnostics
{
    // P/Invoke
    [DllImport("gdi32.dll")]
    static extern uint GetFontUnicodeRanges(IntPtr hdc, IntPtr lpgs);

    [DllImport("gdi32.dll", SetLastError = true)]
    static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll", SetLastError = true)]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    static extern int GetTextFace(IntPtr hdc, int nCount, StringBuilder lpFaceName);

    // GetGlyphIndicesW: maps Unicode characters to glyph indices for the currently selected font
    [DllImport("gdi32.dll", CharSet = CharSet.Unicode)]
    static extern uint GetGlyphIndices(IntPtr hdc, string lpstr, int c, [Out] ushort[] pgi, uint fl);

    [StructLayout(LayoutKind.Sequential)]
    struct GLYPHSET
    {
        public uint cjThis;
        public uint flAccel;
        public uint cGlyphsSupported;
        public uint cRanges;
        // WCRANGE[] follows
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WCRANGE
    {
        public ushort wcLow;
        public ushort wcHigh;
    }

    public static List<(int Low, int High)> GetFontUnicodeRanges(Font font)
    {
        if (font == null) throw new ArgumentNullException(nameof(font));
        using (var g = Graphics.FromHwnd(IntPtr.Zero))
        {
            IntPtr hdc = g.GetHdc();
            IntPtr hFont = IntPtr.Zero, oldObj = IntPtr.Zero;
            try
            {
                hFont = font.ToHfont();
                oldObj = SelectObject(hdc, hFont);

                uint needed = GetFontUnicodeRanges(hdc, IntPtr.Zero);
                if (needed == 0) return new List<(int, int)>();

                IntPtr buffer = Marshal.AllocHGlobal((int)needed);
                try
                {
                    uint got = GetFontUnicodeRanges(hdc, buffer);
                    if (got == 0) throw new InvalidOperationException("GetFontUnicodeRanges failed.");

                    GLYPHSET gs = Marshal.PtrToStructure<GLYPHSET>(buffer);
                    int headerSize = Marshal.SizeOf<GLYPHSET>();
                    IntPtr rangesPtr = IntPtr.Add(buffer, headerSize);
                    var res = new List<(int, int)>();
                    int wcrangeSize = Marshal.SizeOf<WCRANGE>();
                    for (int i = 0; i < gs.cRanges; i++)
                    {
                        IntPtr thisRangePtr = IntPtr.Add(rangesPtr, i * wcrangeSize);
                        WCRANGE wr = Marshal.PtrToStructure<WCRANGE>(thisRangePtr);
                        res.Add((wr.wcLow, wr.wcLow + wr.wcHigh));
                    }
                    return res;
                }
                finally
                {
                    Marshal.FreeHGlobal(buffer);
                }
            }
            finally
            {
                if (oldObj != IntPtr.Zero) SelectObject(hdc, oldObj);
                if (hFont != IntPtr.Zero) DeleteObject(hFont);
                g.ReleaseHdc(hdc);
            }
        }
    }

    static bool IsCodepointInRanges(List<(int Low, int High)> ranges, int cp)
    {
        foreach (var r in ranges)
            if (cp >= r.Low && cp <= r.High) return true;
        return false;
    }

    // Diagnostic: print selected face name in HDC, ranges, and glyph indices for test chars
    public static void DiagnoseFont(string fontName, float size = 12f)
    {
        using (var font = new Font(fontName, size))
        using (var g = Graphics.FromHwnd(IntPtr.Zero))
        {
            IntPtr hdc = g.GetHdc();
            IntPtr hFont = IntPtr.Zero, oldObj = IntPtr.Zero;
            try
            {
                hFont = font.ToHfont();
                oldObj = SelectObject(hdc, hFont);

                // GetTextFace: find actual face selected into HDC
                var sb = new StringBuilder(64);
                int chars = GetTextFace(hdc, sb.Capacity, sb);
                string selectedFace = sb.ToString();
                Console.WriteLine($"Requested font: '{fontName}' size {size}");
                Console.WriteLine($"Face selected into HDC (GetTextFace): '{selectedFace}' (GetTextFace returned {chars})");

                // Get ranges
                var ranges = GetFontUnicodeRanges(font);
                Console.WriteLine($"\nNumber of Unicode ranges reported: {ranges.Count}");
                foreach (var r in ranges)
                {
                    Console.WriteLine($" U+{r.Low:X4} .. U+{r.High:X4}");
                }

                // Test some codepoints
                int[] testCps = new[] { 0x0061, 0x0041, 0x03B1, 0x0410, 0x4E00 }; // 'a','A','α','А','一'
                Console.WriteLine("\nCodepoint existence by ranges and glyph index:");
                foreach (int cp in testCps)
                {
                    bool inRanges = IsCodepointInRanges(ranges, cp);
                    string ch = Char.IsControl((char)cp) ? " " : new string((char)cp, 1);
                    Console.WriteLine($"U+{cp:X4} '{ch}': InRanges={inRanges}");

                    // GetGlyphIndices: maps characters to glyph indices for the selected HFONT
                    ushort[] outGlyphs = new ushort[1];
                    uint res = GetGlyphIndices(hdc, char.ConvertFromUtf32(cp), 1, outGlyphs, 0);
                    // According to MSDN: returns GDI_ERROR (0xFFFFFFFF) on failure. outGlyphs contains glyph indices; 0xFFFF (65535) may indicate missing.
                    bool glyphAvailable = true;
                    if (res == 0xFFFFFFFF) glyphAvailable = false;
                    else
                    {
                        // If glyph index equals 0xFFFF, treat as missing per common practice.
                        if (outGlyphs[0] == 0xFFFF) glyphAvailable = false;
                        // Some fonts may map missing glyph to 0 (glyph 0 usually .notdef); treat 0 as missing as well.
                        if (outGlyphs[0] == 0) glyphAvailable = false;
                    }
                    Console.WriteLine($" GetGlyphIndices -> res={res}, glyphIndex={(res == 0xFFFFFFFF ? "GDI_ERROR" : outGlyphs[0].ToString())}, GlyphAvailable={glyphAvailable}");
                }
            }
            finally
            {
                if (oldObj != IntPtr.Zero) SelectObject(hdc, oldObj);
                if (hFont != IntPtr.Zero) DeleteObject(hFont);
                g.ReleaseHdc(hdc);
            }
        }
    }

    static void Main(string[] args)
    {
        string fontName = args.Length > 0 ? args[0] : "Arial";
        try
        {
            DiagnoseFont(fontName, 12f);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex);
        }
    }
}
