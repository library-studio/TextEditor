using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 上下文信息。编辑器控件通过它把一些委托函数传递给底层函数，以实现定制效果
    /// </summary>
    public class Context : IContext
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
    }
}
