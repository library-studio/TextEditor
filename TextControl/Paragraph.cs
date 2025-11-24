using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using Vanara.Extensions.Reflection;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.Usp10;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 一个自然段结构
    /// 由若干 Line 构成
    /// </summary>
    public class Paragraph : IBox
    {
        // public ConvertTextFunc ConvertText { get; set; }

        // public Color BackColor { get; set; } = Color.Transparent;

        List<Line> _lines = new List<Line>();

        static internal SCRIPT_PROPERTIES[] sp = null;
        static internal SCRIPT_DIGITSUBSTITUTE sub;
        static internal SCRIPT_CONTROL sc;
        static internal SCRIPT_STATE ss;

        public Paragraph()
        {

        }

        public void Paint(SafeHDC dc,
                        IContext context,
            int x,
            int y,
            Rectangle clipRect,
            int blockOffs1,
            int blockOffs2,
            int virtual_tail_length)
        {
            // var old_mode = Gdi32.SetBkMode(dc, Gdi32.BackgroundMode.TRANSPARENT); // 设置背景模式为透明
            /*
            if (_average_char_width == 0)
                _average_char_width = (int)ComputeAverageWidth(dc, _default_font);
            */

            int current_start_offs = 0;
            var block_start = Math.Min(blockOffs1, blockOffs2);
            var block_end = Math.Max(blockOffs1, blockOffs2);
            int i = 0;
            foreach (var line in _lines)
            {
                // 剪切区域下方的部分不必参与循环了
                if (y >= clipRect.Bottom)
                    break;

                int paragraph_width = line.GetPixelWidth();
                int paragraph_height = line.GetPixelHeight();
                var rect = new Rectangle(x, y, paragraph_width, paragraph_height);
                if (clipRect.IntersectsWith(rect))
                {
                    line.Paint(dc,
                        context,
                        x,
                        y,
                        clipRect,
                        block_start - current_start_offs,
                        block_end - current_start_offs,
                        i == _lines.Count - 1 ? virtual_tail_length : 0);
                }
                y += line.GetPixelHeight();
                current_start_offs += line.TextLength;
                i++;
            }
        }

        public bool IntersectsWith(Rectangle this_rect, Rectangle rect)
        {
            if (rect.X < this_rect.X + this_rect.Width && this_rect.X < rect.X + rect.Width && rect.Y < this_rect.Y + this_rect.Height)
            {
                return this_rect.Y < rect.Y + rect.Height;
            }

            return false;
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
            update_rect = System.Drawing.Rectangle.Empty;
            scroll_rect = System.Drawing.Rectangle.Empty;
            scroll_distance = 0;
            replaced = "";

            // 先分别定位到 start 所在的 Line，和 end 所在的 Line
            // 观察被替换的范围 start - end，跨越了多少个 Line
            // 对 text 进行 Line 切分。把这些 Line 替换为由切割后的 text 构造的零到多个新 Line

            var old_lines = FindLines(
                _lines,
                start,
                out string left_text,
                end,
                out string right_text,
                out int first_line_index,
                out replaced);

            if (start == 0 && end == -1 /*&& first_line_index == -1*/)
            {
                first_line_index = 0;
                // _paragraphs.Clear();
            }

            string content = left_text + text + right_text;

            var new_lines = new List<Line>();
            int max_pixel_width = pixel_width;
            if (string.IsNullOrEmpty(content) == false)
            {
                new_lines = BuildLines(
                    dc,
                    // this.ConvertText,
                    content,
                    pixel_width,
                    context,
                    out max_pixel_width);
            }

            if (old_lines.Count > 0)
                _lines.RemoveRange(first_line_index, old_lines.Count);
            if (new_lines.Count > 0)
            {
                Debug.Assert(first_line_index >= 0);
                _lines.InsertRange(first_line_index, new_lines);
            }

            int max = old_lines.Count == 0 ? 0 : old_lines.Max(p => p.GetPixelWidth());
            if (max > max_pixel_width)
                max_pixel_width = max;

            // update_rect 用 old_lines 和 new_lines 两个矩形的较大一个算出
            // 矩形宽度最后用 max_pixel_width 矫正一次
            int old_h = old_lines.Sum(p => p.GetPixelHeight());
            int new_h = new_lines.Sum(p => p.GetPixelHeight());
            int y = SumHeight(_lines, first_line_index);

            update_rect = new Rectangle(0,
                y,
                max_pixel_width,
                Math.Max(old_h, new_h));

            scroll_distance = new_h - old_h;
            if (scroll_distance != 0)
            {
                int move_height = SumHeight(_lines, first_line_index, _lines.Count - first_line_index);
                scroll_rect = new Rectangle(0,
        y + old_h,
        max_pixel_width,
        move_height);
            }
            return max_pixel_width;
        }

        static int SumHeight(List<Line> lines, int count)
        {
            int height = 0;
            for (int i = 0; i < count; i++)
            {
                height += lines[i].GetPixelHeight();
            }
            return height;
        }

        static int SumHeight(List<Line> lines, int start, int count)
        {
            int height = 0;
            for (int i = start; i < start + count; i++)
            {
                height += lines[i].GetPixelHeight();
            }
            return height;
        }


        // 根据 offs 范围获得相关的 Line 列表
        // parameters:
        //      left_text [out] 返回命中的第一个 Line 处于 start 位置之前的局部文字内容
        //      right_text [out] 返回命中的最后一个 Line 处于 end 位置之后的局部文字内容
        //      first_line_index   [out] 命中的第一个 Line 的 index。若为 -1，表示没有命中的
        //      replaced    [out] 返回 start 和 end 之间即将被替换的部分内容
        // return:
        //      命中的 Line 列表
        public static List<Line> FindLines(
            List<Line> _lines,
            int start,
            out string left_text,
            int end,
            out string right_text,
            out int first_line_index,
            out string replaced)
        {
            left_text = "";
            right_text = "";
            first_line_index = 0;
            replaced = "";

            if (end == -1)
                end = int.MaxValue;

            Debug.Assert(start <= end);
            if (start > end)
                throw new ArgumentException($"start {start} must less than end {end}");

            StringBuilder replaced_part = new StringBuilder();

            var lines = new List<Line>();
            int offs = 0;
            int i = 0;
            foreach (var line in _lines)
            {
                if (offs > end)
                    break;

                int line_text_length = line.TextLength;

                // 命中
                if (offs <= end && offs + line_text_length >= start)
                {
                    var text = line.MergeText();
                    if (lines.Count == 0)
                    {
                        left_text = text.Substring(0, start - offs);
                        first_line_index = i;
                    }

                    // right_text 不断被更新，只要留下最后一次的值即可
                    if (offs + line_text_length >= end)
                        right_text = text.Substring(end - offs);
                    lines.Add(line);

                    {
                        var part_start = Math.Max(start - offs, 0);
                        var part_length = line_text_length - part_start;
                        if (offs + line_text_length >= end)
                            part_length = (end - offs) - part_start;
                        if (part_length > 0)
                            replaced_part.Append(text.Substring(part_start, part_length));
                    }
                }

                offs += line_text_length;
                i++;
            }

            replaced = replaced_part.ToString();
            return lines;
        }

        static List<Line> BuildLines(
    SafeHDC dc,
    // ConvertTextFunc func_convertText,
    string content,
    int pixel_width,
    IContext context,
    out int max_pixel_width)
        {
            InitializeUspEnvironment();

            max_pixel_width = 0;

            if (content == null)
            {
                return new List<Line>();
            }

            if (string.IsNullOrEmpty(content))
            {
                return new List<Line> { new Line() };
            }

            string[] contents = null;
            if (context?.SplitRange != null)
                contents = context?.SplitRange(content);
            else
                contents = new string[] { content };

            List<SCRIPT_ITEM> items = new List<SCRIPT_ITEM>();
            List<string> chunks = new List<string>();
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
                    sc,
                    ss,
                    pItems,
                    out int pcItems);
                result.ThrowIfFailed();

                Array.Resize(ref pItems, pcItems);
                /*
                for (int i = 0; i < pcItems; i++)
                {
                    var item = pItems[i];
                    if (sp[item.a.eScript].fComplex)
                    {
                        // requiring glyph shaping
                    }
                    else
                    {

                    }
                }
                */

                int start_index = 0;
                for (int i = 0; i < pItems.Length; i++)
                {
                    var item = pItems[i];

                    // 析出本 item 的文字
                    string str = "";
                    if (i >= pItems.Length - 1)
                        str = segment.Substring(start_index);
                    else
                    {
                        int length = pItems[i + 1].iCharPos - item.iCharPos;
                        str = segment.Substring(start_index, length);
                    }

                    chunks.Add(str);
                    start_index += str.Length;
                }

                items.AddRange(pItems);
            }

            var lines = SplitLines(dc,// e.Graphics.GetHdc(),
                context,
items.ToArray(),
chunks,
pixel_width,
out int new_width);
            if (new_width > max_pixel_width)
                max_pixel_width = new_width;
            return lines;

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
            InitializeUspEnvironment();

            this.Clear();

            if (content == null)
            {
                return 0;
            }

            if (string.IsNullOrEmpty(content))
            {
                _lines.Add(new Line());
                return 0;
            }

            string[] contents = null;
            if (context?.SplitRange != null)
                contents = context.SplitRange(content);
            else
                contents = new string[] { content };

            List<SCRIPT_ITEM> items = new List<SCRIPT_ITEM>();
            List<string> chunks = new List<string>();
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
                    sc,
                    ss,
                    pItems,
                    out int pcItems);
                result.ThrowIfFailed();

                Array.Resize(ref pItems, pcItems);
                /*
                for (int i = 0; i < pcItems; i++)
                {
                    var item = pItems[i];
                    if (sp[item.a.eScript].fComplex)
                    {
                        // requiring glyph shaping
                    }
                    else
                    {

                    }
                }
                */

                int start_index = 0;
                for (int i = 0; i < pItems.Length; i++)
                {
                    var item = pItems[i];

                    // 析出本 item 的文字
                    string str = "";
                    if (i >= pItems.Length - 1)
                        str = segment.Substring(start_index);
                    else
                    {
                        int length = pItems[i + 1].iCharPos - item.iCharPos;
                        str = segment.Substring(start_index, length);
                    }

                    chunks.Add(str);
                    start_index += str.Length;
                }

                items.AddRange(pItems);
            }

            {
                var lines = SplitLines(dc,// e.Graphics.GetHdc(),
                    context,
items.ToArray(),
chunks,
pixel_width,
out int new_width);
                if (new_width > max_pixel_width)
                    max_pixel_width = new_width;
                _lines.AddRange(lines);
            }

            return max_pixel_width;

            string convertText(string t)
            {
                if (context?.ConvertText != null)
                    return context?.ConvertText(t);
                return t;
            }
        }

        static List<Line> SplitLines(SafeHDC dc,
            IContext context,
SCRIPT_ITEM[] pItems,
List<string> chunks,
int line_width,
out int max_pixel_width)
        {
            if (sp == null)
                throw new ArgumentException("Script properties not initialized.");

            max_pixel_width = line_width;
            List<Line> lines = new List<Line>();
            Line line = new Line();
            long start_pixel = 0;

            for (int i = 0; i < pItems.Length; i++)
            {
                var item = pItems[i];

                // 析出本 item 的文字
                string str = chunks[i];

                {
                    Font used_font = null;
                    string text = str;
                    for (; ; )
                    {

                        // 寻找可以 break 的位置
                        // return:
                        //      返回 text 中可以切割的 index 位置。如果为 -1，表示没有找到可以切割的位置。
                        var pos = BreakItem(
                            dc,
                            item,
        context?.ConvertText == null ? text : context.ConvertText(text),
        (line_width == -1 ? int.MaxValue : line_width) - start_pixel,
        start_pixel == 0,
        out int left_width,
        out int abcC,
        ref used_font);

                        // 从 pos 位置开始切割
                        Debug.Assert(pos >= 0 && pos <= text.Length, "pos must be within the bounds of the text string.");

                        // 剩下的右侧空位，连安放一个字符也不够，那么就先换行
                        if (pos == 0 && line.Ranges.Count > 0)
                        {
                            // 折行
                            lines.Add(line);
                            line = new Line();
                            // line.ConvertText = func_convertText;
                            start_pixel = 0;
                            continue;
                        }

                        // 左边部分
                        if (pos > 0)
                        {
                            if (line == null)
                            {
                                line = new Line();
                                // line.ConvertText = func_convertText;
                            }
                            line.Ranges.Add(NewRange(text.Substring(0, pos), left_width));
                            start_pixel += left_width;

                            text = text.Substring(pos);
                            if (string.IsNullOrEmpty(text))
                                break;
                        }

                        if (/*start_pixel >= line_width &&*/ line.Ranges.Count > 0)
                        {
                            // 折行
                            lines.Add(line);
                            line = new Line();
                            // line.ConvertText = func_convertText;
                            start_pixel = 0;

                            /*
                            if (line == null)
                                line = new Line { Ranges = new List<Range>() };

                            line.Ranges.Add(NewRange(text, left_width));
                            start_pixel += (int)left_width;
                            break;
                            */
                        }

                        Range NewRange(string t, int w)
                        {
                            var range = new Range
                            {
                                Item = item,
                                a = item.a,
                                Font = used_font,
                                Text = t,
                                DisplayText = context?.ConvertText?.Invoke(t) ?? t,
                                PixelWidth = w
                            };
                            return range;
                        }
                    }

                }
            }

            if (line != null
                && line.Ranges.Count > 0)
            {
                lines.Add(line);
            }

            max_pixel_width = 0;
            foreach (var current_line in lines)
            {
                Line.LayoutLine(current_line);
                var width = Line.RefreshLine(dc, current_line);
                if (width > max_pixel_width)
                    max_pixel_width = width;
            }

            /*
            // _contentPixelWidth = pixel_width;
            this.SetAutoSizeMode(AutoSizeMode.GrowAndShrink);
            this.AutoScrollMinSize = new Size(pixel_width, line_height * lines.Count);
            */
            return lines;
        }


