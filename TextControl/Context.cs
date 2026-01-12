using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

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

        public IntPtr GetFontHandle(Font font)
        {
            if (_font_cache == null)
            {
                _font_cache = new Hashtable();
            }

            var handle = IntPtr.Zero;
            if (_font_cache.Contains(font))
            {
                handle = (IntPtr)_font_cache[font];
            }
            else
            {
                handle = font.ToHfont();
                if (_font_cache.Count > 1000)
                {
                    _font_cache.Clear();
                }

                _font_cache[font] = handle;
            }

            return handle;
        }

        public void ClearFontCache()
        {
            if (_font_cache == null)
            {
                return;
            }

            foreach (var key in _font_cache.Keys)
            {
                var handle = (IntPtr)_font_cache[key];
                if (handle != IntPtr.Zero)
                {
                    Gdi32.DeleteObject(handle);
                }
            }
            _font_cache.Clear();
        }

        public void Dispose()
        {
            ClearFontCache();
        }
    }
}
