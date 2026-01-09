using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

/// <summary>
/// 验证如何获得字体包含的 code point
/// </summary>
class FontChecker
{
    // P/Invoke for GetFontUnicodeRanges
    [DllImport("gdi32.dll")]
    static extern uint GetFontUnicodeRanges(IntPtr hdc, IntPtr lpgs);

    [DllImport("gdi32.dll", SetLastError = true)]
    static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll", SetLastError = true)]
    static extern bool DeleteObject(IntPtr hObject);

    // GLYPHSET header (variable length, WCRANGE[] follows)
    [StructLayout(LayoutKind.Sequential)]
    struct GLYPHSET
    {
        public uint cjThis;
        public uint flAccel;
        public uint cGlyphsSupported;
        public uint cRanges;
        // followed by WCRANGE array
    }

    [StructLayout(LayoutKind.Sequential)]
    struct WCRANGE
    {
        public ushort wcLow; // inclusive
        public ushort wcHigh; // inclusive
    }

    // Get the Unicode ranges supported by the given font (Windows/GDI only)
    public static List<(int Low, int High)> GetFontUnicodeRanges(Font font)
    {
        if (font == null) throw new ArgumentNullException(nameof(font));

        using (var g = Graphics.FromHwnd(IntPtr.Zero))
        {
            IntPtr hdc = g.GetHdc();
            IntPtr hFont = IntPtr.Zero;
            IntPtr oldObj = IntPtr.Zero;
            try
            {
                hFont = font.ToHfont();
                oldObj = SelectObject(hdc, hFont);

                uint needed = GetFontUnicodeRanges(hdc, IntPtr.Zero);
                if (needed == 0)
                {
                    return new List<(int, int)>();
                }

                IntPtr buffer = Marshal.AllocHGlobal((int)needed);
                try
                {
                    uint got = GetFontUnicodeRanges(hdc, buffer);
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
                        list.Add((wr.wcLow, wr.wcLow + wr.wcHigh));
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

    // High-level: check sets of representative codepoints per "script" and report completeness
    public static IDictionary<string, ScriptCheckResult> CheckFontAgainstScriptSamples(Font font, IDictionary<string, int[]> scriptSamples)
    {
        var ranges = GetFontUnicodeRanges(font);
        var results = new Dictionary<string, ScriptCheckResult>(StringComparer.OrdinalIgnoreCase);

        foreach (var kv in scriptSamples)
        {
            string script = kv.Key;
            int[] samples = kv.Value ?? Array.Empty<int>();
            var missing = new List<int>();
            foreach (int cp in samples)
            {
                if (!IsCodepointSupportedByRanges(ranges, cp))
                    missing.Add(cp);
            }
            results[script] = new ScriptCheckResult
            {
                ScriptName = script,
                TotalSampled = samples.Length,
                Missing = missing.ToArray()
            };
        }

        return results;
    }

    public class ScriptCheckResult
    {
        public string ScriptName;
        public int TotalSampled;
        public int[] Missing;
        public bool IsComplete => Missing == null || Missing.Length == 0;
    }

    // Helpers to build representative sample lists
    static int[] Range(int startInclusive, int endInclusive)
    {
        if (endInclusive < startInclusive) return Array.Empty<int>();
        var len = endInclusive - startInclusive + 1;
        var arr = new int[len];
        for (int i = 0; i < len; i++) arr[i] = startInclusive + i;
        return arr;
    }

    static int[] Concat(params int[][] arrays)
    {
        return arrays.SelectMany(a => a).ToArray();
    }

    // Default representative samples for several scripts.
    // These are not exhaustive; you can extend the arrays for stricter checks.
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

    static void PrintReport(Font font, IDictionary<string, ScriptCheckResult> report)
    {
        Console.WriteLine($"Font: {font.Name} ({font.Size}pt)");
        Console.WriteLine(new string('-', 60));
        foreach (var kv in report)
        {
            var r = kv.Value;
            Console.WriteLine($"Script: {r.ScriptName}");
            Console.WriteLine($" Sampled codepoints: {r.TotalSampled}");
            Console.WriteLine($" Complete: {(r.IsComplete ? "Yes" : "No")}");
            if (!r.IsComplete)
            {
                Console.WriteLine($" Missing ({r.Missing.Length}):");
                // print each missing as "U+XXXX 'ch'" where printable
                var sb = new StringBuilder();
                foreach (var cp in r.Missing)
                {
                    string ch = Char.IsSurrogate((char)cp) ? "?" : new string((char)cp, 1);
                    sb.AppendLine($" U+{cp:X4} '{ch}'");
                }
                Console.Write(sb.ToString());
            }
            Console.WriteLine();
        }
    }

    static void Main(string[] args)
    {
        // Usage:
        // FontUnicodeSupportChecker.exe [FontName]
        // If no font name supplied, prompt user to enter or use "Arial".
        string fontName;
        if (args.Length >= 1 && !string.IsNullOrWhiteSpace(args[0]))
            fontName = args[0];
        else
        {
            Console.Write("Enter font name (or press Enter to use 'Arial'): ");
            fontName = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(fontName)) fontName = "Arial";
        }

        try
        {
            using (var font = new Font(fontName, 12f))
            {
                var samples = GetDefaultScriptSamples();
                var results = CheckFontAgainstScriptSamples(font, samples);
                PrintReport(font, results);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            Console.WriteLine("Make sure the program runs on Windows and the specified font name exists.");
        }

        Console.WriteLine("Done.");
    }
}