#if REMOVED
        List<Line> SplitLines(SafeHDC dc,
    SCRIPT_ITEM[] pItems,
    string content,
    int line_width,
    out int max_pixel_width)
        {
            if (sp == null)
                throw new ArgumentException("Script properties not initialized.");

            max_pixel_width = line_width;
            List<Line> lines = new List<Line>();
            Line line = new Line();
            long start_pixel = 0;
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

                {
                    Font used_font = null;
                    string text = str;
                    for (; ; )
                    {

                        // 寻找可以 break 的位置
                        // return:
                        //      返回 text 中可以切割的 index 位置。如果为 -1，表示没有找到可以切割的位置。
                        var pos = BreakItem(
                            dc,
                            item,
        text,
        line_width - start_pixel,
        start_pixel == 0,
        out int left_width,
        out int abcC,
        ref used_font);

                        // 从 pos 位置开始切割
                        Debug.Assert(pos >= 0 && pos <= text.Length, "pos must be within the bounds of the text string.");

                        // 剩下的右侧空位，连安放一个字符也不够，那么就先换行
                        if (pos == 0 && line.Ranges.Count > 0)
                        {
                            // 折行
                            lines.Add(line);
                            line = new Line();
                            start_pixel = 0;
                            continue;
                        }

                        // 左边部分
                        if (pos > 0)
                        {
                            if (line == null)
                                line = new Line();
                            line.Ranges.Add(NewRange(text.Substring(0, pos), left_width));
                            start_pixel += left_width;

                            text = text.Substring(pos);
                            if (string.IsNullOrEmpty(text))
                                break;
                        }

                        if (/*start_pixel >= line_width &&*/ line.Ranges.Count > 0)
                        {
                            // 折行
                            lines.Add(line);
                            line = new Line();
                            start_pixel = 0;

                            /*
                            if (line == null)
                                line = new Line { Ranges = new List<Range>() };

                            line.Ranges.Add(NewRange(text, left_width));
                            start_pixel += (int)left_width;
                            break;
                            */
                        }

                        Range NewRange(string t, int w)
                        {
                            return new Range
                            {
                                Item = item,
                                a = item.a,
                                Font = used_font,
                                Text = t,
                                PixelWidth = w
                            };
                        }
                    }

                }

                start_index += str.Length;
            }

            if (line != null
                && line.Ranges.Count > 0)
            {
                lines.Add(line);
            }

            max_pixel_width = 0;
            foreach (var current_line in lines)
            {
                Line.LayoutLine(current_line);
                var width = Line.RefreshLine(dc, current_line);
                if (width > max_pixel_width)
                    max_pixel_width = width;
            }

            /*
            // _contentPixelWidth = pixel_width;
            this.SetAutoSizeMode(AutoSizeMode.GrowAndShrink);
            this.AutoScrollMinSize = new Size(pixel_width, line_height * lines.Count);
            */
            return lines;
        }

