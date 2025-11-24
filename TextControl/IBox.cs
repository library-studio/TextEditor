using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 盒子接口。
    /// 所谓盒子，是一种矩形的，可以独立实现编辑效果的显示区域
    /// </summary>
    public interface IBox
    {
        /*
        // 初始化文档
        int Initialize(
    SafeHDC dc,
    string content,
    int pixel_width,
    SplitRangeFunc splitRange);
        */

        // 替换一段或者全部文字
        int ReplaceText(
            Gdi32.SafeHDC dc,
            int start,
            int end,
            string content,
            int pixel_width,
            IContext context,
            out string replaced,
            out Rectangle update_rect,
            out Rectangle scroll_rect,
            out int scroll_distance);

        void Clear();

        // 根据单行行高计算出 Paragraph 总的像素高度
        int GetPixelHeight(/*int line_height*/);

        int GetPixelWidth();


        int TextLength { get; }

        string MergeText(int start = 0, int end = int.MaxValue);

        // int LineCount { get; }

        /*
        // 是否允许向下移动插入符一行？
        bool CanDown(HitInfo info);

        // 是否允许向上移动插入符一行?
        bool CanUp(HitInfo info);
        */

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

        HitInfo HitTest(int x,
    int y/*,
    int line_height*/);

        // 根据 Caret Offs 进行移动
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



        void Paint(SafeHDC dc,
            IContext context,
    int x,
    int y,
    Rectangle clipRect,
    int blockOffs1,
    int blockOffs2,
    int virtual_tail_length);

        // Color BackColor { get; set; }

        // ConvertTextFunc ConvertText { get; set; }
    }

    /// <summary>
    /// 用于定制效果的上下文接口
    /// </summary>
    public interface IContext
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
    }

    // 用于预先切割文字为更小段落的函数
    // 用法: 注意这个函数产生的 string [] 应该和原有的 text 不增减字符，不改变总字符数。
    // 而如果要增删字符内容，则需要考虑在调用 Initialize() 函数之前先行处理好
    public delegate string[] SplitRangeFunc(string text);

    // 将内部文字转换为显示文字的委托
    public delegate string ConvertTextFunc(string text);

    // 为每个 Range 对象动态计算出前景色
    public delegate Color GetForeColorFunc(Range box);
}
