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
    /// 一个自然段结构
    /// 由若干 Line 构成
    /// </summary>
    public class Paragraph : IBox, IDisposable
    {
        public string Name { get; set; }

        public IBox Parent { get; set; }

        List<Line> _lines = new List<Line>();

        static internal SCRIPT_PROPERTIES[] sp = null;
        static internal SCRIPT_DIGITSUBSTITUTE sub;
        static internal SCRIPT_CONTROL sc;
        static internal SCRIPT_STATE ss;

        public ColorCache ColorCache = new ColorCache();

        public Paragraph(IBox parent)
        {
            Parent = parent;
        }

        public void Paint(
            IContext context,
            SafeHDC dc,
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

            if (this._lines.Count == 0)
            {
                // 绘制一个结束符
                var rect = new Rectangle(x,
                    y,
                    FontContext.DefaultReturnWidth,
                    FontContext.DefaultFontHeight);
                if (clipRect.IntersectsWith(rect))
                {
                    // 根据是否处在 text block 中决定使用什么背景色
                    // 要么绘制结束符，要么清除结束符
                    Color back_color;
                    if (Utility.InRange(0, blockOffs1, blockOffs2))
                    {
                        // back_color = context?.GetBackColor?.Invoke(this, true) ?? SystemColors.Highlight;
                        back_color = this.ColorCache
                            .GetBackColor(context?.GetBackColor,
                            this,
                            true);
                    }
                    else
                    {
                        // back_color = context?.GetBackColor?.Invoke(this, false) ?? Color.Transparent;
                        back_color = this.ColorCache
                            .GetBackColor(context?.GetBackColor,
                            this,
                            false);
                    }
                    if (back_color != Color.Transparent)
                    {
                        Line.DrawSolidRectangle(dc,
                        rect.Left,
                        rect.Top,
                        rect.Right,
                        rect.Bottom,
                        back_color,
                        clipRect);
                    }
                }
                return;
            }

            int current_start_offs = 0;
            var block_start = Math.Min(blockOffs1, blockOffs2);
            var block_end = Math.Max(blockOffs1, blockOffs2);
            int i = 0;
            foreach (var line in _lines)
            {
                // 剪切区域下方的部分不必参与循环了
                if (y >= clipRect.Bottom)
                    break;

                int line_width = line.GetPixelWidth();
                int line_height = line.GetPixelHeight();
                var rect = new Rectangle(x, y, line_width, line_height);

                bool is_tail = i == _lines.Count - 1;

                // 把 rect 右侧扩大一点，避免和 clipRect 交叉探测不到
                if (is_tail)
                    rect.Width += FontContext.DefaultReturnWidth;
                if (clipRect.IntersectsWith(rect))
                {
                    line.Paint(
                        context,
                        dc,
                        x,
                        y,
                        clipRect,
                        block_start - current_start_offs,
                        block_end - current_start_offs,
                        is_tail ? virtual_tail_length : 0);
                }
                y += line_height;
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

        public ReplaceTextResult ReplaceText(
            IContext context,
            SafeHDC dc,
            int start,
            int end,
            string text,
            int pixel_width)
        {
            var result = new ReplaceTextResult();
            /*
            update_rect = System.Drawing.Rectangle.Empty;
            scroll_rect = System.Drawing.Rectangle.Empty;
            scroll_distance = 0;
            replaced = "";
            */

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
                out string replaced);

            if (start == 0 && end == -1 /*&& first_line_index == -1*/)
            {
                first_line_index = 0;
                // _paragraphs.Clear();
            }
            result.ReplacedText = replaced;

            string content = left_text + text + right_text;

            var new_lines = new List<Line>();
            int max_pixel_width = pixel_width;
            if (string.IsNullOrEmpty(content) == false)
            {
                new_lines = BuildLines(
                    context,
                    dc,
                    // this.ConvertText,
                    content,
                    pixel_width,
                    this,
                    out max_pixel_width);
            }

            // offs 为(Paragraph)从头开始计算的第一个不同 char 的 offs
            var index = SameLines(old_lines, new_lines, out int offs);
            if (index > 0)
            {
                if (index < old_lines.Count && index < new_lines.Count)
                    offs += SameChars(old_lines[index], new_lines[index]);
                old_lines.RemoveRange(0, index);
                RemoveLines(new_lines, 0, index);   // 刚创建的 field 要删除必须要 Dispose()
                first_line_index += index;
            }

            // TODO: 修改前的最后一个 Line 末尾的回车换行符号区域。
            // 修改后的最后一个 Line 末尾的回车换行符号区域，都应包含到失效区中

            int max = old_lines.Count == 0 ? 0 : old_lines.Max(p => p.GetPixelWidth());
            if (max > max_pixel_width)
                max_pixel_width = max;

            if (old_lines.Count > 0)
                RemoveLines(_lines, first_line_index, old_lines.Count);
            if (new_lines.Count > 0)
            {
                Debug.Assert(first_line_index >= 0);
                _lines.InsertRange(first_line_index, new_lines);
            }

            // update_rect 用 old_lines 和 new_lines 两个矩形的较大一个算出
            // 矩形宽度最后用 max_pixel_width 矫正一次
            int old_h = old_lines.Sum(p => p.GetPixelHeight());
            int new_h = new_lines.Sum(p => p.GetPixelHeight());
            int y = SumHeight(_lines, first_line_index);

            result.UpdateRect = new Rectangle(0,
                y,
                max_pixel_width,
                Math.Max(old_h, new_h));

            result.ScrolledDistance = new_h - old_h;
            if (result.ScrolledDistance != 0)
            {
                int move_height = SumHeight(_lines, first_line_index, _lines.Count - first_line_index);
                result.ScrollRect = new Rectangle(0,
        y + old_h,
        max_pixel_width,
        move_height);
            }

            ProcessBaseline();
            result.MaxPixel = max_pixel_width;
            return result;
        }

        // 检查两组行，前方有连续多少行互相没有不同
        // parameters:
        //      offs    [out] 返回第一个不同的 offs
        // return:
        //      返回第一个不同的 index
        static int SameLines(List<Line> lines1,
            List<Line> lines2,
            out int offs)
        {
            offs = 0;
            var min_count = Math.Min(lines1.Count, lines2.Count);
            for (int i = 0; i < min_count; i++)
            {
                var text1 = lines1[i].MergeText();
                if (text1 != lines2[i].MergeText())
                    return i;
                offs += text1.Length;
            }
            Debug.Assert(min_count <= lines1.Count);
            Debug.Assert(min_count <= lines2.Count);
            return min_count;
        }

        // 比较两行文字，第一处不一样的 char index
        static int SameChars(Line line1, Line line2)
        {
            var text1 = line1.MergeText();
            var text2 = line2.MergeText();
            var min_length = Math.Min(text1.Length, text2.Length);
            for (int i = 0; i < min_length; i++)
            {
                if (text1[i] != text2[i])
                    return i;
            }
            return min_length;
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

        // parameters:
        //      parent  为 Line 设置 _parent
        static List<Line> BuildLines(
    IContext context,
    SafeHDC dc,
    // ConvertTextFunc func_convertText,
    string content,
    int pixel_width,
    IBox parent,
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
                return new List<Line> { new Line(parent) };
            }

#if REMOVED
            var contents = context?.SplitRange?.Invoke(parent, content)
                ?? new Segment[] {
                    new Segment
                    {
                        Text = content,
                        Tag = null
                    }
                };

            var items = new List<SCRIPT_ITEM>();
            var chunks = new List<string>();
            var tags = new List<object>();
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
                    tags.Add(seg.Tag);
                    start_index += str.Length;
                }

                items.AddRange(pItems);
            }

            Debug.Assert(chunks.Count == tags.Count);
            var lines = SplitLines(dc,// e.Graphics.GetHdc(),
                context,
items.ToArray(),
chunks,
tags,
pixel_width,
parent,
out int new_width);

#endif
            var items = Itemize(
    context,
    dc,
    content,
    parent,
    true,
    null);
            var lines = SplitLines(dc,// e.Graphics.GetHdc(),
    context,
items,
pixel_width,
parent,
out int new_width);
            if (new_width > max_pixel_width)
                max_pixel_width = new_width;
            return lines;

            string convertText(string t)
            {
                return context?.ConvertText?.Invoke(t) ?? t;
            }
        }

        // 包含 Chunk 文本，避免再到 SCRIPT_ITEM 中去取 .iCharPos
        public class ScriptItem
        {
            public SCRIPT_ITEM Item { get; set; }
            public string Chunk { get; set; }

            public object Tag { get; set; }
        }

        public static List<ScriptItem> Itemize(
            IContext context,
            SafeHDC dc,
            string content,
            IBox parent,
            bool split_range,
            object default_tag)
        {
            // InitializeUspEnvironment();
            if (content == null)
            {
                return new List<ScriptItem>();
            }

            if (string.IsNullOrEmpty(content))
            {
                return new List<ScriptItem>();
            }

            var contents = split_range && context?.SplitRange != null ? context?.SplitRange?.Invoke(parent, content)
                : new Segment[] {
                    new Segment
                    {
                        Text = content,
                        Tag = default_tag
                    }
                };

            var result_items = new List<ScriptItem>();
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

                    result_items.Add(new ScriptItem
                    {
                        Item = item,
                        Chunk = str,
                        Tag = seg.Tag
                    });
                    start_index += str.Length;
                }
            }

            return result_items;

            string convertText(string t)
            {
                return context?.ConvertText?.Invoke(t) ?? t;
            }
        }


        float _baseLine;
        float _below;

        public float BaseLine
        {
            get
            {
                return _baseLine;
            }
        }

        public float Below
        {
            get
            {
                return _below;
            }
        }

        // 处理整个 Paragraph 的基线
        // 以第一行 Line 的基线为基线
        void ProcessBaseline()
        {
            if (this._lines == null || this._lines.Count == 0)
            {
                _baseLine = 0;
                _below = 0;
                return;
            }
            _baseLine = this._lines[0].BaseLine;
            _below = this._lines[0].Below;  // TODO: 加上除第一行以外的所有行的高度?
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
                _lines.Add(new Line(this));
                return 0;
            }