#endif

        // return:
        //      返回 text 中可以切割的 index 位置。如果为 -1，表示没有找到可以切割的位置。
        static int BreakItem(
            SafeHDC dc,
            SCRIPT_ITEM item,
            string str,
            long pixel_width,
            bool is_left_most,
            out int left_width,
            out int abcC,
            ref Font used_font)
        {
            left_width = 0;
            abcC = 0;

            used_font = null;
            var cache = new SafeSCRIPT_CACHE();
            var a = item.a;
            Line.ShapeAndPlace(
                ref a,
                cache,
dc,
str,
out ushort[] glfs,
out int[] piAdvance,
out _,
out ABC pABC,
out _,
out _,
ref used_font);
            //    item.a = a;
            left_width = (int)(pABC.abcA + pABC.abcB + pABC.abcC);
            if (is_left_most && pABC.abcA < 0)
                left_width += -pABC.abcA;
            abcC = pABC.abcC; // 记录 abcC

            if (left_width <= pixel_width)
            {
                return str.Length; // 整个字符串都可以放下
            }

            // var sa = new SCRIPT_ANALYSIS();
            SCRIPT_LOGATTR[] array = new SCRIPT_LOGATTR[str.Length];
            ScriptBreak(str, str.Length, a, array);

            int start = 0;
            start += pABC.abcA;
            int delta = 0;
            if (pABC.abcC < 0)
                delta = -pABC.abcC;
            for (int i = 0; i < piAdvance.Length; i++)
            {
                int tail = start + piAdvance[i];
                if (i >= (is_left_most ? 1 : 0)   // 避免 i == 0 时返回。确保至少有一个字符被切割
                    && tail + delta >= pixel_width
                    && (array[i].fSoftBreak || array[i].fWhiteSpace || array[i].fCharStop))
                {
                    left_width = start;
                    return i;
                }
                start += piAdvance[i];
            }

            return str.Length;
        }


