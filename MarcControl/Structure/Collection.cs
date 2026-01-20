using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 容纳若干 IBox 对象的容器
    /// </summary>
    public class Collection<T> : IViewBox, IDisposable
        where T : IViewBox, IDisposable, new()
    {
        List<T> _lines = new List<T>();

        public IEnumerable<T> Children
        {
            get
            {
                foreach (var child in _lines)
                {
                    yield return child;
                }
            }
        }

        public ViewMode ViewMode { get; set; }

        public string Name { get; set; }

        public IBox Parent { get; set; }

        public int TextLength => _lines.Sum(c => c.TextLength);

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


        // 向下移动插入符一行
        // parameters:
        //      x   起点 x 位置。这是(调主负责保存的)最近一次左右移动插入符之后，插入符的 x 位置。注意，并不一定等于当前插入符的 x 位置
        //      y   当前插入符的 y 位置
        //      info    [in,out]插入符位置参数。
        //              注: [in] 时，info.X info.Y 被利用，其它成员没有被利用
        // return:
        //      true    成功。新的插入符位置返回在 info 中了
        //      false   无法移动。注意此时 info 中返回的内容无意义
        public bool CaretMoveDown(int x, int y, out HitInfo info)
        {
            if (this._lines == null)
            {
                info = new HitInfo();
                return false;
            }

            // 先定位当前字段的 index
            var temp = this.HitTest(x, y);
            var index = temp.ChildIndex;
            if (index < 0 || index >= _lines.Count)
            {
                info = new HitInfo();
                return false;
            }

            var start_y = SumHeight(_lines, index);

            var field = _lines[index];
            var ret = field.CaretMoveDown(x,
                y - start_y,
                out info);
            if (ret == true)
            {
                // 能成功 Move
                y = info.Y + start_y;
                if (y >= this.GetPixelHeight())
                {
                    info = new HitInfo();
                    return false;
                }
                info = this.HitTest(x, y);
                return true;
            }

            if (index >= this._lines.Count - 1)
            {
                /*
                // 到最后一个字段之下的 Name 区第一个字符位置
                if (index == this._lines.Count - 1)
                {
                    MoveByOffs(this.TextLength,
                        0,
                        out info);
                    return true;
                }
                */
                info = new HitInfo();
                return false;
            }

            {
                if (info.ChildIndex == 0
                    || info.ChildIndex == 1)
                {
                    // 越过本字段高度
                    y += field.GetPixelHeight();
                }
                else
                {
                    // 当前字段的下沿
                    y = start_y + field.GetPixelHeight();
                }

                // 如果越过整个 Collection 下沿
                if (y >= this.GetPixelHeight())
                {
                    info = new HitInfo();
                    return false;
                }
                info = this.HitTest(x, y);
                return true;
            }
        }

        public bool CaretMoveUp(int x, int y, out HitInfo info)
        {
            if (this._lines == null)
            {
                info = new HitInfo();
                return false;
            }

            // 先定位当前字段的 index
            var temp = this.HitTest(x, y);
            var index = temp.ChildIndex;
            // 当前正在最后一个字段以下位置。要移动到最后一个字段的 name 区第一字符位置
            if (index == _lines.Count)
            {
                int y0 = SumHeight(_lines, _lines.Count);
                info = this.HitTest(x, y0 - 1);
                return true;
            }
            if (index < 0 || index >= _lines.Count)
            {
                info = new HitInfo();
                return false;
            }

            var start_y = SumHeight(_lines, index);

            var field = _lines[index];
            var ret = field.CaretMoveUp(x,
                y - start_y,
                out info);
            if (ret == true)
            {
                // 能成功 Move
                info = new HitInfo();
                y -= 1; // Line.GetLineHeight();
                if (y < 0)
                {
                    info = new HitInfo();
                    return false;
                }
                info = this.HitTest(x, y);
                return true;
            }

            if (index == 0)
            {
                info = new HitInfo();
                return false;
            }

            {
                if (info.ChildIndex == 0
    || info.ChildIndex == 1)
                {
                    Debug.Assert(index > 0);
                    // 减去前一个字段的像素高度
                    var prev_field = _lines[index - 1];
                    y -= prev_field.GetPixelHeight();
                }
                else
                {
                    // 当前字段的上沿，再小一点点
                    y = start_y - 1;
                }

                if (y < 0)
                {
                    info = new HitInfo();
                    return false;
                }
                info = this.HitTest(x, y);
                return true;
            }
        }

#if OLD
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

#endif

        IBox HitLine(int x, int y)
        {
            // 找到 x, y 所在的 line
            var temp = this.HitTest(x, y);
            if (temp.ChildIndex < 0 || temp.ChildIndex >= this._lines.Count)
                return null;
            return this._lines[temp.ChildIndex];
        }

        public ColorCache ColorCache = new ColorCache();

        public void Clear()
        {
            DisposeLines();
            ColorCache?.Clear();
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
    y - current_y);
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
            IBox line = null;
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

        public virtual ReplaceTextResult ReplaceText(
    IContext context,
    SafeHDC dc,
    int start,
    int end,
    string text,
    int pixel_width)
        {
            throw new NotImplementedException();
        }

        public virtual ReplaceTextResult ReplaceText(
            ViewModeTree view_mode_tree,
            IContext context,
            SafeHDC dc,
            int start,
            int end,
            string text,
            int pixel_width)
        {
            var result = new ReplaceTextResult();

            // 先分别定位到 start 所在的 Line，和 end 所在的 Line
            // 观察被替换的范围 start - end，跨越了多少个 Line
            // 对 text 进行 Line 切分。把这些 Line 替换为由切割后的 text 构造的零到多个新 Line

            var old_lines = FindChildren(
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

            var new_lines = new List<T>();
            int max_pixel_width = pixel_width;
            if (string.IsNullOrEmpty(content) == false)
            {
                var strings = SplitChildren(content);
                int j = 0;
                foreach (var s in strings)
                {
                    // var child = new T();
                    var child = CreateChild(context, j);
                    child.Parent = this;
                    // collection 会直接把 context 中的 ViewModeTree 不做修改就传递下去
                    var ret = child.ReplaceText(
                        view_mode_tree?.ChildViewModes?.ElementAtOrDefault(j),
                        context,
                        dc,
                        0,
                        -1,
                        s,
                        pixel_width);
                    max_pixel_width = Math.Max(max_pixel_width, ret.MaxPixel);
                    new_lines.Add(child);
                    j++;
                }

            }

            // offs 为(Paragraph)从头开始计算的第一个不同 char 的 offs
            var index = SameLines(old_lines, new_lines, out int offs);
            if (index > 0)
            {
                if (index < old_lines.Count && index < new_lines.Count)
                {
                    offs += SameChars(old_lines[index], new_lines[index]);
                }

                old_lines.RemoveRange(0, index);
                RemoveLines(new_lines, 0, index);   // 刚创建的 field 要删除必须要 Dispose()
                first_line_index += index;
            }

            // TODO: 修改前的最后一个 Line 末尾的回车换行符号区域。
            // 修改后的最后一个 Line 末尾的回车换行符号区域，都应包含到失效区中

            int max = old_lines.Count == 0 ? 0 : old_lines.Max(p => p.GetPixelWidth());
            if (max > max_pixel_width)
            {
                max_pixel_width = max;
            }

            if (old_lines.Count > 0)
            {
                RemoveLines(_lines, first_line_index, old_lines.Count);
            }

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

        public virtual T CreateChild(IContext context, int index)
        {
            var result = new T();
            result.Parent = this;
            return result;
        }

        // 根据 offs 范围获得相关的 T 列表
        // parameters:
        //      left_text [out] 返回命中的第一个 T 处于 start 位置之前的局部文字内容
        //      right_text [out] 返回命中的最后一个 T 处于 end 位置之后的局部文字内容
        //      first_line_index   [out] 命中的第一个 T 的 index。若为 -1，表示没有命中的
        //      replaced    [out] 返回 start 和 end 之间即将被替换的部分内容
        // return:
        //      命中的 T 列表
        public static List<T> FindChildren(
            List<T> sources,
            int start,
            out string left_text,
            int end,
            out string right_text,
            out int start_child_index,
            out string replaced)
        {
            left_text = "";
            right_text = "";
            start_child_index = 0;
            replaced = "";

            if (end == -1)
                end = int.MaxValue;

            Debug.Assert(start <= end);
            if (start > end)
                throw new ArgumentException($"start {start} must less than end {end}");

            StringBuilder replaced_part = new StringBuilder();

            var results = new List<T>();
            int offs = 0;
            int i = 0;
            foreach (var line in sources)
            {
                if (offs > end)
                    break;

                int line_text_length = line.TextLength;

                // 命中
                if (offs <= end && offs + line_text_length >= start)
                {
                    var text = line.MergeText();
                    if (results.Count == 0)
                    {
                        left_text = text.Substring(0, start - offs);
                        start_child_index = i;
                    }

                    // right_text 不断被更新，只要留下最后一次的值即可
                    if (offs + line_text_length >= end)
                        right_text = text.Substring(end - offs);
                    results.Add(line);

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
            return results;
        }

        // 把文字内容按需切割为子结构所需的部分
        public virtual IEnumerable<string> SplitChildren(string text)
        {
            return new List<string>() { text };
        }


        // 检查两组行，前方有连续多少行互相没有不同
        // parameters:
        //      offs    [out] 返回第一个不同的 offs
        // return:
        //      返回第一个不同的 index
        static int SameLines(List<T> lines1,
            List<T> lines2,
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
        static int SameChars(T line1, T line2)
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


        static int SumHeight(List<T> lines, int count)
        {
            int height = 0;
            for (int i = 0; i < count; i++)
            {
                height += lines[i].GetPixelHeight();
            }
            return height;
        }

        static int SumHeight(List<T> lines, int start, int count)
        {
            int height = 0;
            for (int i = start; i < start + count; i++)
            {
                height += lines[i].GetPixelHeight();
            }
            return height;
        }

        static void RemoveLines(List<T> lines, int start, int count)
        {
            for (int i = start; i < start + count; i++)
            {
                lines[i]?.Dispose();
            }
            lines.RemoveRange(start, count);
        }

        // 处理整个 Collection 的基线
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

        // 注: “我”自己的 ViewMode 是无所谓的，要靠父对象的 ViewMode 来定义
        public ViewModeTree GetViewModeTree()
        {
            var results = new List<ViewModeTree>();
            foreach (var child in Children)
            {
                if (child is IViewMode)
                {
                    results.Add((child as IViewMode).GetViewModeTree());
                }
                else
                {
                    results.Add(new ViewModeTree { ViewMode = child.ViewMode });
                }
            }
            return new ViewModeTree { ChildViewModes = results };
        }
    }


    public interface IViewBox : IBox, IViewMode
    {

    }
}
