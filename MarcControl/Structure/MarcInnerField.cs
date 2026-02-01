using System;
using System.Collections.Generic;
using System.Drawing;

using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 用于嵌套的字段。
    /// 结构上暗含前导的两个字符(子字段符号+'1')，但不在视觉上显示出来
    /// </summary>
    public class MarcInnerField : MarcField
    {
        // 是否为纯文本。指没有字段名和指示符部分的纯粹 _content 内容，类似头标区的情况
        public bool PlainText { get; set; }

        /*
        // 内嵌的字段暂时无法表达头标区
        public override bool IsHeader
        {
            get
            {
                return false;
            }
            set
            {

            }
        }
        */

        public override int TextLength {
            get
            {
                if (this.PlainText)
                    return base.PureTextLength;
                return 2 + base.PureTextLength;
            }

        }

        public override string MergeTextMask(int start = 0, int end = int.MaxValue)
        {
            if (this.PlainText)
                return base.MergePureText(start, end);
            else
            {
                // 前导 2 字符
                var leading_fragment = MergeLeadingText(start, end);
                start -= 2;
                end -= 2;
                var content_fragment = base.MergePureTextMask(start, end);
                return leading_fragment + content_fragment;
            }
        }

        // 注意不包含原 MarcField.MergeText() 返回的尾部字段结束符
        public override string MergeText(int start = 0,
            int end = int.MaxValue)
        {
            if (this.PlainText)
                return base.MergePureText(start, end);
            else
            {
                // 前导 2 字符
                var leading_fragment = MergeLeadingText(start, end);
                start -= 2;
                end -= 2;
                var content_fragment = base.MergePureText(start, end);
                return leading_fragment + content_fragment;
            }
        }

        string MergeLeadingText(int start = 0,
            int end = int.MaxValue)
        {
            if (start >= 2)
                return "";
            if (end <= 0)
                return "";
            start = Math.Max(0, start);
            end = Math.Min(2, end);
            return "\u001f1".Substring(start, end - start);
        }

        public override bool CaretMoveDown(int x, int y, out HitInfo info)
        {
            var ret = base.CaretMoveDown(x, y, out info);
            if (this.PlainText)
                return ret;
            else
            {
                info.Offs += 2;
                return ret;
            }
        }

        public override bool CaretMoveUp(int x, int y, out HitInfo info)
        {
            var ret = base.CaretMoveUp(x, y, out info);
            if (this.PlainText)
                return ret;
            else
            {
                info.Offs += 2;
                return ret;
            }
        }

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
        public override int MoveByOffs(int offs,
            int direction,
            out HitInfo info)
        {
            if (this.PlainText)
            {
                var ret = base.MoveByOffs(offs, direction, out info);
                return ret;
            }
            else
            {
                if (offs + direction >= 0
                    && offs + direction < 2)
                {
                    int current_offs = 0 - direction;
                    if (direction <= 0)
                        current_offs = 2 - direction;
                    var ret = base.MoveByOffs(current_offs,
                        direction,
                        out info);
                    info.Offs += 2;
                    return ret;
                }

                {
                    var ret = base.MoveByOffs(offs - 2,
        direction,
        out info);
                    info.Offs += 2;
                    return ret;
                }
            }
        }

        public override Region GetRegion(int start_offs = 0,
    int end_offs = int.MaxValue,
    int virtual_tail_length = 0)
        {
            if (this.PlainText)
            {
                return base.GetRegion(start_offs, start_offs, 0);
            }
            else
            {
                // 跳过前导 2 字符
                // TODO: 如果将来前导 2 字符在选中的时候也要显示块背景色，则需要在这里 Union 它的 Region
                start_offs -= 2;
                end_offs -= 2;
                return base.GetRegion(start_offs, start_offs, 0);
            }
        }

        public override HitInfo HitTest(int x, int y)
        {
            var ret = base.HitTest(x, y);
            if (this.PlainText)
                return ret;
            else
            {
                ret.Offs += 2;
                return ret;
            }
        }

        public override ReplaceTextResult ReplaceText(
    ViewModeTree view_mode_tree,
    IContext context,
    SafeHDC dc,
    int start_param,
    int end_param,
    string content,
    int pixel_width)
        {
            // 如果内容为 $? 前导，则去掉此二字符后进入 base 对象内容
            // 如果内容不是 $? 前导，则可以考虑直接进入 MarcField 的 _content 部分，_name 和 _indicator 部分内容都是空

            int start = start_param;
            int end = end_param;

            var old_text = this.MergeText();
            if (end == -1)
                end = old_text.Length;
            var new_text = old_text.Substring(0, start) + content + old_text.Substring(end);
            if (new_text.StartsWith("\u001f1"))
            {
                new_text = new_text.Substring(2);
                start -= 2;
                end -= 2;

                if (end <= 0)
                {
                    start = 0;
                    end = -1;
                }

                this.PlainText = false;

                return base.ReplaceText(
    view_mode_tree,
    context,
    dc,
    start,
    end,
    new_text,
    pixel_width);
            }
            else
            {
                this.PlainText = true;
                base.IsHeader = true;

                if (start_param == 0 && end_param == -1)
                {
                    start = 0;
                    end = -1;
                }

                try
                {
                    return base.ReplaceText(
        view_mode_tree,
        context,
        dc,
        start,
        end,
        content,
        pixel_width);
                }
                finally
                {
                    base.IsHeader = false;
                }
            }
        }

        public override void Paint(
    IContext context,
    SafeHDC dc,
    int x,
    int y,
    Rectangle clipRect,
    int blockOffs1,
    int blockOffs2,
    int virtual_tail_length)
        {
            if (this.PlainText)
            {
                base.Paint(context,
                    dc,
                    x,
                    y,
                    clipRect,
                    blockOffs1,
                    blockOffs2, 0);
            }
            else
            {
                base.Paint(context,
                    dc,
                    x,
                    y,
                    clipRect,
                    blockOffs1 - 2,
                    blockOffs2 - 2,
                    0);
            }
        }

    }
}