#if REMOVED
        // parameters:
        //      block_start     选中范围的开始偏移量。
        //                      以当前 line 的左边界为 0
        //                      如果大于本行文字长度，表示未选中本行     
        //      block_end       选中范围的结束偏移量
        //                      以当前 line 的左边界为 0
        //                      如果小于 0，表示未选中本行     
        //      virtual_tail_length 行末虚拟尾部字符个数。如果这个尾部处在选择范围，需要显示为选择背景色
        void DisplayLine(
            SafeHDC hdc,
            Line line,
            int x,
            int y,
            // int _line_height,
            int block_start,
            int block_end,
            int virtual_tail_length)
        {
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
                foreach (var range in line.Ranges)   // piVisualToLogical
                {
                    // 绘制选中范围的背景色
                    if (block_start <= range.Text.Length && block_end >= 0)
                    {
                        var tail_in_block = virtual_tail_length > 0
                            && block_start < range.Text.Length + virtual_tail_length && block_end >= range.Text.Length;
                        var block_rect = GetBlockRect(range,
                            x + range.Left,
                            y,
                            _line_height,
                            block_start,
                            block_end);
                        DrawSolidRectangle(hdc,
                            block_rect.left,
                            block_rect.top,
                            block_rect.right + (tail_in_block ? _average_char_width : 0),
                            block_rect.bottom,
                            new COLORREF((uint)SystemColors.Highlight.ToArgb()));

                        // clipping 矩形的左右进行微调。避免斜体字的某些笔画伸出去的部分被显示成不同的颜色
                        var left_delta = range.pABC.abcA;
                        var right_delta = range.pABC.abcC;
                        if (block_start <= 0 && left_delta < 0)
                            block_rect.left -= -left_delta + 1; // 左侧空白
                        if (block_end >= range.Text.Length && right_delta < 0)
                            block_rect.right += -right_delta + 1; // 右侧空白

                        block_rects[i] = block_rect; // 记录块背景矩形
                        full_flags[i] = (block_start <= 0 && block_end >= range.Text.Length); // 标记本 Range 是否全选
                    }

                    block_start -= range.Text.Length;
                    block_end -= range.Text.Length;

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
                        DrawSolidRectangle(hdc,
                            block_rect.left,
                            block_rect.top,
                            block_rect.right,
                            block_rect.bottom,
                            new COLORREF((uint)SystemColors.Highlight.ToArgb()));
                    }
                }
            }

            // 再绘制文本
            foreach (var index in line.piVisualToLogical)   // piVisualToLogical
            {
                var range = line.Ranges[index];
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
                item_rect.left = range.Left;
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
                        if (full_block == false || block_rect == null)
                        {
                            var result = ScriptTextOut(hdc,
                                            cache,
                                            range.Left,
                                            y + _line_height - (int)GetAscentPixel(used_font),
                                            fuOptions,
                                            item_rect,   // [In, Optional] PRECT lprc,
                                            range.a,  // line.Item.a, // in SCRIPT_ANALYSIS psa,
                                            range.Text,  //  [Optional, MarshalAs(UnmanagedType.LPWStr)] string ? pwcReserved,
                                            iReserved,  //  [Optional] int iReserved,
                                            range.glfs,   // [In, MarshalAs(UnmanagedType.LPArray)] ushort[] pwGlyphs, 
                                            range.glfs.Length,    // int cGlyphs,
                                            range.advances,  // [In, MarshalAs(UnmanagedType.LPArray)] int[] piAdvance,
                                            null,   // [In, Optional, MarshalAs(UnmanagedType.LPArray)] int[] ? piJustify,
                                            range.pGoffset[0]); // in GOFFSET pGoffset); 
                            result.ThrowIfFailed();
                        }

                        // 第二次显示块部分文字，用 Highlight Color
                        if (block_rect != null)
                        {
                            var old_color = Gdi32.SetTextColor(hdc, new COLORREF((uint)SystemColors.HighlightText.ToArgb())); // 设置文本颜色为黑色
                            //var old_bk_color = Gdi32.SetBkColor(hdc, new COLORREF((uint)SystemColors.Highlight.ToArgb())); // 设置文本颜色为黑色
                            try
                            {
                                var ret = ScriptTextOut(hdc,
                    cache,
                    range.Left,
                    y + _line_height - (int)GetAscentPixel(used_font),
                    (int)Gdi32.ETO.ETO_CLIPPED, // | (int)Gdi32.ETO.ETO_OPAQUE,
                    block_rect,   // [In, Optional] PRECT lprc,
                    range.a,  // line.Item.a, // in SCRIPT_ANALYSIS psa,
                    range.Text,  //  [Optional, MarshalAs(UnmanagedType.LPWStr)] string ? pwcReserved,
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

                        // x_offset += line.PixelWidth;    // pABC.abcA + pABC.abcB + pABC.abcC;
                    }
                }
                finally
                {
                    Gdi32.DeleteFont(font_handle);
                }

            }
        }

