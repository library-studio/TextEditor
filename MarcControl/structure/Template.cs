using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using Vanara.Collections;
using Vanara.PInvoke;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 定长输入模板
    /// </summary>
    public class Template : IBox, IDisposable, IViewMode
    {
        List<Line> _lines = new List<Line>();
        Metrics _metrics = null;

        public Template(IBox parent, Metrics metrics)
        {
            Parent = parent;
            _metrics = metrics;
        }

        public string Name { get; set; }

        public IBox Parent { get; set; }

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

        public ViewMode ViewMode { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

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

        Line HitLine(int x, int y)
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


        public int GetPixelHeight(/*int line_height*/)
        {
            if (_lines == null)
                return 0;
            if (_lines.Count == 0)
                return FontContext.DefaultFontHeight;
            // 注意，每个 Line 的像素高度可能不同
            return _lines.Sum(line => line.GetPixelHeight());
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

        public int MoveByOffs(int offs_param, int direction, out HitInfo info)
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

        public void Paint(IContext context,
            Gdi32.SafeHDC dc,
            int x,
            int y,
            Rectangle clipRect,
            int blockOffs1,
            int blockOffs2,
            int virtual_tail_length)
        {
            if (_lines == null)
                return;
            int y_offset = y;
            int offs = 0;
            int i = 0;
            foreach (var line in _lines)
            {
                var tail_line = (i == _lines.Count - 1);
                line.Paint(context,
                    dc,
                    x,
                    y_offset,
                    clipRect,
                    blockOffs1 - offs,
                    blockOffs2 - offs,
                    tail_line ? virtual_tail_length : 0);
                y_offset += line.GetPixelHeight();
                offs += line.TextLength;
                i++;
            }
        }

        public ReplaceTextResult ReplaceText(IContext context, Gdi32.SafeHDC dc, int start, int end, string content, int pixel_width)
        {
            throw new NotImplementedException();
        }


        // context 中需要一种机制用于获取模板的结构定义
        public ReplaceTextResult ReplaceText(
            ViewModeTree view_mode_tree,
            IContext context,
            Gdi32.SafeHDC dc,
            int start,
            int end,
            string content,
            int pixel_width)
        {
            if (start < 0 || end < -1)
                throw new ArgumentException($"start ({start}) 或 end ({end}) 不合法");

            EnsureLines();

            // 确保 start 是较小的一个
            if (end != -1 && start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            if (end == -1)
                end = Int32.MaxValue;

            var old_text = this.MergeText();
            var new_text = old_text.Substring(0, start) + content + old_text.Substring(end);

            var new_lines = new List<Line>();
            if (_metrics == null)
                throw new ArgumentException("无法进行 ReplaceText 操作，因为没有指定 Metrics.GetStructure 函数");

            var field_infos = _metrics.GetStructure(this)?.ToList();
            if (field_infos == null)
                throw new ArgumentException("无法进行 ReplaceText 操作，因为没有指定 Metrics.GetStructure 函数调用返回了 null");

            // 如果 new_text 长度不足?
            var total_length = field_infos.Sum((info) => info.Length);
            if (new_text.Length < total_length)
            {
                new_text = new_text.PadRight(total_length, ' ');
            }

            int pixel_height = 0;
            int max_pixel_width = 0;
            int i = 0;
            int offs = 0;
            foreach (var info in field_infos)
            {
                var line_text = new_text.Substring(offs, info.Length);
                var line = new Line(this);
                line.ReplaceText(context,
                    dc,
                    0,
                    info.Length,
                    line_text,
                    pixel_width);
                pixel_height += line.GetPixelHeight();
                max_pixel_width = Math.Max(max_pixel_width, line.GetPixelWidth());
                i++;
                offs += info.Length;
            }

            ProcessBaseline();
            var result = new ReplaceTextResult
            {
                MaxPixel = max_pixel_width,
                NewText = new_text.Substring(start, content.Length),
                ReplacedText = old_text.Substring(start, end - start),
                UpdateRect = new Rectangle(0, 0, max_pixel_width, pixel_height),
                ScrollRect = Rectangle.Empty,
                ScrolledDistance = 0,
            };
            return result;
        }

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

        void EnsureLines()
        {
            if (_lines == null)
            {
                _lines = new List<Line>();
            }
        }

        // TODO: 允许在不具备模板分段信息的情况下，把内容当作整个一个 Paragraph 处理。这时就存在 ViewMode 为两种不同值的可能性的
        public ViewModeTree GetViewModeTree()
        {
            // 目前一定是展开状态
            return new ViewModeTree { ViewMode = ViewMode.Table };
        }


#if REMOVED
        public List<Line> FindLines(
    int start,
    out string left_text,
    int end,
    out string right_text,
    string new_text,
    out int first_line_index,
    out string replaced)
        {
            if (start < 0 || end < -1)
                throw new ArgumentException($"start ({start}) 或 end ({end}) 不合法");

            // 确保 start 是较小的一个
            if (end != -1 && start > end)
            {
                int temp = start;
                start = end;
                end = temp;
            }

            if (end == -1)
                end = Int32.MaxValue;

            var field_infos = _metrics.GetStructure(this)?.ToList();

            // 新的相对于旧的变化了多少。负数表示变短
            int delta = new_text.Length - (end - start);

            first_line_index = -1;

            var results = new List<Line>();
            int offs = 0;
            int i = 0;
            StringBuilder left = new StringBuilder();
            StringBuilder middle = new StringBuilder();
            StringBuilder right = new StringBuilder();
            bool complete = false;
            foreach (var line in _lines)
            {
                FieldInfo struct_info = null;
                if (i < field_infos.Count)
                    struct_info = field_infos[i];
                else
                    struct_info = new FieldInfo
                    {
                        Name = $"未知字段{i}",
                        Caption = $"未知字段{i}",
                        Length = int.MaxValue,
                    };
                var length = struct_info.Length;
                if (Utility.Cross(start, end,
                    offs, offs + length)
                    )
                {
                    var start_length = Math.Max(start - offs, 0);   // 防止负数
                    var end_length = Math.Max((offs + length) - end, 0);
                    if (start_length > 0)
                        left.Append(line.MergeText(0, start_length));
                    middle.Append(line.MergeText(start_length, length - end_length));
                    if (end_length > 0)
                        right.Append(line.MergeText(length - end_length, length));

                    results.Add(line);
                    if (results.Count == 1)
                        first_line_index = i;

                    if (right.Length > 0)
                    {
                        complete = true;
                        goto CONTINUE;
                    }
                }
                else if (offs >= end)
                    complete = true;

                if (complete == true)
                {
                }

            CONTINUE:
                offs += length;
                i++;
            }

            if (first_line_index == -1)
                first_line_index = i;
            left_text = left.ToString();
            replaced = middle.ToString();
            right_text = right.ToString();
            return results;
        }
#endif
    }
}
