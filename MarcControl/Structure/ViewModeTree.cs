using System;
using System.Collections.Generic;
using static Vanara.PInvoke.Gdi32;


namespace LibraryStudio.Forms
{
    /// <summary>
    /// 用于保存下级展开状态
    /// </summary>
    public class ViewModeTree
    {
        public ViewMode ViewMode { get; set; } = ViewMode.None;

        public IEnumerable<ViewModeTree> ChildViewModes { get; set; }

        // 用于调试
        public string Name { get; set; }
    }

    public interface IViewMode
    {
        ViewMode ViewMode { get; set; }

        ViewModeTree GetViewModeTree();

        ReplaceTextResult ReplaceText(
            ViewModeTree modeTree,
            IContext context,
            SafeHDC dc,
            int start,
            int end,
            string content,
            int pixel_width);
    }
}
