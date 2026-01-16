using System;
using System.Collections.Generic;
using System.Drawing;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Usp10;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 盒子接口。
    /// 所谓盒子，是一种矩形的，可以独立实现编辑效果的显示区域
    /// </summary>
    public interface IBox
    {
        string Name { get; set; }

        // 父节点
        IBox Parent { get; }

        // 清除
        void Clear();

        // 获得像素高度
        int GetPixelHeight(/*int line_height*/);

        // 获得像素宽度
        int GetPixelWidth();

        // 文本长度

        int TextLength { get; }

        // 获得文本
        string MergeText(int start = 0, int end = int.MaxValue);

        // 替换一段或者全部文字
        ReplaceTextResult ReplaceText(
            IContext context,
            SafeHDC dc,
            int start,
            int end,
            string content,
            int pixel_width/*,
            out string replaced,
            out Rectangle update_rect,
            out Rectangle scroll_rect,
            out int scroll_distance*/);




        // 向下移动插入符一行
        // parameters:
        //      x   起点 x 位置。这是(调主负责保存的)最近一次左右移动插入符之后，插入符的 x 位置。注意，并不一定等于当前插入符的 x 位置
        //      y   当前插入符的 y 位置
        //      info    [in,out]插入符位置参数。
        //              注: [in] 时，info.X info.Y 被利用，其它成员没有被利用
        // return:
        //      true    成功。新的插入符位置返回在 info 中了
        //      false   无法移动。注意此时 info 中返回的内容无意义
        bool CaretMoveDown(
            int x,
            int y,
            out HitInfo info);

        // 向上移动插入符一行
        // parameters:
        //      x   起点 x 位置。这是(调主负责保存的)最近一次左右移动插入符之后，插入符的 x 位置。注意，并不一定等于当前插入符的 x 位置
        //      y   当前插入符的 y 位置
        //      info    [in,out]插入符位置参数。
        //              注: [in] 时，info.X info.Y 被利用，其它成员没有被利用
        // return:
        //      true    成功。新的插入符位置返回在 info 中了
        //      false   无法移动。注意此时 info 中返回的内容无意义
        bool CaretMoveUp(
            int x,
            int y,
            out HitInfo info);

        /*
        // 根据 info 对象的 .ChildIndex  和 .TextIndex 计算出全局偏移量
        int GetGlobalOffs(HitInfo info);
        */

        // 以全局偏移量为参数，获得点击位置
        // HitInfo HitByGlobalOffs(int offs_param, bool trailing);

        // 点击测试
        // 利用 x y 进行 HitTest()
        HitInfo HitTest(int x,
            int y);

        // 探测 Caret Offs 位置。相当于利用 offs 进行的 HitTest()
        // 利用 direction 参数可以获得 Caret Offs 进行移动的信息
        // 注: 偏移位置中有一些位置是具有多个可用位置的情况。比如 Paragraph 中
        // 下一行的开头等同于上一行的末尾，为了获得其中期望的一个可用位置，可善用
        // direction 参数值，direction
        // 小于 0 表示这是从后向前移动，如果遇到后方可用的位置优先使用后方的；direction
        // 大于 0 表表示这是从前向后的移动，如果遇到靠前的可用位置优先使用靠前的。
        // 而如果 direction 为零，则无法表达取舍倾向性。比如 offs:1 direction:0。如果确有倾向性要求，
        // 以倾向靠后的可用位置为例，上例可以改为以 offs:2 direction:-1 调用。
        // parameters:
        //      offs    插入符在当前对象中的偏移
        //      direction   -1 向左 0 原地 1 向右
        // return:
        //      -1  越过左边
        //      0   成功
        //      1   越过右边
        int MoveByOffs(int offs,
            int direction,
            out HitInfo info);


        // 获得一段文本显示范围的 Region
        // parameters:
        //      virtual_tail_length 行末虚拟尾部字符个数。如果这个尾部处在选择范围，需要包含代表它的一块矩形
        // return:
        //      null    空区域
        //      其它      可用 Region
        Region GetRegion(int start_offs = 0,
            int end_offs = int.MaxValue,
            int virtual_tail_length = 0);


        // 绘制
        void Paint(
            IContext context,
            SafeHDC dc,
            int x,
            int y,
            Rectangle clipRect,
            int blockOffs1,
            int blockOffs2,
            int virtual_tail_length);

        void ClearCache();

        /*
        // 安放位置
        // 处理基线对齐等等任务。处理前后，Pixel Height 会有变化
        int Placement();
        */

        float BaseLine { get; }
        float Below { get; }
    }

    public class ReplaceTextResult
    {
        // 修改涉及的内容的最大像素宽度
        // 如果为 0 表示不清楚最大宽度。此时需要调主自行用 GetPixelWidth() 探测
        public int MaxPixel { get; set; }

        // 被替换部分的原有文字
        public string ReplacedText { get; set; } = "";

        // 实际使用的 新文本。可能和参数相比有所变化，比如末尾添加了结束符
        // 如果为 null 表示不适用此成员，依调用参数
        public string NewText { get; set; }

        // 修改涉及到的更新区域(第二阶段全部更新)
        public Rectangle UpdateRect { get; set; } = System.Drawing.Rectangle.Empty;

        // 卷滚区域
        public Rectangle ScrollRect { get; set; } = System.Drawing.Rectangle.Empty;

        // 卷滚距离
        public int ScrolledDistance { get; set; }

        // 2025/12/4
        // 第一阶段快速更新的区域
        public Rectangle SmallUpdateRect { get; set; } = System.Drawing.Rectangle.Empty;
    }

    /// <summary>
    /// 用于定制效果的上下文接口
    /// </summary>
    public interface IContext : IDisposable
    {
        /// <summary>
        /// 预先切割文字为分离的片段
        /// </summary>
        SplitRangeFunc SplitRange { get; set; }

        /// <summary>
        /// 内部文字转换为显示文字
        /// </summary>
        ConvertTextFunc ConvertText { get; set; }

        /// <summary>
        /// 动态计算文字前景色
        /// </summary>
        GetForeColorFunc GetForeColor { get; set; }

        /// <summary>
        /// 动态计算背景色
        /// </summary>
        GetBackColorFunc GetBackColor { get; set; }

        /// <summary>
        /// 动态绘制边框和背景
        /// </summary>
        PaintBackFunc PaintBack { get; set; }

        // 获得可用字体集合
        GetFontFunc GetFont { get; set; }

        IFontCacheItem GetFontCache(Font font);

        void ClearFontCache();
    }

    public interface IFontCacheItem : IDisposable
    {
        Font Font { get; }
        IntPtr Handle { get; }
        SafeSCRIPT_CACHE Cache { get; }
        IFontMetrics FontMetrics { get; }
    }

    public interface IFontMetrics
    {
        float Ascent { get; }
        float Descent { get; }
        float Spacing { get; }
    }

    // 用于预先切割文字为更小段落的函数
    // 用法: 注意这个函数产生的 string [] 应该和原有的 text 不增减字符，不改变总字符数。
    // 而如果要增删字符内容，则需要考虑在调用 Initialize() 函数之前先行处理好
    public delegate Segment[] SplitRangeFunc(IBox box, string text);

    public struct Segment
    {
        public string Text;
        public object Tag;
    }

    // 将内部文字转换为显示文字的委托
    public delegate string ConvertTextFunc(string text);

    // 为每个 Range 对象动态计算出前景色
    public delegate Color GetForeColorFunc(object box, bool highlight);

    // 为每个 Range 对象动态计算出背景色
    public delegate Color GetBackColorFunc(object box, bool highlight);

    // 绘制背景
    public delegate void PaintBackFunc(object box, SafeHDC hdc, Rectangle rect, Rectangle clipRect);

    // 获得字体
    // 注意 box 和 tag 可能并不是一致的。有可能一个 Paragraph 触发了函数，tag 是刚切分的还未形成 Line 和 Range 的文字，只有 tag。tag 是根据稍早触发 SplitRangeFunc 得到的 segment.Tag
    public delegate IEnumerable<Font> GetFontFunc(IBox box, object tag);
}