#endif


#if REMOVED
        void ShapeAndPlace(
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
#endif

        public void Clear()
        {
            _lines.Clear();
        }

        // 根据单行行高计算出 Paragraph 总的像素高度
        public int GetPixelHeight(/*int line_height*/)
        {
            if (_lines == null)
                return 0;
            if (_lines.Count == 0)
                return Line._line_height;
            // return lines.Sum(line => line.GetPixelHeight());
            return _lines.Count * Line._line_height;
        }

        public int GetPixelWidth()
        {
            return _lines.Count == 0 ? 0 : _lines.Max(l => l.GetPixelWidth());
        }

        public static void InitializeUspEnvironment()
        {
            if (sp != null)
                return;

            var result = ScriptGetProperties(out sp);
            result.ThrowIfFailed();

            result = ScriptRecordDigitSubstitution(LCID.LOCALE_CUSTOM_DEFAULT,
                out sub);
            result.ThrowIfFailed();

            result = ScriptApplyDigitSubstitution(sub,
                out sc,
                out ss);
            result.ThrowIfFailed();

            // _cache = new SafeSCRIPT_CACHE();
        }


        #region HitTest

        // parameters:
        //      int x   注意这是 Paragraph 内文档坐标
        public HitInfo HitTest(int x,
            int y/*,
            int _line_height*/)
        {
            var result = new HitInfo();
            int current_y = 0;
            int offs = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                bool isLastLine = (i == _lines.Count - 1);
                if (y < current_y)
                    break;
                if (y >= current_y && (y < current_y + Line._line_height || isLastLine))
                {
#if REMOVED
                    var ret = line.HitTest(x,
                        out int hit_offs,
                        out int hit_x_in_range);
                    return new HitInfo
                    {
                        X = hit_x_in_range,
                        Y = current_y,
                        ChildIndex = i,
                        // RangeIndex = hit_range_index,
                        Area = y < current_y + _line_height ? Area.Text : Area.BottomBlank,
                        TextIndex = hit_offs,
                        Offs = offs + hit_offs,
                        LineHeight = _line_height,
                    };
#endif
                    var result_info = line.HitTest(x,
    current_y);
                    return new HitInfo
                    {
                        X = result_info.X,
                        Y = current_y + result_info.Y,
                        ChildIndex = i,
                        // RangeIndex = hit_range_index,
                        TextIndex = result_info.Offs,
                        Offs = offs + result_info.Offs,
                        LineHeight = result_info.LineHeight,
                        Area = y < current_y + Line._line_height ? Area.Text : Area.BottomBlank,
                    };
                }

                current_y += Line._line_height;
                offs += line.TextLength;
            }

            // 空白内容
            return new HitInfo { Area = Area.BottomBlank };
        }


