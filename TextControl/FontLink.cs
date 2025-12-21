using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

using Microsoft.Win32;

namespace LibraryStudio.Forms
{
    public static class FontLink
    {
        static List<Link> _links = new List<Link>();

        public static void Initialize()
        {
            // 计算机\HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\FontLink\SystemLink
            RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Microsoft\\Windows NT\\CurrentVersion\\FontLink\\SystemLink", false);
            if (key == null)
                throw new Exception("FontLink registry key not found. Please ensure the application is running with sufficient privileges to access the registry.");

            foreach (var name in key.GetValueNames())
            {
                // TODO: throw exception?
                if (key.GetValueKind(name) != RegistryValueKind.MultiString)
                    continue; // Skip non-string values 

                var lines = (string[])key.GetValue(name, "");
                /*
    SIMSUN.TTC,SimSun
    MINGLIU.TTC,PMingLiU
    MSGOTHIC.TTC,MS UI Gothic
    BATANG.TTC,Batang
    MSYH.TTC,Microsoft YaHei UI
    MSJH.TTC,Microsoft JhengHei UI
    YUGOTHM.TTC,Yu Gothic UI
    MALGUN.TTF,Malgun Gothic
    SEGUISYM.TTF,Segoe UI Symbol
     * 
     * */
                var link = new Link { SourceFont = name };
                if (link.FontInfos == null)
                    link.FontInfos = new List<FontInfo>();
                link.FontInfos.Add(new FontInfo
                {
                    FontFileName = "",
                    FontName = "Arial Unicode MS"   // "Arial Unicode MS",
                });

                Hashtable name_table = new Hashtable(); // 名字去重
                foreach (var line in lines)
                {
                    var parts = line.Split(',');
                    if (parts.Length < 2)
                        continue; // Skip invalid lines

                    var fontName = parts[1].Trim();
                    if (name_table.ContainsKey(fontName))
                        continue;
                    name_table.Add(fontName, "");

                    if (link.FontInfos == null)
                        link.FontInfos = new List<FontInfo>();
                    link.FontInfos.Add(new FontInfo
                    {
                        FontFileName = parts[0].Trim(),
                        FontName = fontName,
                    });
                }

                link.FontInfos.Add(new FontInfo
                {
                    FontFileName = "",
                    FontName = "Aldhabi"   // "Arial Unicode MS",
                });

                _links.Add(link);
            }
        }

        public static string GetLocalName(string fontName)
        {
            FontFamily fontFamily = new FontFamily("SimSun");
            return fontFamily.GetName(CultureInfo.CurrentCulture.LCID);
        }


        // 找到一个联结关系。可以从一个字体名，找到和它关联的多个字体的名字
        static public Link GetLink(string fontName,
            Link default_link)
        {
            var names = FontNameMapping.GetIdenticalFontNames(fontName);
            if (_links.Count == 0)
                Initialize();
            foreach (var name in names)
            {
                var link = _links.FirstOrDefault(l => l.SourceFont.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (link != null)
                    return link;
            }
            return default_link;
            // return _links.FirstOrDefault(link => link.SourceFont.Equals(fontName, StringComparison.OrdinalIgnoreCase));
        }

        static public Link FirstLink
        {
            get
            {
                if (_links.Count == 0)
                    Initialize();
                return _links.FirstOrDefault();
            }
        }

#if REMOVED
        // 获取所有已知的 LCID（区域性标识符）
        static int[] GetAllLcid()
        {
            return CultureInfo.GetCultures(CultureTypes.AllCultures)
                .Select(c => c.LCID)
                .Distinct()
                .ToArray();
        }

        public static int GetLCID()
        {
            FontFamily fontFamily = new FontFamily("SimSun");
            foreach (var lcid in GetAllLcid())
            {
                if (lcid == 0)
                    continue; // Skip the invariant culture
                var name = fontFamily.GetName(lcid);
                if (name == "Sim Sun")
                    return lcid;
            }
            return 0;
        }

        public static bool AreFontNamesEquivalent(string name1, string name2)
        {
            foreach (var fontFamily in FontFamily.Families)
            {
                var names = CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .Select(c => {
                        try { return fontFamily.GetName(c.LCID); }
                        catch { return null; }
                    })
                    .Where(n => !string.IsNullOrEmpty(n))
                    .ToList();

                if (names.Contains(name1) && names.Contains(name2))
                    return true;
            }
            return false;
        }
#endif

    }

    // 构建一个等同字体的映射表。可以从一个字体名，找到和它等同的其它名字
    public static class FontNameMapping
    {
        static List<List<string>> _map = new List<List<string>>();
        public static void Initialize()
        {
            _map.Clear();
            foreach (var fontFamily in FontFamily.Families)
            {
                var names = CultureInfo.GetCultures(CultureTypes.AllCultures)
                    .Select(c =>
                    {
                        try { return fontFamily.GetName(c.LCID); }
                        catch { return null; }
                    })
                    .Where(n => !string.IsNullOrEmpty(n))
                    .Distinct().ToList();
                _map.Add(names);
            }
        }

        public static List<string> GetIdenticalFontNames(string fontName)
        {
            if (_map.Count == 0)
                Initialize();
            // TODO: 可以考虑用 Hashtable 来加速查找
            foreach (var names in _map)
            {
                if (names.Contains(fontName))
                {
                    return names;
                }
            }
            return new List<string>();  // not found
        }
    }

    /*
CultureInfo.LCID
    * */

    // 一个字体信息
    public class FontInfo
    {
        public string FontName { get; set; }

        public string FontFileName { get; set; }
    }

    // 一个联结关系。可以从一个字体名，找到和它关联的多个字体的名字
    public class Link
    {
        public string SourceFont { get; set; }
        public List<FontInfo> FontInfos { get; set; }

        public List<Font> BuildFonts(Font refFont)
        {
            if (FontInfos == null)
                throw new ArgumentException(".FontInfos == null");
            var fonts = new List<Font>() { refFont };
            // TODO: 可以通过调节 fonts 中各种字体出现的先后顺序，
            // 实现类似令英文字符先用上一个非汉字的字体的效果
            Hashtable name_table = new Hashtable();
            name_table.Add(refFont.FontFamily.GetName(0), ""); // 添加原字体的名字，避免重复
            foreach (var info in FontInfos)
            {
                try
                {
                    // Debug.WriteLine(info.FontName);
                    var fontFamily = new FontFamily(info.FontName);
                    if (name_table.Contains(fontFamily.GetName(0)))
                        continue;
                    fonts.Add(new Font(fontFamily, refFont.SizeInPoints, refFont.Style, GraphicsUnit.Point));
                    name_table.Add(fontFamily.GetName(0), "");
                }
                catch (ArgumentException)
                {
                    continue;
                }
            }

            /*
            fonts.Sort((a, b) => {
                // Aldhabi 放到最后
                if (a.Name.StartsWith("Microsoft Sans Serif"))
                    return -1;
                if (b.Name.StartsWith("Microsoft Sans Serif"))
                    return 1;
                return string.CompareOrdinal(a.Name, b.Name);
            });
            */
            return fonts;
        }

        // parameters:
        //      fonts   要释放的对象
        //      exclude fonts 中特意不要释放的对象
        public static void DisposeFonts(List<Font> fonts,
            Font exclude = null)
        {
            if (fonts != null)
            {
                foreach (var font in fonts)
                {
                    if (font != exclude)
                        font.Dispose();
                }

                fonts.Clear();
            }
        }
    }
}
