using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Usp10;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 一行
    /// 由若干 Range 构成
    /// </summary>
    public class Line : IBox
    {
        public string Name { get; set; }

        IBox _parent = null;
        public IBox Parent => _parent;

        public int Height;  // 该 Line 的高度。由下属每个 Range 的高度汇总而来

        public List<RangeWrapper> Ranges { get; set; }
        public int[] piVisualToLogical { get; set; }
        public int[] piLogicalToVisual { get; set; }

        // Line 内的文字整体水平对齐方式
        public TextAlign TextAlign { get; set; }

        public ColorCache ColorCache = new ColorCache();


        public Line(IBox parent)
        {
            _parent = parent;
            Ranges = new List<RangeWrapper>();
            piVisualToLogical = new int[0];
            piLogicalToVisual = new int[0];
        }

        public string MergeText(int start = 0, int end = int.MaxValue)
        {
            if (end <= start || end <= 0)
                return "";
            if (Ranges == null)
                return "";
            StringBuilder sb = new StringBuilder();
            foreach (var range in Ranges)
            {
                sb.Append(range.Text);
            }
            string result = sb.ToString();
            if (end > result.Length)
                end = result.Length;
            if (start >= result.Length)
                return "";
            if (start <= 0 && end >= result.Length)
                return result;
            start = Math.Max(0, start);
            return result.Substring(start, end - start);
        }

        public int TextLength
        {
            get
            {
                if (Ranges == null)
                    return 0;
                int length = 0;
                foreach (var range in Ranges)
                {
                    length += range.Text.Length;
                }
                return length;
            }
        }

        public object Tag { get; set; }


#if REMOVED
        // 注: 是否考虑 trailing 字符。true 表示考虑 trailing 字符，false 表示不考虑 trailing 字符
        // 所谓“考虑 trailing 字符”的意思是，点击一个字符的中间靠右的部分，会滑向这个字符右侧的空隙。
        // 相对来说，“不考虑”的意思是点击字符的任何部分，都会滑向这个字符左侧的空隙。
        // parameters:
        //      x   点击位置 x。以 Line 的左边界为 0
        public bool HitTest(int x,
            out int hit_range_index,
            out int hit_x_in_line,
            out int cp_index)
        {
            // hit_range = null;
            hit_range_index = -1; // 没有找到 Range
            hit_x_in_line = -1;    // invalid x in range value
            cp_index = 0;

            if (x < 0)
                return false;
            // 按照视觉顺序对 Ranges 进行遍历
            for (int index = 0; index < piVisualToLogical.Length; index++)
            {
                var range = Ranges[index];
                int current_x = range.Left;

                bool isLastRange = (index == piVisualToLogical.Length - 1);
                if (x >= current_x
                    && (x < current_x + range.PixelWidth || isLastRange))
                {
                    // 找到对应的 Range
                    hit_range_index = index;
                    // hit_range = range;

                    /*
    // 构造 pwLogClust 参数的例子
    // 假设 text 是要显示的字符串，glfs 是 ScriptShape 得到的字形数组
    // pwLogClust 的长度应为 text.Length，每个元素表示该字符对应的 glyph 索引
    // 这里假设每个字符都映射到一个 glyph（简单情况），实际复杂脚本需用 ScriptShape 返回的 logClust
    string text = "Hello";
                    ushort[] glfs = new ushort[] { 10, 11, 12, 13, 14 }; // 假设 ScriptShape 得到的 glyph 索引
                    ushort[] pwLogClust = new ushort[text.Length];
                    for (int i = 0; i < text.Length; i++)
                    {
                        pwLogClust[i] = (ushort)i; // 简单一一对应，复杂脚本需用 ScriptShape 返回的 logClust
                    }
                    // pwLogClust 现在为 [0, 1, 2, 3, 4]                     * 
                     * */


                    var result = ScriptXtoCP(x - current_x,
                        range.Text.Length,
                        range.glfs.Length,
                        range.logClust,
                        range.sva,
                        range.advances,
                        range.a,
                        out cp_index,
                        out int trailing);
                    result.ThrowIfFailed();

                    bool isRightBlank = (x >= current_x + range.PixelWidth);
                    /*
                    if (isRightBlank)
                    {
                        hit_x_in_line = current_x + range.PixelWidth;
                        return true;
                    }
                    */
                    // 中间靠右滑向右侧，Caret Position (Offset) 要对 trailing 进行调整
                    cp_index += trailing;
                    result = ScriptCPtoX(cp_index,
                        false,  // isRightBlank ? true : trailing != 0,
                        range.Text.Length,
                        range.glfs.Length,
                        range.logClust,
                        range.sva,
                        range.advances,
                        range.a,
                        out int hit_x_in_range);
                    result.ThrowIfFailed();

                    hit_x_in_line = current_x + hit_x_in_range;
                    return true;
                }
            }

            return false;
        }

#endif

#if REMOVED
        // 注: 是否考虑 trailing 字符。true 表示考虑 trailing 字符，false 表示不考虑 trailing 字符
        // 所谓“考虑 trailing 字符”的意思是，点击一个字符的中间靠右的部分，会滑向这个字符右侧的空隙。
        // 相对来说，“不考虑”的意思是点击字符的任何部分，都会滑向这个字符左侧的空隙。
        // parameters:
        //      x   点击位置 x。以 Line 的左边界为 0
        //      hit_offs    [out] 返回点击字符的 offs。注意，这是相对于整个 Line 的 offs
        public bool HitTest(int x,
            out int hit_offs,
            //out int hit_range_index,
            out int hit_x_in_line
            // out int cp_index
            )
        {
            hit_offs = -1;  // invalid offs value
            hit_x_in_line = -1;    // invalid x in range value

            if (x < 0)
                return false;

            // 行中没有任何文本的情况
            if (piVisualToLogical.Length == 0)
            {
                hit_offs = 0;
                hit_x_in_line = 0;
                return true;
            }

            // 按照视觉顺序对 Ranges 进行遍历
            for (int index = 0; index < piVisualToLogical.Length; index++)
            {
                var range = Ranges[index];
                int current_x = range.Left;

                bool isLastRange = (index == piVisualToLogical.Length - 1);
                if (x >= current_x
                    && (x < current_x + range.PixelWidth || isLastRange))
                {
                    /*
    // 构造 pwLogClust 参数的例子
    // 假设 text 是要显示的字符串，glfs 是 ScriptShape 得到的字形数组
    // pwLogClust 的长度应为 text.Length，每个元素表示该字符对应的 glyph 索引
    // 这里假设每个字符都映射到一个 glyph（简单情况），实际复杂脚本需用 ScriptShape 返回的 logClust
    string text = "Hello";
                    ushort[] glfs = new ushort[] { 10, 11, 12, 13, 14 }; // 假设 ScriptShape 得到的 glyph 索引
                    ushort[] pwLogClust = new ushort[text.Length];
                    for (int i = 0; i < text.Length; i++)
                    {
                        pwLogClust[i] = (ushort)i; // 简单一一对应，复杂脚本需用 ScriptShape 返回的 logClust
                    }
                    // pwLogClust 现在为 [0, 1, 2, 3, 4]                     * 
                     * */


                    var result = ScriptXtoCP(x - current_x,
                        range.Text.Length,
                        range.glfs.Length,
                        range.logClust,
                        range.sva,
                        range.advances,
                        range.a,
                        out int cp_index,
                        out int trailing);
                    result.ThrowIfFailed();

                    bool isRightBlank = (x >= current_x + range.PixelWidth);
                    /*
                    if (isRightBlank)
                    {
                        hit_x_in_line = current_x + range.PixelWidth;
                        return true;
                    }
                    */
                    // 中间靠右滑向右侧，Caret Position (Offset) 要对 trailing 进行调整
                    cp_index += trailing;
                    result = ScriptCPtoX(cp_index,
                        false,  // isRightBlank ? true : trailing != 0,
                        range.Text.Length,
                        range.glfs.Length,
                        range.logClust,
                        range.sva,
                        range.advances,
                        range.a,
                        out int hit_x_in_range);
                    result.ThrowIfFailed();

                    hit_x_in_line = current_x + hit_x_in_range;
                    int start_offs = GetStartOffs(index);
                    if (start_offs == -1)
                        throw new Exception($"index 为 {index} 的 Range 对象没有找到");
                    hit_offs = start_offs + cp_index;
                    return true;
                }
            }

            return false;
        }
#endif
        // 注: 是否考虑 trailing 字符。true 表示考虑 trailing 字符，false 表示不考虑 trailing 字符
        // 所谓“考虑 trailing 字符”的意思是，点击一个字符的中间靠右的部分，会滑向这个字符右侧的空隙。
        // 相对来说，“不考虑”的意思是点击字符的任何部分，都会滑向这个字符左侧的空隙。
        // parameters:
        //      x   点击位置 x。以 Line 的左边界为 0
        //      hit_offs    [out] 返回点击字符的 offs。注意，这是相对于整个 Line 的 offs
        public HitInfo HitTest(int x,
            int y)
        {
            // int hit_offs = -1;  // invalid offs value
            // int hit_x_in_line = -1;    // invalid x in range value

            //if (x < 0)
            //    return false;
            int line_height = this.GetPixelHeight();

            // 行中没有任何文本的情况
            if (piVisualToLogical.Length == 0)
            {
                // hit_offs = 0;
                // hit_x_in_line = 0;
                return new HitInfo
                {
                    X = 0,
                    Y = 0,
                    ChildIndex = 0,
                    TextIndex = 0,
                    Offs = 0,
                    LineHeight = line_height,
                    Area = Area.Text
                };
            }

            // 按照视觉顺序对 Ranges 进行遍历
            for (int index = 0; index < piVisualToLogical.Length; index++)
            {
                var range = Ranges[index];
                int current_x = range.Left;

                bool isLastRange = (index == piVisualToLogical.Length - 1);
                bool isFirstRange = (index == 0);
                if ((x >= current_x || isFirstRange)
                    && (x < current_x + range.PixelWidth || isLastRange))
                {
                    /*
    // 构造 pwLogClust 参数的例子
    // 假设 text 是要显示的字符串，glfs 是 ScriptShape 得到的字形数组
    // pwLogClust 的长度应为 text.Length，每个元素表示该字符对应的 glyph 索引
    // 这里假设每个字符都映射到一个 glyph（简单情况），实际复杂脚本需用 ScriptShape 返回的 logClust
    string text = "Hello";
                    ushort[] glfs = new ushort[] { 10, 11, 12, 13, 14 }; // 假设 ScriptShape 得到的 glyph 索引
                    ushort[] pwLogClust = new ushort[text.Length];
                    for (int i = 0; i < text.Length; i++)
                    {
                        pwLogClust[i] = (ushort)i; // 简单一一对应，复杂脚本需用 ScriptShape 返回的 logClust
                    }
                    // pwLogClust 现在为 [0, 1, 2, 3, 4]                     * 
                     * */
                    bool isRightBlank = (x >= current_x + range.PixelWidth);
                    int start_offs = GetStartOffs(index);
                    if (start_offs == -1)
                        throw new Exception($"index 为 {index} 的 Range 对象没有找到");

                    if (range.DisplayText.Length == 0)
                        return new HitInfo
                        {
                            X = current_x + 0,
                            Y = 0,
                            ChildIndex = index,
                            TextIndex = 0,
                            Offs = start_offs + 0,
                            LineHeight = line_height,
                            Area = isRightBlank ? Area.RightBlank : Area.Text,
                            InnerHitInfo = new HitInfo { X = 0 },
                        };

                    var result = ScriptXtoCP(x - current_x,
                        range.DisplayText.Length,
                        range.glfs.Length,
                        range.logClust,
                        range.sva,
                        range.advances,
                        range.a,
                        out int cp_index,
                        out int trailing);
                    result.ThrowIfFailed();

                    /*
                    if (isRightBlank)
                    {
                        hit_x_in_line = current_x + range.PixelWidth;
                        return true;
                    }
                    */
                    // 中间靠右滑向右侧，Caret Position (Offset) 要对 trailing 进行调整
                    cp_index += trailing;
                    result = ScriptCPtoX(cp_index,
                        false,  // isRightBlank ? true : trailing != 0,
                        range.DisplayText.Length,
                        range.glfs.Length,
                        range.logClust,
                        range.sva,
                        range.advances,
                        range.a,
                        out int hit_x_in_range);
                    result.ThrowIfFailed();

                    // hit_x_in_line = current_x + hit_x_in_range;
                    return new HitInfo
                    {
                        X = current_x + hit_x_in_range,
                        Y = 0,
                        ChildIndex = index,
                        TextIndex = cp_index,
                        Offs = start_offs + cp_index,
                        LineHeight = line_height,
                        Area = isRightBlank ? Area.RightBlank : Area.Text,
                        InnerHitInfo = new HitInfo { X = hit_x_in_range },
                    };
                }
            }

            return new HitInfo
            {
                X = 0,
                Y = 0,
                ChildIndex = 0,
                TextIndex = 0,
                Offs = 0,
                LineHeight = line_height,
                Area = Area.RightBlank,
                InnerHitInfo = null,
            };
        }

        // 计算一个 range 的起始 offs
        int GetStartOffs(int range_index)
        {
            int start = 0;
            int i = 0;
            foreach (var range in Ranges)
            {
                if (i >= range_index)
                    return start;
                start += range.Text.Length;
                i++;
            }
            return -1;  // 表示指定的 range_index 没有找到
        }

        // 根据 Caret Offs 进行移动
        // parameters:
        //      offs    插入符在当前 Line 中的偏移
        //      direction   -1 向左 0 原地 1 向右。注: direction 可能是 -2 -3 +2 +3 等等
        // return:
        //      -1  越过左边
        //      0   成功
        //      1   越过右边
        public int MoveByOffs(int offs,
            int direction,
            out HitInfo info)
        {
            info = new HitInfo();
            if (offs + direction < 0)
                return -1;
            /*
            if (direction == -1 && offs <= 0)
                return -1;
            */
            int start_offs = 0;
            int i = 0;
            foreach (var range in Ranges)
            {
                if (offs + direction >= start_offs && offs + direction <= start_offs + range.DisplayText.Length)
                {
                    var pos = offs - start_offs;
                    if (direction <= -1)
                        pos += direction;
                    else if (direction > 1)
                        pos += direction - 1;

                    int hit_x_in_range = 0;
                    int offs1 = 0;
                    int trailing1 = 0;

                    if (range.DisplayText.Length > 0)
                    {
                        var result = ScriptCPtoX(pos,
                direction >= 1,
                range.DisplayText.Length,
                range.glfs.Length,
                range.logClust,
                range.sva,
                range.advances,
                range.a,
                out hit_x_in_range);
                        result.ThrowIfFailed();

                        info.X = range.Left + hit_x_in_range; // + current_x;
                        info.Y = 0;
                        info.ChildIndex = i;
                        info.Box = range;
                        info.InnerHitInfo = new HitInfo
                        {
                            X = hit_x_in_range,
                        };

                        if (is_zero_width(range.sva, pos, direction >= 1) == false)   // range.advances.Where(o => o == 0).Any() == false
                        {
                            result = ScriptXtoCP(hit_x_in_range,
                range.DisplayText.Length,
                range.glfs.Length,
                range.logClust,
                range.sva,
                range.advances,
                range.a,
                out offs1,
                out trailing1);
                            result.ThrowIfFailed();
                        }
                        else
                        {
                            offs1 = pos;
                            trailing1 = direction >= 1 ? 1 : 0;
                        }
                    }

                    info.Offs = start_offs + offs1 + trailing1;
                    info.TextIndex = offs1;
                    info.Area = Area.Text;
                    info.LineHeight = this.GetPixelHeight();
                    return 0;
                }

                start_offs += range.DisplayText.Length;
                i++;
            }

            // 没有任何 Range 的情况
            if (i == 0
                && offs + direction == 0)
            {
                info.X = 0;
                info.Y = 0;
                info.ChildIndex = 0;
                info.Offs = offs + direction;
                info.TextIndex = 0;
                info.Area = Area.Text;
                info.LineHeight = this.GetPixelHeight();
                return 0;
            }

            return 1;

            // 判断 index 位置是否为一个 0 显示宽度的字符
            bool is_zero_width(SCRIPT_VISATTR[] svs, int index, bool trailing)
            {
                if (trailing == false)
                {
                    index--;
                    if (index < 0 || index >= svs.Length)
                        return false;
                    return svs[index].fZeroWidth;
                }
                if (index < 0 || index >= svs.Length)
                    return false;
                return svs[index].fZeroWidth;
            }
        }

        // 获得一段文本显示范围的 Region
        // parameters:
        //      virtual_tail_length 如果为 1，表示需要关注末尾结束符是否在选择范围内，如果在，要加入一个表示结束符的矩形
        public Region GetRegion(int start_offs = 0,
            int end_offs = int.MaxValue,
            int virtual_tail_length = 0)
        {
            if (end_offs < start_offs)
                throw new ArgumentException($"start_offs ({start_offs}) 必须小于或等于 end_offs ({end_offs})");

            if (this.Ranges?.Count == 0)
            {
                // 如果需要虚拟尾部字符
                if (virtual_tail_length > 0
    && start_offs <= 0
    && end_offs >= 0 + virtual_tail_length)
                {
                    return new Region(new Rectangle(0, 0,
                        FontContext.DefaultReturnWidth,
                        FontContext.DefaultFontHeight));
                }
                return null;
            }

            if (start_offs == end_offs)
                return null;
            if (end_offs <= 0)
                return null;
            if (start_offs >= this.TextLength + virtual_tail_length)
                return null;

            Region region = null;
            int current_offs = 0;
            // 注意这是视觉上最右边的一个
            var last_index = this.piVisualToLogical[this.piVisualToLogical.Length - 1];
            int i = 0;
            foreach (var range in this.Ranges)
            {
                var rect = range.GetRect(start_offs - current_offs,
                    end_offs - current_offs,
                    i == last_index ? virtual_tail_length : 0);
                if (rect.IsEmpty == false)
                {
                    // 注: RangeWrapper 得到的 Rect 不用再 Offset 调整，但要重置 Height
                    rect.Height = this.Height;
                    rect.Y = 0;
                    if (region == null)
                        region = new Region(rect);
                    else
                    {
                        region.Union(rect);
                    }
                }
                current_offs += range.TextLength;
                i++;
            }

            return region;
        }


        public bool CaretMoveDown(
    int x,
    int y,
    out HitInfo info)
        {
            info = new HitInfo();
            return false;
        }

        public bool CaretMoveUp(
            int x,
            int y,
            out HitInfo info)
        {
            info = new HitInfo();
            return false;
        }

#if REMOVED
        public int GetGlobalOffs(HitInfo info)
        {
            var ret = this.HitTest(info.X,
                info.Y);
            return ret.Offs;
        }
#endif

#if REMOVED
        // **** 准备废弃
        // TODO: 调用 MoveByOffs()
        public HitInfo HitByGlobalOffs(int offs_param,
    bool trailing)
        {
            HitInfo info = new HitInfo();

            int offs = 0;
            RangeWrapper range = null;
            int start_x = 0;
            int start_y = 0;

            var line_start_offs = offs; // 本行的起始偏移量
            for (int i = 0; i < this.Ranges.Count; i++)
            {
                range = this.Ranges[i];
                start_x = range.Left;
                if (offs + range.DisplayText?.Length >= offs_param)
                {
                    info.ChildIndex = i;
                    info.TextIndex = offs_param - line_start_offs;
                    info.Offs = offs + info.TextIndex;
                    info.Area = Area.Text;
                    goto END1;
                }
                offs += range.DisplayText.Length;

                // info.RangeIndex++;
            }


            info.Area = Area.RightBlank;

        END1:
            if (range != null)
            {
                int hit_x_in_range = 0;
                if (range.DisplayText.Length > 0)
                {
                    var result = ScriptCPtoX(offs_param - offs,
            trailing,  // isRightBlank ? true : trailing != 0,
            range.DisplayText.Length,
            range.glfs.Length,
            range.logClust,
            range.sva,
            range.advances,
            range.a,
            out hit_x_in_range);
                    result.ThrowIfFailed();
                }

                info.X = range.Left + hit_x_in_range; // + current_x;
                info.Y = start_y;

                /*
                result = ScriptXtoCP(hit_x_in_range,
    range.Text.Length,
    range.glfs.Length,
    range.logClust,
    range.sva,
    range.advances,
    range.a,
    out int offs1,
    out int trailing1);
                result.ThrowIfFailed();
                output_offs = offs1 + trailing1;
                */
            }
            else
            {
                info.X = start_x;   // ??
                info.Y = start_y;
            }

            return info;
        }

#endif

        public int GetPixelHeight()
        {
            if (this.Height == 0)
                return FontContext.DefaultFontHeight;
            return this.Height; // 由下属每个 Range 的高度汇总而来
        }

        // 从左侧基线(竖线)开始直到最右端的宽度。
        // 注意，这个宽度并不包括溢出基线左侧的部分
        public int GetPixelWidth()
        {
            // return this.Ranges.Sum(r => r.PixelWidth);
            if (this.Ranges == null || this.Ranges.Count == 0)
                return 0;
            return this.Ranges.Max(r => r.Left + r.PixelWidth);
        }

        // 实际有内容的 x 位置。如果为负数，表示溢出到左侧以外了
        public int GetX()
        {
            if (this.Ranges == null || this.Ranges.Count == 0)
                return 0;
            return this.Ranges.Min(r => r.Left);
        }

        // 获得包围实际文字内容的矩形。注意左侧可能会溢出(在 Align Center 等情形)，x 为负数。
        public Rectangle GetBoundRect()
        {
            var width = this.GetPixelWidth();
            var delta = this.GetX();
            return new Rectangle(delta, 0, width - delta, GetPixelHeight());
        }

        public ReplaceTextResult ReplaceText(
            IContext context,
            SafeHDC dc,
            int start,
            int end,
            string text,
            int pixel_width/*,
            out string replaced,
            out Rectangle update_rect,
            out Rectangle scroll_rect,
            out int scroll_distance*/)
        {
            /*
            replaced = "";
            update_rect = System.Drawing.Rectangle.Empty;
            scroll_rect = System.Drawing.Rectangle.Empty;
            scroll_distance = 0;
            */
            var result = new ReplaceTextResult();

            Paragraph.InitializeUspEnvironment();

            // 获得当前内容 Rectangle
            Rectangle old_rect = System.Drawing.Rectangle.Empty;
            {
                /*
                var w = this.GetPixelWidth();
                var h = this.GetPixelHeight();
                old_rect = new Rectangle(0, 0, w, h);
                */
                old_rect = GetBoundRect();
            }


            string old_content = this.MergeText();

            if (end == -1)
                end = old_content.Length;
            var content = old_content.Substring(0, start) + text + old_content.Substring(end);

            // 如果没有发生变化
            if (content == old_content)
            {
                result.MaxPixel = this.GetPixelWidth();
                //InitialDefault();
                return result;
            }

            this.Clear();

            /*
            if (content == null)
            {
                return 0;
            }
            */

            if (string.IsNullOrEmpty(content))
            {
                /*
                var new_range = new Range() { Tag = null };
                //new_range.ConvertText = this.ConvertText;
                Ranges.Add(new_range);
                // TODO: 这里 Range 对象没有完成 Uniscribe 初始化，后面用到会有问题
                */
                this.Clear();
                this.Tag = null;
                result.UpdateRect = old_rect;
                result.MaxPixel = 0;
                //InitialDefault();
                return result;
            }

            var contents = context?.SplitRange?.Invoke(this, content)
                ?? new Segment[] {
                    new Segment
                    {
                        Text = content,
                        Tag = null
                    }
                };

            // Debug.Assert(contents.Length == 1);

            int max_pixel_width = 0;
            foreach (var seg in contents)
            {
                var segment = seg.Text;
                var segment_text = segment.Length == 0 ? " " : segment;
                int cMaxItems = segment_text.Length + 1;
                var pItems = new SCRIPT_ITEM[cMaxItems + 1];

                /*
                 * 

    返回值
    如果成功，则返回 0。 如果函数不成功，则返回非零 HRESULT 值。

    如果 pwcInChars 设置为 NULL、 cInChars 为 0、 pItems 设置为 NULL 或 cMaxItems< 2，则该函数将返回E_INVALIDARG。

    如果 cMaxItems 的值不足，函数将返回E_OUTOFMEMORY。 与所有错误情况一样，不会完全处理任何项，并且输出数组中没有任何部分包含定义的值。 如果函数返回E_OUTOFMEMORY，则应用程序可以使用更大的 pItems 缓冲区再次调用它。
                * */
                var result0 = ScriptItemize(convertText(segment_text),
                    segment_text.Length,
                    cMaxItems,
                    Paragraph.sc,
                    Paragraph.ss,
                    pItems,
                    out int pcItems);
                result0.ThrowIfFailed();

                Array.Resize(ref pItems, pcItems);
                /*
                for (int i = 0; i < pcItems; i++)
                {
                    var item = pItems[i];
                    if (Paragraph.sp[item.a.eScript].fComplex)
                    {
                        // requiring glyph shaping
                    }
                    else
                    {

                    }
                }
                */

                /*
                var width = BuildRanges(dc,
                    context,
                    pItems,
                    segment,
                    pixel_width,
                    seg.Tag);
                if (width > max_pixel_width)
                    max_pixel_width = width;
                */
                AppendRanges(dc,
    context,
    pItems,
    segment,
    pixel_width,
    seg.Tag);

            }

            {
                LayoutLine(this);
                max_pixel_width = RefreshLine(
                    (p, o) =>
                    {
                        return context?.GetFont?.Invoke(this, this.Tag);
                    },
                    dc,
                    this,
                    pixel_width);
            }

            // 获得新内容 Rectangle
            Rectangle new_rect = System.Drawing.Rectangle.Empty;
            {
                /*
                var w = this.GetPixelWidth();
                var h = this.GetPixelHeight();
                new_rect = new Rectangle(0, 0, w, h);
                */
                new_rect = GetBoundRect();
            }

            // update_rect = new Rectangle(0, 0, Math.Max(old_rect.Width, new_rect.Width), Math.Max(old_rect.Height, new_rect.Height));
            result.UpdateRect = Utility.Union(new_rect, old_rect);
            result.MaxPixel = max_pixel_width;
            // InitialDefault();
            return result;

            string convertText(string t)
            {
                return context?.ConvertText?.Invoke(t) ?? t;
            }

#if REMOVED
            void InitialDefault()
            {
                if (this.Ranges.Count == 0)
                {
                    // 用于探测字体高度宽度的默认 Range
                    var range = new Range();
                    range.ReplaceText(context,
                        dc,
                        0,
                        -1,
                        " ",
                        1000);
                    _defaultLineHeight = range.GetPixelHeight();
                    _returnWidth = range.GetPixelWidth();
                }
            }
#endif
        }

        public int Initialize(
SafeHDC dc,
string content,
int pixel_width,
IContext context)
        {
            Paragraph.InitializeUspEnvironment();

            this.Clear();

            /*
            if (content == null)
            {
                return 0;
            }
            */

            if (string.IsNullOrEmpty(content))
            {
                /*
                var new_range = new Range() { Tag = null };
                //new_range.ConvertText = this.ConvertText;
                Ranges.Add(new_range);
                return 0;
                */
                this.Clear();
                this.Tag = null;
                return 0;
            }

            var contents = context?.SplitRange?.Invoke(this, content)
                ?? new Segment[] {
                    new Segment
                    {
                        Text = content,
                        Tag = null
                    }
                };

            int max_pixel_width = 0;
            foreach (var seg in contents)
            {
                var segment = seg.Text;
                int cMaxItems = segment.Length + 1;
                var pItems = new SCRIPT_ITEM[cMaxItems + 1];

                /*
                 * 

    返回值
    如果成功，则返回 0。 如果函数不成功，则返回非零 HRESULT 值。

    如果 pwcInChars 设置为 NULL、 cInChars 为 0、 pItems 设置为 NULL 或 cMaxItems< 2，则该函数将返回E_INVALIDARG。

    如果 cMaxItems 的值不足，函数将返回E_OUTOFMEMORY。 与所有错误情况一样，不会完全处理任何项，并且输出数组中没有任何部分包含定义的值。 如果函数返回E_OUTOFMEMORY，则应用程序可以使用更大的 pItems 缓冲区再次调用它。
                * */
                var result = ScriptItemize(convertText(segment),
                    segment.Length,
                    cMaxItems,
                    Paragraph.sc,
                    Paragraph.ss,
                    pItems,
                    out int pcItems);
                result.ThrowIfFailed();

                Array.Resize(ref pItems, pcItems);
                /*
                for (int i = 0; i < pcItems; i++)
                {
                    var item = pItems[i];
                    if (Paragraph.sp[item.a.eScript].fComplex)
                    {
                        // requiring glyph shaping
                    }
                    else
                    {

                    }
                }
                */

                /*
                var width = BuildRanges(dc,
                    context,
                    pItems,
                    segment,
                    pixel_width,
                    seg.Tag);
                if (width > max_pixel_width)
                    max_pixel_width = width;
                */
                AppendRanges(dc,
                    context,
                    pItems,
                    segment,
                    pixel_width,
                    seg.Tag);
            }

            {
                LayoutLine(this);
                max_pixel_width = RefreshLine(
                    (p, o) =>
                    {
                        return context?.GetFont?.Invoke(this, this.Tag);
                    },
                    dc,
                    this,
                    pixel_width);
            }

            return max_pixel_width;

            string convertText(string t)
            {
                return context?.ConvertText?.Invoke(t) ?? t;
            }
        }

        public void Clear()
        {
            Ranges.Clear();
            piVisualToLogical = new int[0];
            piLogicalToVisual = new int[0];
            Tag = null;
            ColorCache?.Clear();
        }

        // 即将废止。用 AppendRanges() 代替
        int BuildRanges(SafeHDC dc,
            IContext context,
            SCRIPT_ITEM[] pItems,
            string content,
            int pixel_width,
            object tag)
        {
            if (Paragraph.sp == null)
                throw new ArgumentException("Script properties not initialized.");

            this.Clear();

            // long start_pixel = 0;
            int start_index = 0;
            for (int i = 0; i < pItems.Length; i++)
            {
                var item = pItems[i];

                // 析出本 item 的文字
                string str = "";
                if (i >= pItems.Length - 1)
                    str = content.Substring(start_index);
                else
                {
                    int length = pItems[i + 1].iCharPos - item.iCharPos;
                    str = content.Substring(start_index, length);
                }

                var new_range = new RangeWrapper
                {
                    Text = str,
                    DisplayText = context?.ConvertText?.Invoke(str) ?? str,
                    Item = item,
                    a = item.a,
                    Tag = tag,
                };
                //new_range.ConvertText = this.ConvertText;
                this.Ranges.Add(new_range);
                start_index += str.Length;
            }

            LayoutLine(this);
            return RefreshLine(
                (p, o) =>
                {
                    return context?.GetFont?.Invoke(this, this.Tag);
                },
                dc,
                this,
                pixel_width);
        }

        internal void AppendRanges(SafeHDC dc,
            IContext context,
            SCRIPT_ITEM[] pItems,
            string content,
            int pixel_width,
            object tag)
        {
            if (Paragraph.sp == null)
                throw new ArgumentException("Script properties not initialized.");

            // long start_pixel = 0;
            int start_index = 0;
            for (int i = 0; i < pItems.Length; i++)
            {
                var item = pItems[i];

                // 析出本 item 的文字
                string str = "";
                if (i >= pItems.Length - 1)
                    str = content.Substring(start_index);
                else
                {
                    int length = pItems[i + 1].iCharPos - item.iCharPos;
                    str = content.Substring(start_index, length);
                }

                var new_range = new RangeWrapper
                {
                    Text = str,
                    DisplayText = context?.ConvertText?.Invoke(str) ?? str,
                    Item = item,
                    a = item.a,
                    Tag = tag,
                };
                //new_range.ConvertText = this.ConvertText;
                this.Ranges.Add(new_range);
                start_index += str.Length;
            }

            //LayoutLine(this);
            //return RefreshLine(dc, this, pixel_width);
        }


        public static void ShapeAndPlace(
            GetFontFunc func_getfont,
            SafeHDC dc,
ref SCRIPT_ANALYSIS sa,
SafeSCRIPT_CACHE cache,
string str,
out ushort[] glfs,
out int[] piAdvance,
out GOFFSET[] pGoffset,
out ABC pABC,
out SCRIPT_VISATTR[] sva,
out ushort[] log,
ref Font used_font)
        {
            var max = (int)Math.Round(str.Length * 1.5m + 16);
            glfs = new ushort[max];
            log = new ushort[str.Length];
            sva = new SCRIPT_VISATTR[max];

            IEnumerable<Font> fonts = null;

            if (used_font != null)
            {
                fonts = new List<Font>() { used_font };
            }
            else
            {
                fonts = func_getfont?.Invoke(null, null);
                /*
                fonts.AddRange(_fonts);
                if (_default_font != null)
                    fonts.Insert(0, _default_font);
                */
            }

            foreach (var font in fonts)
            {
                var font_handle = font.ToHfont();
                try
                {
                    // var handle = Gdi32.SelectObject(dc, font.ToHfont());
                    using (var dc_context = dc.SelectObject(font_handle))
                    {
                        cache = new SafeSCRIPT_CACHE();
                        uint USP_E_SCRIPT_NOT_IN_FONT = 0x80040200;
                        var result = ScriptShape(dc,
                            cache,
                            str,
                            str.Length,
                            max,
                            ref sa, // 指向运行的 SCRIPT_ANALYSIS 结构的指针，其中包含之前对 ScriptItemize 的调用的结果。
                            glfs,
                            log,
                            sva,
                            out var c);
                        if (result == USP_E_SCRIPT_NOT_IN_FONT)
                            continue;

                        result.ThrowIfFailed();

                        Array.Resize(ref glfs, c);

                        // 检查是否有空的字形
                        // TODO: 记下最少空字形的一轮，以便最后采纳
                        if (glfs.Where(g => g == 0).Any())
                            continue;

                        if (used_font == null)
                            used_font = font; // 记录实际使用的字体

                        Array.Resize(ref sva, c);

                        piAdvance = new int[c];
                        pGoffset = new GOFFSET[c];
                        result = ScriptPlace(dc,
                            cache,
                            glfs,
                            c,
                            sva,
                            ref sa,
                            piAdvance,
                            pGoffset,
                            out pABC);
                        result.ThrowIfFailed();

                        return;
                    }
                }
                finally
                {
                    Gdi32.DeleteFont(font_handle);
                }
            }

            throw new Exception($"字符串 '{str}' 中出现了无法显示的字形");
        }


        public static void LayoutLine(Line line)
        {
            List<byte> levels = new List<byte>();
            foreach (var range in line.Ranges)
            {
                levels.Add((byte)range.a.s.uBidiLevel);
            }

            int[] piVisualToLogical = new int[line.Ranges.Count];
            int[] piLogicalToVisual = new int[line.Ranges.Count];
            var result = ScriptLayout(
                line.Ranges.Count,
                levels.ToArray(),
                piVisualToLogical,
                piLogicalToVisual);
            result.ThrowIfFailed();

            line.piVisualToLogical = piVisualToLogical;
            line.piLogicalToVisual = piLogicalToVisual;
        }


        // 重做一次 ShapeAndPlace()，刷新某些成员。并调整每个 Range 的 abc
        // return:
        //      返回行的 Pixel 宽度
        public static int RefreshLine(
            GetFontFunc func_getfont,
            SafeHDC hdc,
            Line line,
            int pixel_width)
        {
            int x_offset = 0;
            foreach (var index in line.piVisualToLogical)   // piVisualToLogical
            {
                var range = line.Ranges[index];

                bool isLeftMost = (index == 0);
                bool isRightMost = (index == line.Ranges.Count - 1);

                Font used_font = range.Font;
                var cache = new SafeSCRIPT_CACHE();
                var a = range.a;
                ShapeAndPlace(
                    func_getfont,
                    hdc,
                    ref a,
                    cache,
                    range.DisplayText,
                    out ushort[] glfs,
                    out int[] piAdvance,
                    out GOFFSET[] pGoffset,
                    out ABC pABC,
                    out SCRIPT_VISATTR[] sva,
                    out ushort[] log,
                    ref used_font);
                if (range.Font == null)
                    range.Font = used_font; // 记录实际使用的字体

                range.Font = used_font;
                range.sva = sva; // sva 在 SplitLines() 中尚未计算，是在这里首次计算的。TODO: 将来可以改为在 SplieLines() 结束前计算
                range.advances = piAdvance; // 记录 advances
                range.glfs = glfs;
                range.logClust = log;
                range.pABC = pABC;

                range.Left = x_offset;

                range.PixelWidth = (int)(pABC.abcA + pABC.abcB + pABC.abcC);
                if (isLeftMost && pABC.abcA < 0)
                {
                    range.PixelWidth += -pABC.abcA;
                    range.Left += -pABC.abcA;
                }
                if (isRightMost && pABC.abcC < 0)
                {
                    range.PixelWidth += -pABC.abcC;
                }

                range.pGoffset = pGoffset; // 记录 pGoffset
                range.a = a;

                x_offset += range.PixelWidth; // 更新 line 的左边界位置。注意这个左边界位置不能靠遍历 Range 元素累加来获得，因为逻辑顺序和显示顺序不一定是一致的
            }

            if (line.TextAlign != TextAlign.None)
            {
                x_offset += line.AlignmentText(pixel_width, x_offset);
            }

            line.ProcessBaseline(null);
            return x_offset; // 返回行的 Pixel 宽度
        }

        // 2025/11/27
        // 调整行内文字对齐效果
        // parameters:
        //      container_width 容器的宽度
        //      current_width   纯粹文字部分的宽度
        // return:
        //      返回行 pixel 变化的分量
        int AlignmentText(int container_width, int current_width)
        {
            if (this.TextAlign == TextAlign.None)
                return 0;
            // 溢出
            // 注: 如果 OverflowXXX 一个也没有，则按照 XXX 直接处理。也就是说这种定义方式下，溢出与不溢出的效果一致
            if (current_width > container_width
                && (this.TextAlign & TextAlign.OverflowMask) != 0)
            {
                if (this.TextAlign.HasFlag(TextAlign.OverflowLeft))
                    return 0;
                int delta = container_width - current_width;
                if (this.TextAlign.HasFlag(TextAlign.OverflowCenter))
                    delta /= 2;
                foreach (var range in this.Ranges)
                {
                    range.Left += delta;
                }
                return delta;
            }
            else
            {
                if (this.TextAlign.HasFlag(TextAlign.Left))
                    return 0;
                int delta = container_width - current_width;
                if (this.TextAlign.HasFlag(TextAlign.Center))
                    delta /= 2;
                foreach (var range in this.Ranges)
                {
                    range.Left += delta;
                }
                return delta;
            }
        }

        static PRECT Intersect(PRECT rect, System.Drawing.Rectangle clipRect)
        {
            var result = (PRECT)System.Drawing.Rectangle.Intersect((System.Drawing.Rectangle)rect, clipRect);
            return result;
        }



        // parameters:
        //      block_start     选中范围的开始偏移量。
        //                      以当前 line 的左边界为 0
        //                      如果大于本行文字长度，表示未选中本行     
        //      block_end       选中范围的结束偏移量
        //                      以当前 line 的左边界为 0
        //                      如果小于 0，表示未选中本行     
        //      virtual_tail_length 行末虚拟尾部字符个数。如果这个尾部处在选择范围，需要显示为选择背景色
        public void Paint(
            IContext context,
            SafeHDC hdc,
            int x,
            int y,
            Rectangle clipRect,
            int blockOffs1,
            int blockOffs2,
            int virtual_tail_length)
        {
            Line line = this;


            int line_height = this.GetPixelHeight();
            // 代表回车换行符号字符的像素宽度
            // int _average_char_width = Line.GetAverageCharWidth();

            var block_start = Math.Min(blockOffs1, blockOffs2);
            var block_end = Math.Max(blockOffs1, blockOffs2);

            block_start = Math.Max(0, block_start);
            block_end = Math.Min(line.TextLength + virtual_tail_length, block_end);

            //block_start = 0;
            //block_end = 100;

            // 绘制 Range 特定的背景色。指非 highlight 背景色
            foreach (var range in line.Ranges)   // piVisualToLogical
            {
                // 绘制普通背景色
                // 如果 .GetBackColor() 返回 Color.Transparent 则不进行绘制，保持背景透明
                {
                    // var bk_color = context.GetBackColor?.Invoke(range, false) ?? Color.Transparent;
                    var bk_color = range.ColorCache
                        .GetBackColor(context.GetBackColor,
                        range,
                        false);
                    if (bk_color != Color.Transparent)
                    {
                        var rect = GetBlockRect(range,
x + range.Left,
y,
line_height,
0,
range.Text.Length);
                        // var larger = ((Rectangle)(rect)).Larger();
                        if (clipRect.IntersectsWith((Rectangle)rect))
                        {
                            DrawSolidRectangle(hdc,
                            rect.left,
                            rect.top,
                            rect.right,
                            rect.bottom,
                            bk_color,
                            clipRect);
                        }
                    }
                }
            }

            // 块的背景矩形数组
            PRECT[] block_rects = new PRECT[line.Ranges.Count];
            // 块是否包含全部文字的标志
            bool[] full_flags = new bool[line.Ranges.Count];

            // 先绘制行和块背景
            // 以逻辑顺序遍历 Ranges。注意显示位置 x 可能是跳动的
            // 每个 Range 的块背景色不能在分散到每个 Range 的处理中绘制，因为那样可能会擦掉 Range 伸出去的笔画(例如 Italic 风格的 'f')。
            if (block_start != block_end)
            {
                int i = 0;
                int tail_range_index = 0;
                if (line.Ranges.Count > 0 && line.piLogicalToVisual.Length > line.Ranges.Count - 1)
                    tail_range_index = line.piVisualToLogical[line.Ranges.Count - 1];
                foreach (var range in line.Ranges)   // piVisualToLogical
                {
                    var is_tail_in_line = i == tail_range_index;

                    // 绘制选中范围的背景色
                    if (block_start <= range.DisplayText.Length && block_end >= 0)
                    {
                        var tail_in_block = is_tail_in_line
                            && virtual_tail_length > 0
                            && block_start < range.DisplayText.Length + virtual_tail_length
                            && block_end > range.DisplayText.Length;
                        var block_rect = GetBlockRect(range,
                            x + range.Left,
                            y,
                            line_height,
                            block_start,
                            block_end);

                        // 如果当前 range 正巧是视觉上最右边一个 range，那么可以加宽宽度即可。
                        // 但如果不是最右边的，则要另外专门绘制一次最右边的这个代表回车的块
                        if (tail_in_block)
                        {
                            if (i == this.piLogicalToVisual[i])
                                block_rect.Width += FontContext.DefaultReturnWidth;
                            else
                            {
                                PaintReturnSelectedBack(
                                    context,
                                    hdc,
                                    x,
                                    y,
                                    clipRect);
                            }
                        }

                        // var larger = ((Rectangle)(block_rect)).Larger();

                        if (clipRect.IntersectsWith((Rectangle)block_rect))
                        {
                            // var back_color = context?.GetBackColor?.Invoke(range, true) ?? SystemColors.Highlight;
                            var back_color = range.ColorCache
                                .GetBackColor(context?.GetBackColor,
                                range,
                                true);

                            if (back_color != Color.Transparent)
                            {
                                DrawSolidRectangle(hdc,
                                block_rect.left,
                                block_rect.top,
                                block_rect.right,   // + (tail_in_block ? _average_char_width : 0),
                                block_rect.bottom,
                                back_color,
                                clipRect);
                            }
                        }
                        // clipping 矩形的左右进行微调。避免斜体字的某些笔画伸出去的部分被显示成不同的颜色
                        var left_delta = range.pABC.abcA;
                        var right_delta = range.pABC.abcC;
                        if (block_start <= 0 && left_delta < 0)
                            block_rect.left -= -left_delta + 1; // 左侧空白
                        if (block_end >= range.DisplayText.Length && right_delta < 0)
                            block_rect.right += -right_delta + 1; // 右侧空白

                        block_rects[i] = block_rect; // 记录块背景矩形
                        full_flags[i] = (block_start <= 0 && block_end >= range.DisplayText.Length); // 标记本 Range 是否全选
                    }

                    block_start -= range.DisplayText.Length;
                    block_end -= range.DisplayText.Length;

                    i++;
                }

                // 没有任何 Range 的情况，依然要显示 tail char
                if (i == 0 && virtual_tail_length > 0)
                {
                    // 绘制选中范围的背景色
                    if (block_start <= 0 && block_end >= virtual_tail_length)
                    {
                        var block_rect = new PRECT(x, y,
                            x + FontContext.DefaultReturnWidth,
                            y + line_height);

                        // var larger = ((Rectangle)(block_rect)).Larger();
                        if (clipRect.IntersectsWith((Rectangle)block_rect))
                        {
                            // var back_color = context?.GetBackColor?.Invoke(null, true) ?? SystemColors.Highlight;
                            var back_color = line.ColorCache
                                .GetBackColor(
                                context?.GetBackColor,
                                null,
                                true);
                            if (back_color != Color.Transparent)
                            {
                                DrawSolidRectangle(hdc,
                                        block_rect.left,
                                        block_rect.top,
                                        block_rect.right,
                                        block_rect.bottom,
                                        back_color,
                                        clipRect);
                            }
                        }
                    }
                }
            }

            // 再绘制文本
            foreach (var index in line.piVisualToLogical)   // piVisualToLogical
            {
                var range = line.Ranges[index];

                if (string.IsNullOrEmpty(range.DisplayText))
                    continue;

                var block_rect = block_rects[index]; // 获取块背景矩形
                var full_block = full_flags[index]; // 获取是否全选标志

                Font used_font = range.Font;
                var cache = new SafeSCRIPT_CACHE();
                /*
                var a = line.a;
                ShapeAndPlace(
                    ref a,
                    cache,
                    hdc,
                    line.Text,
                    out ushort[] glfs,
                    out int[] piAdvance,
                    out GOFFSET[] pGoffset,
                    out ABC pABC,
                    out SCRIPT_VISATTR[] sva,
                    out ushort[] log,
                    ref used_font);
                if (line.Font == null)
                    line.Font = used_font; // 记录实际使用的字体

                line.sva = sva; // sva 在 SplitLines() 中尚未计算，是在这里首次计算的。TODO: 将来可以改为在 SplieLines() 结束前计算
                line.advances = piAdvance; // 记录 advances
                line.glfs = glfs;
                line.logClust = log;
                line.PixelWidth = (int)(pABC.abcA + pABC.abcB + pABC.abcC);

                line.a = a;
                line.Left = x_offset; // 记录 line 的左边界位置。注意这个左边界位置不能靠遍历 Range 元素累加来获得，因为逻辑顺序和显示顺序不一定是一致的
                */

                int iReserved = 0;
                // uint fuOptions = 0; // /*(int)Gdi32.ETO.ETO_OPAQUE |*/ (int)Gdi32.ETO.ETO_CLIPPED;


                // //
                PRECT item_rect = new PRECT();
                item_rect.left = x + range.Left;
                item_rect.top = y;
                item_rect.Width = range.PixelWidth;   // (int)(pABC.abcA + pABC.abcB + pABC.abcC);
                item_rect.Height = line_height;

                var font_handle = used_font.ToHfont();
                try
                {
                    using (var dc_context = hdc.SelectObject(font_handle))
                    {
                        /*
                        // 绘制选中范围的背景色
                        if (block_start != block_end
                            && (block_start < line.Text.Length && block_end >= 0))
                        {
                            var block_rect = GetBlockRect(line, block_start, block_end);
                            DrawSolidRectangle(hdc,
                                block_rect.left + item_rect.left,
                                block_rect.top + item_rect.top,
                                block_rect.right + item_rect.left,
                                block_rect.bottom + item_rect.top,
                                new COLORREF(Color.Yellow));
                        }
                        */

                        // 第一次显示 Range 内全部文字，用 Text Color
                        // 如果是全部属于块，这样的第一次显示正常文字可以省略
                        if ((full_block == false || block_rect == null)
                            && clipRect.IntersectsWith((Rectangle)(item_rect)))
                        {
                            // var text_color = context?.GetForeColor?.Invoke(range, false) ?? SystemColors.WindowText;
                            var text_color = range.ColorCache
                                .GetForeColor(
                                context?.GetForeColor,
                                range,
                                false);

                            var old_color = Gdi32.SetTextColor(hdc, new COLORREF(text_color)); // 设置文本颜色为黑色
                            var old_mode = Gdi32.SetBkMode(hdc, Gdi32.BackgroundMode.TRANSPARENT); // 设置背景模式为透明

                            try
                            {
                                var result = ScriptTextOut(hdc,
                                            cache,
                                            x + range.Left,
                                            y + range.Y,    // y + _line_height - (int)GetAscentPixel(used_font),
                                            (int)Gdi32.ETO.ETO_CLIPPED, // fuOptions,
                                            Intersect(item_rect, clipRect),   // [In, Optional] PRECT lprc,
                                            range.a,  // line.Item.a, // in SCRIPT_ANALYSIS psa,
                                            range.DisplayText,  // range.Text,  //  [Optional, MarshalAs(UnmanagedType.LPWStr)] string ? pwcReserved,
                                            iReserved,  //  [Optional] int iReserved,
                                            range.glfs,   // [In, MarshalAs(UnmanagedType.LPArray)] ushort[] pwGlyphs, 
                                            range.glfs.Length,    // int cGlyphs,
                                            range.advances,  // [In, MarshalAs(UnmanagedType.LPArray)] int[] piAdvance,
                                            null,   // [In, Optional, MarshalAs(UnmanagedType.LPArray)] int[] ? piJustify,
                                            range.pGoffset[0]); // in GOFFSET pGoffset); 
                                result.ThrowIfFailed();
                            }
                            finally
                            {
                                Gdi32.SetBkMode(hdc, old_mode);
                                Gdi32.SetTextColor(hdc, old_color); // 恢复文本颜色
                            }
                        }

                        // 第二次显示块部分文字，用 Highlight Color
                        if (block_rect != null
                            && clipRect.IntersectsWith((Rectangle)(block_rect)))
                        {
                            // var highlight_text_color = context?.GetForeColor?.Invoke(range, true) ?? SystemColors.HighlightText;
                            var highlight_text_color = range.ColorCache
                                .GetForeColor(
                                context?.GetForeColor,
                                range,
                                true);
                            var old_color = Gdi32.SetTextColor(hdc, new COLORREF(highlight_text_color));
                            var old_mode = Gdi32.SetBkMode(hdc, Gdi32.BackgroundMode.TRANSPARENT); // 设置背景模式为透明

                            //var old_bk_color = Gdi32.SetBkColor(hdc, new COLORREF((uint)SystemColors.Highlight.ToArgb())); // 设置文本颜色为黑色
                            try
                            {
                                var ret = ScriptTextOut(hdc,
                cache,
                x + range.Left,
                y + range.Y,    // y + _line_height - (int)GetAscentPixel(used_font),
                (int)Gdi32.ETO.ETO_CLIPPED, // | (int)Gdi32.ETO.ETO_OPAQUE,
                Intersect(block_rect, clipRect),   // [In, Optional] PRECT lprc,
                range.a,  // line.Item.a, // in SCRIPT_ANALYSIS psa,
                range.DisplayText,  // range.Text,  //  [Optional, MarshalAs(UnmanagedType.LPWStr)] string ? pwcReserved,
                iReserved,  //  [Optional] int iReserved,
                range.glfs,   // [In, MarshalAs(UnmanagedType.LPArray)] ushort[] pwGlyphs, 
                range.glfs.Length,    // int cGlyphs,
                range.advances,  // [In, MarshalAs(UnmanagedType.LPArray)] int[] piAdvance,
                null,   // [In, Optional, MarshalAs(UnmanagedType.LPArray)] int[] ? piJustify,
                range.pGoffset[0]); // in GOFFSET pGoffset); 
                                ret.ThrowIfFailed();
                            }
                            finally
                            {
                                Gdi32.SetBkMode(hdc, old_mode);
                                Gdi32.SetTextColor(hdc, old_color); // 恢复文本颜色
                            }
                        }
                    }
                    // x_offset += line.PixelWidth;    // pABC.abcA + pABC.abcB + pABC.abcC;
                }
                finally
                {
                    Gdi32.DeleteFont(font_handle);
                }
            }
        }

        // 绘制代表回车换行符号的选择背景
        void PaintReturnSelectedBack(
            IContext context,
            SafeHDC hdc,
            int x,
            int y,
            Rectangle clipRect)
        {
            var visual_right_index = this.piVisualToLogical[this.piVisualToLogical.Length];
            var range = this.Ranges[visual_right_index];
            var block_rect = new Rectangle(x + range.Left,
                y,
                FontContext.DefaultReturnWidth,
                range.GetPixelHeight());
            if (clipRect.IntersectsWith(block_rect))
            {
                // var back_color = context?.GetBackColor?.Invoke(range, true) ?? SystemColors.Highlight;
                var back_color = range.ColorCache
                    .GetBackColor(
                    context?.GetBackColor,
                    range,
                    true);
                if (back_color != Color.Transparent)
                {
                    DrawSolidRectangle(hdc,
                    block_rect.Left,
                    block_rect.Top,
                    block_rect.Right,
                    block_rect.Bottom,
                    back_color,
                    clipRect);
                }
            }
        }

        // 绘制一个实心的带有颜色的矩形区域
        public static void DrawSolidRectangle(SafeHDC hdc,
            int left,
            int top,
            int right,
            int bottom,
            COLORREF color,
            System.Drawing.Rectangle clipRect)
        {
            using (var region = Gdi32.CreateRectRgn(clipRect.Left,
                clipRect.Top,
                clipRect.Right,
                clipRect.Bottom))
            {
                Gdi32.SelectClipRgn(hdc, region);
                // 这里的绘制操作只会影响 region 区域
                {
                    // 创建实心画刷
                    var hBrush = Gdi32.CreateSolidBrush(color);
                    var oldBrush = Gdi32.SelectObject(hdc, hBrush);

                    int delta = 0;
                    var hPen = Gdi32.CreatePen((int)Gdi32.PenStyle.PS_NULL, delta, color);
                    var hOldPen = Gdi32.SelectObject(hdc, hPen);
                    //var old_mode = Gdi32.SetBkMode(hdc, Gdi32.BackgroundMode.OPAQUE);

                    // 绘制实心矩形
                    Gdi32.Rectangle(hdc,
                        left,
                        top,
                        right + 1,
                        bottom + 1);    // +1 的原因是想要让上下相邻的 rect 看起来连续。但要注意 GetRegion() 接口中得到的区域要向右下也扩大一个像素，避免刷新时漏掉一些线

                    //Gdi32.SetBkMode(hdc, old_mode);

                    // 恢复原画刷并释放资源
                    Gdi32.SelectObject(hdc, oldBrush);
                    Gdi32.SelectObject(hdc, hOldPen);
                    Gdi32.DeleteObject(hBrush);
                    Gdi32.DeleteObject(hPen);   // 2025/12/21
                }

                // 恢复剪裁（可选）
                Gdi32.SelectClipRgn(hdc, HRGN.NULL);
            }
        }

        PRECT GetBlockRect(RangeWrapper range,
            int x,
            int y,
            int line_height,
            int block_start,
            int block_end)
        {
            block_start = Math.Max(0, block_start);
            block_end = Math.Min(range.Text.Length, block_end);

            int start_x = 0;
            int end_x = 0;
            if (range.Text.Length > 0)
            {
                var result = ScriptCPtoX(block_start,
        false,  // isRightBlank ? true : trailing != 0,
        range.Text.Length,
        range.glfs.Length,
        range.logClust,
        range.sva,
        range.advances,
        range.a,
        out start_x);
                result.ThrowIfFailed();

                result = ScriptCPtoX(block_end,
    false,  // isRightBlank ? true : trailing != 0,
    range.Text.Length,
    range.glfs.Length,
    range.logClust,
    range.sva,
    range.advances,
    range.a,
    out end_x);
                result.ThrowIfFailed();
            }

            var rect = new PRECT();
            rect.top = y;
            rect.left = x + Math.Min(start_x, end_x);
            rect.right = x + Math.Max(end_x, start_x);
            rect.Height = line_height;
            return rect;
        }

        static float GetAscentPixel(Font used_font)
        {
            // 计算字体的 ascent
            // https://github.com/MicrosoftDocs/win32/blob/docs/desktop-src/gdiplus/-gdiplus-obtaining-font-metrics-use.md
            var fontFamily = used_font.FontFamily;

            // the ascent in design units and pixels.
            var ascent = fontFamily.GetCellAscent(used_font.Style);

            // fontFamily.GetCellDescent(used_font.Style);

            // 14.484375 = 16.0 * 1854 / 2048
            return used_font.GetHeight() * ascent / fontFamily.GetEmHeight(used_font.Style);
        }

#if REMOVED
        void ProcessBaseLine(Font default_font)
        {
            float max_up_height = 0;
            float max_blow_height = 0;
            List<float> up_heights = new List<float>();

            foreach (var range in this.Ranges)
            {
                var font = range.Font ?? default_font;
                if (font == null)
                {
                    // 防御性处理：若没有字体可用，跳过
                    up_heights.Add(0f);
                    continue;
                }

                var fontFamily = font.FontFamily;
                var height = font.GetHeight();
                var spacing = fontFamily.GetLineSpacing(font.Style);

                // 设计单位的 ascent/descent/emHeight
                var ascent = fontFamily.GetCellAscent(font.Style);
                var descent = fontFamily.GetCellDescent(font.Style);
                var emHeight = fontFamily.GetEmHeight(font.Style);

                // 以设计单位为依据计算像素高度
                var up_height = height * ascent / emHeight;
                var blow_height = height * descent / emHeight;

                Debug.WriteLine($"{fontFamily.Name} height={height} lineSpacing={spacing} ascent={ascent} descent={descent} emHeight={emHeight} up_height={up_height} blow_height={blow_height}");

                if (up_height > max_up_height)
                    max_up_height = up_height;
                if (blow_height > max_blow_height)
                    max_blow_height = blow_height;

                up_heights.Add(up_height);
            }

            // 使用实际的 ascent + descent（像素）来作为行高，而不是直接用 Font.GetHeight()
            var requiredHeight = max_up_height + max_blow_height;
            if (requiredHeight <= 0)
            {
                // 回退到默认行高
                this.Height = Line._line_height > 0 ? Line._line_height : 0;
            }
            else
            {
                this.Height = (int)Math.Ceiling(requiredHeight);
            }

            // 计算基线 Y（基于行高和下部高度）
            var base_line_y = this.Height - max_blow_height;

            // 为每个 range 计算 Y 偏移
            for (int i = 0; i < this.Ranges.Count; i++)
            {
                var range = this.Ranges[i];
                var up_height = up_heights[i];
                range.Y = (int)Math.Round(base_line_y - up_height);
            }
        }
#endif

#if REMOVED
        void ProcessBaseLine(Font default_font)
        {
            /*
            float max_up_height = 0;
            float max_blow_height = 0;
            float max_height = 0;
            */
            List<float> up_heights = new List<float>();
            List<float> below_heights = new List<float>();
            foreach (var range in this.Ranges)
            {
                var font = range.Font;
                if (font == null)
                    font = default_font;

                var fontFamily = font.FontFamily;
                var height = font.GetHeight();

                // var em_height = fontFamily.GetEmHeight(font.Style);

                // the ascent in design units and pixels.
                var ascent = fontFamily.GetCellAscent(font.Style);
                var descent = fontFamily.GetCellDescent(font.Style);
                var line_spacing = fontFamily.GetLineSpacing(font.Style);


                var em_height = line_spacing;
                var spacing = em_height - (ascent + descent);
                // fontFamily.GetCellDescent(used_font.Style);

                // 14.484375 = 16.0 * 1854 / 2048
                var spacing_height = height * spacing / em_height;
                var up_height = height * ascent / em_height;
                var below_height = height * descent / em_height;

                Debug.WriteLine($"{fontFamily.Name} height={height} em_height={em_height} spacing={spacing} ascent={ascent} descent={descent} up_height={up_height} blow_height={below_height} spacing_height={spacing_height}");

                /*
                // 调整 spacing_height
                height -= spacing_height;
                spacing_height = 0;
                */

                /*
                if (up_height > max_up_height)
                    max_up_height = up_height;
                if (below_height + spacing_height > max_blow_height)
                    max_blow_height = below_height + spacing_height;
                if (height > max_height)
                    max_height = height;
                */

                up_heights.Add(up_height + spacing_height);
                below_heights.Add(below_height);
            }

            var max_up = up_heights.Max();
            var max_blow = below_heights.Max();

            // 注: 阿拉伯文可能会高于汉字
            this.Height = (int)Math.Ceiling(max_up + max_blow); // 记录行的高度
            var base_line_y = max_up;// 计算基线位置

            Debug.WriteLine($"this.Height={this.Height} base_line_y={base_line_y}");

            int i = 0;
            foreach (var range in this.Ranges)
            {
                var up_height = up_heights[i];
                range.Y = (int)(base_line_y - up_height);
                i++;
            }
        }
#endif
        void ProcessBaseline(Font default_font)
        {
            foreach (var range in this.Ranges)
            {
                range.ProcessBaseline(default_font);
            }

            if (this.Ranges.Count == 0)
            {
                this._baseLine = 0;
                this._below = 0;
                this.Height = 0;
                return;
            }

            var max_up = this.Ranges.Max(r => r.Ascent + r.Spacing);
            var max_below = this.Ranges.Max(r => r.Descent);

            this._baseLine = max_up;
            this._below = max_below;

            // 注: 阿拉伯文可能会高于汉字
            this.Height = (int)Math.Ceiling(max_up + max_below); // 记录行的高度

            // 按照基线对齐
            foreach (var range in this.Ranges)
            {
                var up_height = range.Ascent + range.Spacing;
                range.Y = (int)(max_up - up_height);
            }
        }

        float _baseLine;
        public float BaseLine
        {
            get
            {
                return _baseLine;
            }
        }

        float _below;
        public float Below
        {
            get
            {
                return _below;
            }
        }

#if REMOVED

        int _defaultLineHeight = 0;

        public int GetDefaultLineHeight()
        {
            return _defaultLineHeight;
        }

        int _returnWidth = 0;

        // 回车换行符号字符的像素宽度
        public int ReturnWidth
        {
            get
            {
                return _returnWidth;
            }
        }
#endif

#if REMOVED

        #region Fonts

        public static int ReturnWidth()
        {
            // 代表回车换行符号字符的像素宽度
            return Line.GetAverageCharWidth();
        }

        // 当前字体的联结关系
        static Link _fontLink = null;

        // 候选的字体列表
        static List<Font> _fonts = new List<Font>();

        internal static Font _default_font = null;
        internal static int _line_height = 0;
        internal static int _average_char_width = 0;
        internal static int _max_char_width = 0;

        public static int GetLineHeight()
        {
            return _line_height;
        }

        public static int GetAverageCharWidth()
        {
            return _average_char_width;
        }

        public static int GetMaxCharWidth()
        {
            return _max_char_width;
        }

        // return:
        //      返回字体高度
        public static int InitialFonts(Font default_font)
        {
            _default_font = default_font;
            _line_height = default_font.Height;
            var fontName = default_font.FontFamily.GetName(0);
            _fontLink = FontLink.GetLink(fontName, FontLink.FirstLink);

            Link.DisposeFonts(_fonts);
            _fonts = _fontLink.BuildFonts(default_font);

            if (_average_char_width == 0)
            {
                _average_char_width = (int)ComputeAverageWidth(_default_font, out float max_value);
                _max_char_width = (int)max_value;
            }

            return _line_height;
        }

        public static void DisposeFonts()
        {
            Link.DisposeFonts(_fonts);
        }


        public static float ComputeAverageWidth(Font font,
            out float maxCharWidth)
        {
            maxCharWidth = 0F;
            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))    // Graphics.FromHdc(dc.DangerousGetHandle())
            {
                {
                    // TODO: 循环测试一组可能的最宽字符
                    string sample = "中国人民";
                    SizeF size = g.MeasureString(sample, font);
                    maxCharWidth = size.Width / 4;
                }
                {
                    string sample = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                    SizeF size = g.MeasureString(sample, font);
                    return size.Width / sample.Length;
                }
            }
        }

        public static float ComputeAverageWidth(SafeHDC dc, Font font)
        {
            using (var g = Graphics.FromHdc(dc.DangerousGetHandle()))
            {
                string sample = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                SizeF size = g.MeasureString(sample, font);
                return size.Width / sample.Length;
            }
        }


        #endregion

