using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using Vanara.PInvoke;

using static Vanara.PInvoke.Usp10;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 上下文信息。编辑器控件通过它把一些委托函数传递给底层函数，以实现定制效果
    /// </summary>
    public class Context : IContext, IDisposable
    {
        /// <summary>
        /// 预先切割文字为分离的片段
        /// </summary>
        public SplitRangeFunc SplitRange { get; set; }

        /// <summary>
        /// 内部文字转换为显示文字
        /// </summary>
        public ConvertTextFunc ConvertText { get; set; }

        /// <summary>
        /// 动态计算文字前景色
        /// </summary>
        public GetForeColorFunc GetForeColor { get; set; }

        public GetBackColorFunc GetBackColor { get; set; }

        public PaintBackFunc PaintBack { get; set; }

        public GetFontFunc GetFont { get; set; }

        Hashtable _font_cache = new Hashtable();

        public IFontCacheItem GetFontCache(Font font)
        {
            if (_font_cache == null)
            {
                _font_cache = new Hashtable();
            }

            FontCacheItem item = null;
            if (_font_cache.Contains(font))
            {
                item = (FontCacheItem)_font_cache[font];
            }
            else
            {
                item = new FontCacheItem(font);

                if (_font_cache.Count > 1000)
                {
                    ClearFontCache();
                }

                _font_cache[font] = item;
            }

            return item;
        }

        public void ClearFontCache()
        {
            if (_font_cache == null)
            {
                return;
            }

            foreach (var key in _font_cache.Keys)
            {
                var item = (FontCacheItem)_font_cache[key];
                item?.Dispose();
            }
            _font_cache.Clear();
        }

        public void Dispose()
        {
            ClearFontCache();
        }
    }


    public class FontCacheItem : IFontCacheItem
    {
        Font _font;
        IntPtr _handle;
        SafeSCRIPT_CACHE _cache;
        FontMetrics _fontMetrics;

        public FontCacheItem(Font font)
        {
            _font = font;
            _handle = font.ToHfont();
            _cache = new SafeSCRIPT_CACHE();
        }

        public Font Font { get => _font; }
        public IntPtr Handle { get => _handle; }
        public SafeSCRIPT_CACHE Cache { get => _cache; }

        public IFontMetrics FontMetrics
        {
            get
            {
                if (_fontMetrics == null)
                {
                    _fontMetrics = new FontMetrics(_font);
                }

                return _fontMetrics;
            }
        }

        public void Dispose()
        {
            if (this._handle != IntPtr.Zero)
            {
                Gdi32.DeleteObject(this._handle);
            }
            this._cache?.Dispose();
        }
    }

    public class FontMetrics : IFontMetrics
    {
        public float _ascent;
        public float _descent;
        public float _spacing;

        public FontMetrics(Font font)
        {
            var fontFamily = font.FontFamily;
            var height = font.GetHeight();

            var ascent = fontFamily.GetCellAscent(font.Style);
            var descent = fontFamily.GetCellDescent(font.Style);
            var line_spacing = fontFamily.GetLineSpacing(font.Style);

            var em_height = line_spacing;
            var spacing = em_height - (ascent + descent);

            var up_height = height * ascent / em_height;
            var spacing_height = height * spacing / em_height;
            var below_height = height * descent / em_height;

            // Debug.WriteLine($"{fontFamily.Name} height={height} em_height={em_height} spacing={spacing} ascent={ascent} descent={descent} up_height={up_height} blow_height={below_height} spacing_height={spacing_height}");

            this._ascent = up_height;
            this._spacing = spacing_height;
            this._descent = below_height;
        }

        public float Ascent => _ascent;

        public float Descent => _descent;

        public float Spacing => _spacing;
    }



}