#if REMOVED
            Segment[] contents = context?.SplitRange?.Invoke(this, content)
                ?? new Segment[] {
                    new Segment
                    {
                        Text = content,
                        Tag = null
                    }
                };

            var items = new List<SCRIPT_ITEM>();
            var chunks = new List<string>();
            var tags = new List<object>();
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
                    tags.Add(seg.Tag);
                    start_index += str.Length;
                }

                items.AddRange(pItems);
            }

            {
                Debug.Assert(chunks.Count == tags.Count);
                var lines = SplitLines(dc,// e.Graphics.GetHdc(),
                    context,
items.ToArray(),
chunks,
tags,
pixel_width,
this,
out int new_width);
                if (new_width > max_pixel_width)
                    max_pixel_width = new_width;
                _lines.AddRange(lines);
            }
#endif
            int max_pixel_width = 0;

            var items = Itemize(
context,
dc,
content,
this,
true,
null);
            var lines = SplitLines(dc,// e.Graphics.GetHdc(),
    context,
items,
pixel_width,
this,
out int new_width);
            if (new_width > max_pixel_width)
                max_pixel_width = new_width;
            _lines.AddRange(lines);

            ProcessBaseline();
            return max_pixel_width;

            string convertText(string t)
            {
                return context?.ConvertText?.Invoke(t) ?? t;
            }
        }

        // parameters:
        //      chunkes 分片的原始文字
        static List<Line> SplitLines(SafeHDC dc,
            IContext context,
            List<ScriptItem> items,
            int line_width,
            IBox parent,
            out int max_pixel_width)
        {
            if (sp == null)
                throw new ArgumentException("Script properties not initialized.");

            max_pixel_width = line_width;
            var lines = new List<Line>();

            var replaced_chunks = new List<string>(); // 发生替换后的 chunk 内容
            for (int i = 0; i < items.Count; i++)
            {
                replaced_chunks.Add(null/*items[i].Chunk*/);
            }
            Debug.Assert(items.Count == replaced_chunks.Count);

            Line line = new Line(parent);
            long start_pixel = 0;

            for (int i = 0; i < items.Count; i++)
            {
                var data = items[i];
                var item = data.Item;

                {
                    // 析出本 item 的文字
                    string text = data.Chunk;
                    string replaced = replaced_chunks[i];
                    if (replaced == null)
                    {
                        replaced = context?.ConvertText?.Invoke(data.Chunk) ?? data.Chunk;
                    }

                    var tag = data.Tag;

                    Font used_font = null;
                    //string text = str;
                    //var replaced = context?.ConvertText?.Invoke(text) ?? text;
                    for (; ; )
                    {
                        // 寻找可以 break 的位置
                        // return:
                        //      返回 text 中可以切割的 index 位置。如果为 -1，表示 str 中发现了无法显示的字符。
                        var pos = BreakItem(
                        (p, o) =>
                        {
                            return context?.GetFont?.Invoke(parent, tag);
                        },
                        context,
                        dc,
                        item,
    replaced,
    (line_width == -1 ? int.MaxValue : line_width) - start_pixel,
    start_pixel == 0,
    out int left_width,
    out int abcC,
    ref used_font);
                        if (pos == -1)
                        {
                            // s 需要替换无法显示的字符，并且重新 Itemize
                            replaced = Line.ReplaceMissingGlyphs(
            dc,
            used_font,
            replaced,
            out int replace_count);
                            if (replace_count == 0)
                            {
                                replaced = Line.ReplaceMissingGlyphs(
dc,
null,
replaced,
out replace_count);
                            }
                            // TODO: 重新 Itemize，修改 pItems
                            var new_items = Itemize(
context,
dc,
replaced,
parent,
false,
tag);

                            {
                                replaced_chunks.RemoveAt(i);
                                replaced_chunks.InsertRange(i, new_items.Select(o => o.Chunk));
                            }

                            {
                                items.RemoveAt(i);
                                // 要把 new_item.Chunk 重新设置回原始内容
                                // 按照 new_item 中的片段位置，从 text 中取一一对应的位置，覆盖到原 Chunk 中
                                int offs = 0;
                                int j = 0;
                                foreach (var s in new_items.Select(o => o.Chunk))
                                {
                                    new_items[j].Chunk = text.Substring(offs, s.Length);
                                    offs += s.Length;
                                    j++;
                                }
                                items.InsertRange(i, new_items);
                            }

                            Debug.Assert(items.Count == replaced_chunks.Count);

                            i--;
                            break;
                        }


                        // 从 pos 位置开始切割
                        Debug.Assert(pos >= 0 && pos <= text.Length, "pos must be within the bounds of the text string.");

                        // 剩下的右侧空位，连安放一个字符也不够，那么就先换行
                        if (pos == 0 && line.Ranges.Count > 0)
                        {
                            // 折行
                            lines.Add(line);
                            line = new Line(parent);
                            // line.ConvertText = func_convertText;
                            start_pixel = 0;
                            continue;
                        }

                        if (pos == 0)
                        {
                            // throw new Exception();
                            pos = 1;
                        }

                        // 左边部分
                        if (pos > 0)
                        {
                            if (line == null)
                            {
                                line = new Line(parent);
                                // line.ConvertText = func_convertText;
                            }
                            line.Ranges.Add(NewRange(text.Substring(0, pos),
                                replaced.Substring(0, pos),
                                left_width));
                            start_pixel += left_width;

                            text = text.Substring(pos);
                            replaced = replaced.Substring(pos);
                            if (string.IsNullOrEmpty(text))
                                break;
                        }

                        if (/*start_pixel >= line_width &&*/ line.Ranges.Count > 0)
                        {
                            // 折行
                            lines.Add(line);
                            line = new Line(parent);
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

                        // t 是原始文字内容，r 是替换后的文字内容
                        RangeWrapper NewRange(string t,
                            string r,
                            int w)
                        {
                            var range = new RangeWrapper
                            {
                                Item = item,
                                a = item.a,
                                Font = used_font,
                                Text = t,
                                DisplayText = r,    // context?.ConvertText?.Invoke(t) ?? t,
                                PixelWidth = w,
                                Tag = tag,
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
                // RefreshLine() 中不应该再出现缺失字形的情况了
                var width = Line.RefreshLine(
                    (p, o) =>
                    {
                        return context?.GetFont?.Invoke(current_line, current_line.Tag);
                    },
                    context,
                    dc,
                    current_line,
                    line_width);
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

#if OLD
        // parameters:
        //      chunkes 分片的原始文字
        static List<Line> SplitLines(SafeHDC dc,
            IContext context,
            // GetFontFunc func_getfont,
            SCRIPT_ITEM[] pItems,
            List<string> chunks,
            List<object> tags,
            int line_width,
            IBox parent,
            out int max_pixel_width)
        {
            if (sp == null)
                throw new ArgumentException("Script properties not initialized.");

            max_pixel_width = line_width;
            List<Line> lines = new List<Line>();
            Line line = new Line(parent);
            long start_pixel = 0;

            for (int i = 0; i < pItems.Length; i++)
            {
                var item = pItems[i];

                // 析出本 item 的文字
                string str = chunks[i];
                var tag = tags[i];

                {
                    Font used_font = null;
                    string text = str;
                    var replaced = context?.ConvertText?.Invoke(text) ?? text;
                    bool has_replaced = false;
                    for (; ; )
                    {
                        // 只要发生过一次替换，那就表明多个 Range 无法共用同一个 font 了
                        if (has_replaced)
                            used_font = null;

                        // 寻找可以 break 的位置
                        // return:
                        //      返回 text 中可以切割的 index 位置。如果为 -1，表示 str 中发现了无法显示的字符。
                        var pos = BreakItem(
                        (p, o) =>
                        {
                            return context?.GetFont?.Invoke(parent, tag);
                        },
                        context,
                        dc,
                        item,
    replaced,
    (line_width == -1 ? int.MaxValue : line_width) - start_pixel,
    start_pixel == 0,
    out int left_width,
    out int abcC,
    ref used_font);
                        if (pos == -1)
                        {
                            // s 需要替换无法显示的字符，并且重新 Itemize
                            replaced = Line.ReplaceMissingGlyphs(
            dc,
            used_font,
            replaced);
                            // TODO: 重新 Itemize，修改 pItems


                            used_font = null;
                            has_replaced = true;
                            // TODO: 这里要检测和防止无限循环。同一个内容只允许 redo 一次
                            continue;
                        }


                        // 从 pos 位置开始切割
                        Debug.Assert(pos >= 0 && pos <= text.Length, "pos must be within the bounds of the text string.");

                        // 剩下的右侧空位，连安放一个字符也不够，那么就先换行
                        if (pos == 0 && line.Ranges.Count > 0)
                        {
                            // 折行
                            lines.Add(line);
                            line = new Line(parent);
                            // line.ConvertText = func_convertText;
                            start_pixel = 0;
                            continue;
                        }

                        if (pos == 0)
                        {
                            // throw new Exception();
                            pos = 1;
                        }

                        // 左边部分
                        if (pos > 0)
                        {
                            if (line == null)
                            {
                                line = new Line(parent);
                                // line.ConvertText = func_convertText;
                            }
                            line.Ranges.Add(NewRange(text.Substring(0, pos),
                                replaced.Substring(0, pos),
                                left_width));
                            start_pixel += left_width;

                            text = text.Substring(pos);
                            replaced = replaced.Substring(pos);
                            if (string.IsNullOrEmpty(text))
                                break;
                        }

                        if (/*start_pixel >= line_width &&*/ line.Ranges.Count > 0)
                        {
                            // 折行
                            lines.Add(line);
                            line = new Line(parent);
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

                        // t 是原始内容，r 是替换后的内容
                        RangeWrapper NewRange(string t,
                            string r,
                            int w)
                        {
                            var range = new RangeWrapper
                            {
                                Item = item,
                                a = item.a,
                                Font = used_font,
                                Text = t,
                                DisplayText = r,    // context?.ConvertText?.Invoke(t) ?? t,
                                PixelWidth = w,
                                Tag = tag,
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
                var width = Line.RefreshLine(
                    (p, o) =>
                    {
                        return context?.GetFont?.Invoke(current_line, current_line.Tag);
                    },
                    context,
                    dc,
                    current_line,
                    line_width);
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
        //      返回 text 中可以切割的 index 位置。如果为 -1，表示 str 中发现了无法显示的字符。
        static int BreakItem(
            GetFontFunc func_getfont,
            IContext context,
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
            //using (var cache = new SafeSCRIPT_CACHE())
            {
                var a = item.a;
                var ret = Line.ShapeAndPlace(
                    func_getfont,
                    context,
                    dc,
                    ref a,
                    //cache,
                    str,
                    out ushort[] glfs,
                    out int[] piAdvance,
                    out _, // GOFFSET[] pGoffset,
                    out ABC pABC,
                    out _,  //SCRIPT_VISATTR[] sva,   // 中间有关于 ZeroWidth 的信息
                    out ushort[] log,
                    ref used_font);
                if (ret == 1)
                {
                    return -1;
                }

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

                // piAdvance 数量可能比 str 内的字符要多？

                // Debug.Assert(array.Length == piAdvance.Length);

                int start = 0;
                start += pABC.abcA;
                int delta = 0;
                if (pABC.abcC < 0)
                    delta = -pABC.abcC;
                // i 是 Glyphs 下标
                for (int i = 0; i < piAdvance.Length; i++)
                {
                    int tail = start + piAdvance[i];

                    if (i >= (is_left_most ? 1 : 0)   // 避免 i == 0 时返回。确保至少有一个字符被切割
                        && tail + delta >= pixel_width)
                    {
                        // chars 下标
                        var char_index = GetCharIndex(i);
                        var attr = array[char_index];
                        if (attr.fSoftBreak || attr.fWhiteSpace || attr.fCharStop)
                        {
                            left_width = start;
                            return char_index;
                        }
                    }
                    start += piAdvance[i];
                }

                return str.Length;

                int GetCharIndex(int glyphIndex)
                {
                    int i = 0;
                    foreach (var v in log)
                    {
                        if (v >= glyphIndex)
                            return i;
                        i++;
                    }
                    throw new ArgumentException($"glyphIndex {glyphIndex} 在 log 数组中没有找到");
                }
            }
        }

#if REMOVED
        // 将 glyphIndex 映射到逻辑字符索引（返回 -1 表示无法映射）
        static int GlyphIndexToCharIndex(ushort[] logClust /*可为 null*/, SCRIPT_VISATTR[] sva, int glyphIndex)
        {
            if (glyphIndex < 0)
                return -1;

            // 优先使用 logClust（如果它的语义是 glyph -> char）
            if (logClust != null && glyphIndex < logClust.Length)
            {
                return logClust[glyphIndex]; // 可能需要根据你的 ScriptShape 返回值再调整
            }

            // 回退：通过 sva 跳过 zero-width / diacritic glyph 来计数
            int charIndex = 0;
            for (int i = 0; i <= glyphIndex && i < (sva?.Length ?? 0); i++)
            {
                // 当 glyph 不是 zero-width 时，视为对应一个后续的字符位置
                if (!sva[i].fZeroWidth)
                    charIndex++;
            }
            // 返回字符索引（注意：上面计数得到的是“已通过的字符数”，可能需要 -1/调整以适配你的 array 索引）
            return Math.Max(0, charIndex - 1);
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
            DisposeLines();
            ColorCache?.Clear();
        }

        // 根据单行行高计算出 Paragraph 总的像素高度
        public int GetPixelHeight(/*int line_height*/)
        {
            if (_lines == null)
                return 0;
            if (_lines.Count == 0)
                return FontContext.DefaultFontHeight;
            // 注意，每个 Line 的像素高度可能不同
            return _lines.Sum(line => line.GetPixelHeight());
            // return _lines.Count * Line._line_height;
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
            int y)
        {
            // 2025/12/3
            if (y < 0)
                return new HitInfo { Area = Area.TopBlank };

            var result = new HitInfo();
            int current_y = 0;
            int offs = 0;
            for (int i = 0; i < _lines.Count; i++)
            {
                var line = _lines[i];
                bool isLastLine = (i == _lines.Count - 1);
                if (y < current_y)
                    break;
                if (y >= current_y && (y < current_y + line.GetPixelHeight() || isLastLine))
                {
                    var sub_info = line.HitTest(x,
    current_y);
                    return new HitInfo
                    {
                        X = sub_info.X,
                        Y = current_y + sub_info.Y,
                        ChildIndex = i,
                        // RangeIndex = hit_range_index,
                        TextIndex = sub_info.Offs,
                        Offs = offs + sub_info.Offs,
                        LineHeight = sub_info.LineHeight,
                        Area = y < current_y + line.GetPixelHeight() ? Area.Text : Area.BottomBlank,
                        InnerHitInfo = sub_info,
                    };
                }

                current_y += line.GetPixelHeight();
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
            RangeWrapper range = null;
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

                start_y += line.GetPixelHeight();

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
                            InnerHitInfo = hit_info,
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

                start_y += line.GetPixelHeight();
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
                info.LineHeight = FontContext.DefaultFontHeight;
                return 0;
            }

            info.Area = Area.BottomBlank;
            return 1;
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


            if (start_offs == end_offs)
                return null;
            if (end_offs <= 0)
                return null;
            if (start_offs >= this.TextLength + virtual_tail_length)
                return null;

            if (this._lines?.Count == 0)
            {
                // 尾部结束符
                if (virtual_tail_length > 0
                    && Utility.InRange(0, start_offs, end_offs))
                {
                    return new Region(new RectangleF(0,
                        0,
                        FontContext.DefaultReturnWidth,
                        FontContext.DefaultFontHeight));
                }
                return null;
            }

            Region region = null;
            int y = 0;
            int i = 0;
            foreach (var line in this._lines)
            {
                bool is_last_line = i == this._lines.Count - 1;
                var result = line.GetRegion(start_offs,
                    end_offs,
                    is_last_line ? virtual_tail_length : 0);
                if (result != null)
                {
                    result.Offset(0, y);

                    if (region == null)
                        region = result;
                    else
                    {
                        region.Union(result);
                        result.Dispose();
                    }
                }
                var length = line.TextLength;
                start_offs -= length;
                end_offs -= length;
                y += line.GetPixelHeight();
                i++;
            }

            return region;
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
        Line HitLine(int x, int y)
        {
            // 找到 x, y 所在的 line
            var temp = this.HitTest(x, y);
            if (temp.ChildIndex < 0 || temp.ChildIndex >= this._lines.Count)
                return null;
            return this._lines[temp.ChildIndex];
        }

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

            if (y < 0)
                y = 0;

            // 找到 x, y 所在的 line
            var line = HitLine(x, y);
            if (line == null)
            {
                return false;
            }

            var line_height = line.GetPixelHeight();

            // 注：每一个 Line 的行高都可能不一样，不能这样加法
            y += line_height;
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

            // 找到 x, y 所在的 line
            var line = HitLine(x, y);
            if (line == null)
            {
                return false;
            }

            var line_height = line.GetPixelHeight();

            y -= line_height;
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

        public void ClearCache()
        {
            if (_lines == null)
                return;
            this.ColorCache?.Clear();
            foreach (var line in _lines)
            {
                line.ClearCache();
            }
        }

        public void Dispose()
        {
            DisposeLines();
        }

        void DisposeLines()
        {
            if (_lines == null)
                return;
            foreach (var line in _lines)
            {
                line?.Dispose();
            }

            _lines.Clear();
        }

        static void RemoveLines(List<Line> lines, int start, int count)
        {
            for (int i = start; i < start + count; i++)
            {
                lines[i]?.Dispose();
            }
            lines.RemoveRange(start, count);
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

        public Paragraph(IBox parent, string text)
        {
            Parent = parent;
            _lines = new List<Line>() {
            new Line(this, text)
            };
        }

        #endregion
    }
}