#endif

        #region 配合测试的代码

        public Line(IBox parent, string text)
        {
            _parent = parent;
            Ranges = new List<RangeWrapper>()
            {
                new RangeWrapper(text)
            };
        }

        #endregion

        public void ClearCache()
        {
            if (this.Ranges == null)
                return;
            this.ColorCache?.Clear();
            foreach (var range in this.Ranges)
            {
                range.ClearCache();
            }
        }
    }

    [Flags]
    public enum TextAlign
    {
        None = 0,
        Left = 0x01,
        Center = 0x02,
        Right = 0x04,
        OverflowLeft = 0x08,
        OverflowCenter = 0x10,
        OverflowRight = 0x20,
        OverflowMask = OverflowLeft | OverflowCenter | OverflowRight,
    }

    /// <summary>
    /// 对 Range 包装了位置和缓存信息的类
    /// </summary>
    public class RangeWrapper : Range
    {
        public int Left; // 左边界 x 坐标

        public int Y; // 该 Range 的左上角 y 坐标

        public ColorCache ColorCache = new ColorCache();

        public RangeWrapper()
        {

        }

        public RangeWrapper(string text) : base(text)
        {
        }

        public override void Clear()
        {
            base.Clear();
            Left = 0;
            Y = 0;
            ColorCache?.Clear();
        }

        public override void ClearCache()
        {
            this.ColorCache?.Clear();
            base.ClearCache();
        }

        public override Region GetRegion(int start_offs = 0,
            int end_offs = int.MaxValue,
            int virtual_tail_length = 0)
        {
            var rect = GetRect(start_offs, end_offs, virtual_tail_length);
            if (rect.IsEmpty)
                return null;
            //rect.Width += 1;
            //rect.Height += 1;   // 和 line.Paint() 中绘制选择背景时向右下扩大一个像素配套
            return new Region(rect);
        }

        public override Rectangle GetRect(int start_offs = 0,
            int end_offs = int.MaxValue,
            int virtual_tail_length = 0)
        {
            var rect = base.GetRect(start_offs, end_offs, virtual_tail_length);
            if (rect.IsEmpty)
                return rect;
            // 调整位置
            rect.Offset(Left, Y);
            return rect;
        }
    }

    // 缓存的各种颜色
    public class ColorCache
    {
        Color? _highlightForeColor = null;
        Color? _highlightBackColor = null;
        Color? _foreColor = null;
        Color? _backColor = null;

        public void Clear()
        {
            _highlightForeColor = null;
            _highlightBackColor = null;
            _foreColor = null;
            _backColor = null;
        }

        public Color GetForeColor(GetForeColorFunc func,
            IBox box,
            bool highlight)
        {
            if (highlight)
            {
                if (_highlightForeColor == null)
                    _highlightForeColor = func?.Invoke(box, highlight) ?? SystemColors.HighlightText;
                return (Color)_highlightForeColor;
            }
            if (_foreColor == null)
                _foreColor = func?.Invoke(box, highlight) ?? SystemColors.WindowText;
            return (Color)_foreColor;
        }

        public Color GetBackColor(GetBackColorFunc func,
    IBox box,
    bool highlight)
        {
            if (highlight)
            {
                if (_highlightBackColor == null)
                    _highlightBackColor = func?.Invoke(box, highlight) ?? SystemColors.Highlight;
                return (Color)_highlightBackColor;
            }
            if (_backColor == null)
                _backColor = func?.Invoke(box, highlight) ?? Color.Transparent;
            return (Color)_backColor;
        }
    }
}