#if REMOVED
        // 根据 info 对象的 .ChildIndex  和 .TextIndex 计算出全局偏移量
        public int GetGlobalOffs(HitInfo info)
        {
            int offs = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                if (i == info.ChildIndex)
                {
                    return offs + info.TextIndex;
                }
                offs += line.TextLength; // 累加本行的文字长度
            }

            return offs; // 没有行命中，所以 info.TextIndex 不采纳
        }
#endif

        // **** 准备废弃
        // 以全局偏移量为参数，获得点击位置
        // TODO: 改造为调用 Line::HitByGlobalOffs
        public HitInfo HitByGlobalOffs(int offs_param,
            bool trailing)
        {
            HitInfo info = new HitInfo();
            int offs = 0;
            Line line = null;
            Range range = null;
            int start_x = 0;
            int start_y = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                // info.RangeIndex = 0;
                line = _lines[i];
                var line_start_offs = offs; // 本行的起始偏移量
                for (int j = 0; j < line.Ranges.Count; j++)
                {
                    range = line.Ranges[j];
                    start_x = range.Left;
                    if (offs + range.Text.Length >= offs_param)
                    {
                        info.ChildIndex = i;
                        // info.RangeIndex = j;
                        info.TextIndex = offs_param - line_start_offs;
                        info.Area = Area.Text;
                        goto END1;
                    }
                    offs += range.Text.Length;

                    // info.RangeIndex++;
                }
                if (i >= _lines.Count - 1)
                    break;

                start_y += Line._line_height;

                info.ChildIndex++;
            }

            info.Area = Area.BottomBlank;

        END1:
            if (range != null)
            {
                var result = ScriptCPtoX(offs_param - offs,
        trailing,  // isRightBlank ? true : trailing != 0,
        range.Text.Length,
        range.glfs.Length,
        range.logClust,
        range.sva,
        range.advances,
        range.a,
        out int hit_x_in_range);
                result.ThrowIfFailed();

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

#if REMOVED
        // 根据 Caret Offs 进行移动
        // parameters:
        //      direction   -1 向左 0 原地 1 向右
        public HitInfo MoveByOffs(int offs_param,
            int direction)
        {
            HitInfo info = new HitInfo();
            int offs = 0;
            Line line = null;
            Range range = null;
            int start_x = 0;
            int start_y = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                // info.RangeIndex = 0;
                line = _lines[i];
                var line_start_offs = offs; // 本行的起始偏移量
                for (int j = 0; j < line.Ranges.Count; j++)
                {
                    range = line.Ranges[j];
                    start_x = range.Left;
                    if (offs + range.Text.Length >= offs_param)
                    {
                        info.ChildIndex = i;
                        // info.RangeIndex = j;
                        info.TextIndex = offs_param - line_start_offs;
                        info.Area = Area.Text;
                        goto END1;
                    }
                    offs += range.Text.Length;

                    // info.RangeIndex++;
                }
                if (i >= _lines.Count - 1)
                    break;

                start_y += _line_height;

                info.ChildIndex++;
            }

            info.Area = Area.BottomBlank;

        END1:
            if (range != null)
            {
                var result = ScriptCPtoX(offs_param - offs,
        trailing,  // isRightBlank ? true : trailing != 0,
        range.Text.Length,
        range.glfs.Length,
        range.logClust,
        range.sva,
        range.advances,
        range.a,
        out int hit_x_in_range);
                result.ThrowIfFailed();

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

        // 根据 Caret Offs 进行移动
        // parameters:
        //      offs    插入符在当前 Paragraph 中的偏移
        //      direction   -1 向左 0 原地 1 向右
        // return:
        //      -1  越过左边
        //      0   成功
        //      1   越过右边
        public int MoveByOffs(int offs_param,
            int direction,
            out HitInfo info)
        {
            info = new HitInfo();

            var infos = new List<HitInfo>();

            int offs = 0;
            Line line = null;
            int start_y = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                // info.RangeIndex = 0;
                line = _lines[i];
                var line_text_length = line.TextLength;
                if (offs_param + direction >= offs && offs_param + direction <= offs + line_text_length)
                {
                    var ret = line.MoveByOffs(offs_param - offs,
                        direction,
                        out HitInfo hit_info);
                    if (ret == 0)
                    {
                        Debug.Assert(ret == 0);

                        var temp_info = new HitInfo
                        {
                            X = hit_info.X,
                            Y = hit_info.Y + start_y,
                            Area = hit_info.Area,
                            ChildIndex = i,
                            TextIndex = hit_info.Offs,
                            Offs = offs + hit_info.Offs,
                            LineHeight = hit_info.LineHeight,
                        };

                        if (direction >= 0)
                        {
                            info = temp_info;
                            return 0;
                        }
                        // 暂时不返回，继续匹配后面可能匹配的位置
                        infos.Add(temp_info);
                    }
                }
                else
                {
                    // 如果早先发生过匹配，则表明此时发生不匹配以后，再往后不可能发生匹配了，
                    // 于是及时返回，避免多余的后继匹配操作
                    if (infos.Count > 0)
                    {
                        info = infos[infos.Count - 1];
                        return 0;
                    }
                }

                offs += line_text_length;

                start_y += Line._line_height;
            }

            if (infos.Count > 0)
            {
                info = infos[infos.Count - 1];
                return 0;
            }

            // 没有任何 Line 的情况
            if (_lines.Count == 0
                && offs + direction == 0)
            {
                info.X = 0;
                info.Y = 0;
                info.ChildIndex = 0;
                info.Offs = offs + direction;
                info.TextIndex = 0;
                info.Area = Area.Text;
                info.LineHeight = Line._line_height;
                return 0;
            }

            info.Area = Area.BottomBlank;
            return 1;
        }

#if REMOVED
        // 注意函数中不要改变 info 内容
        public bool CanDown(HitInfo info)
        {
            if (info.ChildIndex < this.LineCount)
                return true;
            return false;
        }

        // 注意函数中不要改变 info 内容
        public bool CanUp(HitInfo info)
        {
            if (info.ChildIndex > 0)
                return true;
            return false;
        }
#endif

        // parameters:
        //      x   最近一次左右移动插入符之后，插入符的 x 位置。注意，并不一定等于当前插入符的 x 位置
        //      y   当前插入符的 y 位置
        //      info    [in,out]插入符位置参数。
        //              [in] 时，info.X info.Y 被利用，其它成员没有被利用
        // return:
        //      true    成功。新的插入符位置返回在 info 中了
        //      false   无法移动。注意此时 info 中返回的内容无意义
        public bool CaretMoveDown(
            int x,
            int y,
            out HitInfo info)
        {
            info = new HitInfo();
            y += Line._line_height;
            if (y >= this.GetPixelHeight())
                return false;
            info = this.HitTest(x, y);
            return true;
        }

        public bool CaretMoveUp(
    int x,
    int y,
    out HitInfo info)
        {
            info = new HitInfo();
            y -= Line._line_height;
            if (y < 0)
                return false;
            info = this.HitTest(x, y);
            return true;
        }

        #endregion

        public int LineCount
        {
            get
            {
                return _lines.Count;
            }
        }

        public string MergeText(int start = 0, int end = int.MaxValue)
        {
            StringBuilder builder = new StringBuilder();
            int offs = 0;
            foreach (var line in _lines)
            {
                var current_length = line.TextLength;
                builder.Append(line.MergeText(start - offs, end - offs));
                offs += current_length;
                if (offs > end)
                    break;
            }

            return builder.ToString();
        }

        public int TextLength
        {
            get
            {
                if (_lines == null)
                    return 0;
                int length = 0;
                foreach (var line in _lines)
                {
                    length += line.TextLength;
                }
                return length;
            }
        }

        #region 配合测试的代码

        public Paragraph(string text)
        {
            _lines = new List<Line>() {
            new Line(text)
            };
        }

        #endregion
    }
}
