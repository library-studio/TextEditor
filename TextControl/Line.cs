using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Contexts;
using System.Text;
using System.Threading.Tasks;

using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Kernel32;
using static Vanara.PInvoke.Usp10;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 一行
    /// 由若干 Range 构成
    /// </summary>
    public class Line : IBox
    {
        // public ConvertTextFunc ConvertText { get; set; }

        // public Color BackColor { get; set; } = Color.Transparent;

        public int Height;  // 该 Line 的高度。由下属每个 Range 的高度汇总而来

        public List<Range> Ranges { get; set; }
        public int[] piVisualToLogical { get; set; }
        public int[] piLogicalToVisual { get; set; }

        public Line()
        {
            Ranges = new List<Range>();
            piVisualToLogical = new int[0];
            piLogicalToVisual = new int[0];
        }

#if REMOVED
        public string Text
        {
            get
            {
                if (Ranges == null)
                    return "";
                StringBuilder sb = new StringBuilder();
                foreach (var range in Ranges)
                {
                    sb.Append(range.Text);
                }
                return sb.ToString();
            }
        }
#endif

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
                        range.DisplayText.Length,
                        range.glfs.Length,
                        range.logClust,
                        range.sva,
                        range.advances,
                        range.a,
                        out int hit_x_in_range);
                    result.ThrowIfFailed();

                    // hit_x_in_line = current_x + hit_x_in_range;
                    int start_offs = GetStartOffs(index);
                    if (start_offs == -1)
                        throw new Exception($"index 为 {index} 的 Range 对象没有找到");
                    return new HitInfo
                    {
                        X = current_x + hit_x_in_range,
                        Y = 0,
                        ChildIndex = index,
                        TextIndex = cp_index,
                        Offs = start_offs + cp_index,
                        LineHeight = line_height,
                        Area = isRightBlank ? Area.RightBlank : Area.Text,
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

        // **** 准备废弃
        // TODO: 调用 MoveByOffs()
        public HitInfo HitByGlobalOffs(int offs_param,
    bool trailing)
        {
            HitInfo info = new HitInfo();

            int offs = 0;
            Range range = null;
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



        public int GetPixelHeight()
        {
            if (this.Height == 0)
                return Line._line_height;
            return this.Height; // 由下属每个 Range 的高度汇总而来
        }

        public int GetPixelWidth()
        {
            return this.Ranges.Sum(r => r.PixelWidth);
        }

        public int ReplaceText(
Gdi32.SafeHDC dc,
int start,
int end,
string text,
int pixel_width,
            IContext context,
            out string replaced,
            out Rectangle update_rect,
            out Rectangle scroll_rect,
            out int scroll_distance)
        {
            replaced = "";
            update_rect = System.Drawing.Rectangle.Empty;
            scroll_rect = System.Drawing.Rectangle.Empty;
            scroll_distance = 0;

            Paragraph.InitializeUspEnvironment();

            // 获得当前内容 Rectangle
            Rectangle old_rect = System.Drawing.Rectangle.Empty;
            {
                var w = this.GetPixelWidth();
                var h = this.GetPixelWidth();
                old_rect = new Rectangle(0, 0, w, h);
            }


            string content = this.MergeText();

            if (end == -1)
                end = content.Length;
            content = content.Substring(0, start) + text + content.Substring(end);

            this.Clear();

            if (content == null)
            {
                return 0;
            }

            if (string.IsNullOrEmpty(content))
            {
                var new_range = new Range();
                //new_range.ConvertText = this.ConvertText;
                Ranges.Add(new_range);
                return 0;
            }

            string[] contents = null;
            if (context?.SplitRange != null)
                contents = context.SplitRange(content);
            else
                contents = new string[] { content };

            int max_pixel_width = 0;
            foreach (var segment in contents)
            {
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

                var width = BuildRanges(dc, context, pItems, segment);
                if (width > max_pixel_width)
                    max_pixel_width = width;
            }

            // 获得新内容 Rectangle
            Rectangle new_rect = System.Drawing.Rectangle.Empty;
            {
                var w = this.GetPixelWidth();
                var h = this.GetPixelWidth();
                new_rect = new Rectangle(0, 0, w, h);
            }

            update_rect = new Rectangle(0, 0, Math.Max(old_rect.Width, new_rect.Width), Math.Max(old_rect.Height, new_rect.Height));
            return max_pixel_width;

            string convertText(string t)
            {
                if (context?.ConvertText != null)
                    return context?.ConvertText(t);
                return t;
            }
        }

        public int Initialize(
SafeHDC dc,
string content,
int pixel_width,
IContext context)
        {
            Paragraph.InitializeUspEnvironment();

            this.Clear();

            if (content == null)
            {
                return 0;
            }

            if (string.IsNullOrEmpty(content))
            {
                var new_range = new Range();
                //new_range.ConvertText = this.ConvertText;
                Ranges.Add(new_range);
                return 0;
            }

            string[] contents = null;
            if (context?.SplitRange != null)
                contents = context?.SplitRange(content);
            else
                contents = new string[] { content };

            int max_pixel_width = 0;
            foreach (var segment in contents)
            {

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

                var width = BuildRanges(dc, context, pItems, segment);
                if (width > max_pixel_width)
                    max_pixel_width = width;
            }

            return max_pixel_width;

            string convertText(string t)
            {
                if (context?.ConvertText != null)
                    return context?.ConvertText(t);
                return t;
            }
        }

        public void Clear()
        {
            Ranges.Clear();
        }

        int BuildRanges(SafeHDC dc,
            IContext context,
SCRIPT_ITEM[] pItems,
string content)
        {
            if (Paragraph.sp == null)
                throw new ArgumentException("Script properties not initialized.");

            this.Ranges.Clear();

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

                var new_range = new Range
                {
                    Text = str,
                    DisplayText = context?.ConvertText(str) ?? str,
                    Item = item,
                    a = item.a,
                };
                //new_range.ConvertText = this.ConvertText;
                this.Ranges.Add(new_range);
                start_index += str.Length;
            }

            LayoutLine(this);
            return RefreshLine(dc, this);
        }

        public static void ShapeAndPlace(
ref SCRIPT_ANALYSIS sa,
SafeSCRIPT_CACHE cache,
SafeHDC dc,
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

            var fonts = new List<Font>();

            if (used_font != null)
                fonts.Add(used_font);
            else
            {
                fonts.AddRange(_fonts);
                if (_default_font != null)
                    fonts.Insert(0, _default_font);
            }

            foreach (var font in fonts)
            {
                var font_handle = font.ToHfont();
                try
                {
                    // var handle = Gdi32.SelectObject(dc, font.ToHfont());
                    using (var context = dc.SelectObject(font_handle))
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
        public static int RefreshLine(SafeHDC hdc,
            Line line)
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
                    ref a,
                    cache,
                    hdc,
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

            line.ProcessBaseLine(null);
            return x_offset; // 返回行的 Pixel 宽度
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
            SafeHDC hdc,
            IContext box_context,
            int x,
            int y,
            Rectangle clipRect,
            int blockOffs1,
            int blockOffs2,
            int virtual_tail_length)
        {
            Line line = this;


            int _line_height = this.GetPixelHeight();
            // 代表回车换行符号字符的像素宽度
            int _average_char_width = Line.GetAverageCharWidth();

            var block_start = Math.Min(blockOffs1, blockOffs2);
            var block_end = Math.Max(blockOffs1, blockOffs2);

            block_start = Math.Max(0, block_start);
            block_end = Math.Min(line.TextLength + virtual_tail_length, block_end);

            //block_start = 0;
            //block_end = 100;

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
                            _line_height,
                            block_start,
                            block_end);
                        if (clipRect.IntersectsWith(Utility.GetRectangle(block_rect)))
                        {
                            DrawSolidRectangle(hdc,
                            block_rect.left,
                            block_rect.top,
                            block_rect.right + (tail_in_block ? _average_char_width : 0),
                            block_rect.bottom,
                            new COLORREF(SystemColors.Highlight),
                            clipRect);
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
                            x + _average_char_width,
                            y + _line_height);

                        if (clipRect.IntersectsWith(Utility.GetRectangle(block_rect)))
                        {
                            DrawSolidRectangle(hdc,
                                block_rect.left,
                                block_rect.top,
                                block_rect.right,
                                block_rect.bottom,
                                new COLORREF(SystemColors.Highlight),
                                clipRect);
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
                uint fuOptions = 0; // /*(int)Gdi32.ETO.ETO_OPAQUE |*/ (int)Gdi32.ETO.ETO_CLIPPED;


                // //
                PRECT item_rect = new PRECT();
                item_rect.left = x + range.Left;
                item_rect.top = y;
                item_rect.Width = range.PixelWidth;   // (int)(pABC.abcA + pABC.abcB + pABC.abcC);
                item_rect.Height = _line_height;

                var font_handle = used_font.ToHfont();
                try
                {
                    using (var context = hdc.SelectObject(font_handle))
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
                            && clipRect.IntersectsWith(Utility.GetRectangle(item_rect)))
                        {
                            var text_color = box_context?.GetForeColor?.Invoke(range) ?? SystemColors.WindowText;
                            var old_color = Gdi32.SetTextColor(hdc, new COLORREF(text_color)); // 设置文本颜色为黑色

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
                                Gdi32.SetTextColor(hdc, old_color); // 恢复文本颜色
                            }
                        }

                        // 第二次显示块部分文字，用 Highlight Color
                        if (block_rect != null
                            && clipRect.IntersectsWith(Utility.GetRectangle(block_rect)))
                        {
                            var old_color = Gdi32.SetTextColor(hdc, new COLORREF(SystemColors.HighlightText)); // 设置文本颜色为黑色
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
                                Gdi32.SetTextColor(hdc, old_color); // 恢复文本颜色
                                                                    //Gdi32.SetBkColor(hdc, old_bk_color); // 恢复文本颜色
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

        // 示例：利用 Gdi32 绘制一个实心的带有颜色的矩形区域
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

                    var hPen = Gdi32.CreatePen((int)Gdi32.PenStyle.PS_NULL, 1, color);
                    var hOldPen = Gdi32.SelectObject(hdc, hPen);

                    // 绘制实心矩形
                    Gdi32.Rectangle(hdc, left, top, right + 1, bottom + 1);

                    // 恢复原画刷并释放资源
                    Gdi32.SelectObject(hdc, oldBrush);
                    Gdi32.SelectObject(hdc, hOldPen);
                    Gdi32.DeleteObject(hBrush);
                }

                // 恢复剪裁（可选）
                Gdi32.SelectClipRgn(hdc, HRGN.NULL);
            }
        }

        PRECT GetBlockRect(Range range,
            int x,
            int y,
            int _line_height,
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
            rect.Height = _line_height;
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

        void ProcessBaseLine(Font default_font)
        {
            float max_up_height = 0;
            float max_blow_height = 0;
            float max_height = 0;
            List<float> up_heights = new List<float>();
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

                var em_height = ascent + descent;
                // fontFamily.GetCellDescent(used_font.Style);

                // 14.484375 = 16.0 * 1854 / 2048
                var up_height = height * ascent / em_height;
                var blow_height = height * descent / em_height;

                if (up_height > max_up_height)
                    max_up_height = up_height;
                if (blow_height > max_blow_height)
                    max_blow_height = blow_height;
                if (height > max_height)
                    max_height = height;

                up_heights.Add(up_height);
            }

            this.Height = (int)Math.Ceiling(max_height); // 记录行的高度
            var base_line_y = max_height - max_blow_height;// 计算基线位置

            int i = 0;
            foreach (var range in this.Ranges)
            {
                var up_height = up_heights[i];
                range.Y = (int)(base_line_y - up_height);
                i++;
            }
        }

        #region Fonts

        // 当前字体的联结关系
        static Link _fontLink = null;

        // 候选的字体列表
        static List<Font> _fonts = new List<Font>();

        internal static Font _default_font = null;
        internal static int _line_height = 0;
        internal static int _average_char_width = 0;

        public static int GetLineHeight()
        {
            return _line_height;
        }

        public static int GetAverageCharWidth()
        {
            return _average_char_width;
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
                _average_char_width = (int)ComputeAverageWidth(_default_font);

            return _line_height;
        }

        public static void DisposeFonts()
        {
            Link.DisposeFonts(_fonts);
        }


        public static float ComputeAverageWidth(Font font)
        {
            using (var bitmap = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bitmap))    // Graphics.FromHdc(dc.DangerousGetHandle())
            {
                string sample = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
                SizeF size = g.MeasureString(sample, font);
                return size.Width / sample.Length;
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


        #region 配合测试的代码

        public Line(string text)
        {
            Ranges = new List<Range>() {
            new Range(text)
            };
        }

        #endregion
    }

    /// <summary>
    /// 一个最小文字单元
    /// </summary>
    public class Range
    {
        // public ConvertTextFunc ConvertText;

        public SCRIPT_ITEM Item;

        public SCRIPT_ANALYSIS a;   // 从 this.Item.a 复制过来

        // 记载 ScriptShape() 实际用到的字体
        public Font Font { get; set; }

        private string _text;
        public string Text
        {
            get
            {
                return _text;
            }
            set
            {
                _text = value;
                _displayText = null; // 清除 DisplayText 缓存
            }
        }

        private string _displayText;
        public string DisplayText
        {
            get
            {
                /*
                if (_displayText == null)
                {
                    if (ConvertText != null)
                        _displayText = ConvertText(Text);
                    else
                        _displayText = Text; // 如果没有 ConvertText 委托，直接返回 Text
                }
                */
                return _displayText;
            }
            set
            {
                _displayText = value;
            }
        }

        public int Left; // 左边界 x 坐标
        public int PixelWidth; // 该 Range 的像素宽度

        public int Y; // 该 Range 的左上角 y 坐标

        public SCRIPT_VISATTR[] sva; // ScriptShape() 返回的视觉属性
        public ushort[] glfs;
        public int[] advances; // ScriptPlace() 返回的 advances
        public ushort[] logClust; // ScriptShape() 返回的 logClust
        public GOFFSET[] pGoffset; // ScriptPlace() 返回的 pGoffset
        public ABC pABC;

        public Range()
        {
            Text = "";
        }

        public Range(string text)
        {
            Text = text;
        }
    }

}
