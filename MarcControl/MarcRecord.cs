using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;

using LibraryStudio.Forms;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 一个 MARC 记录编辑区域
    /// 由若干 MarcField 构成
    /// </summary>
    public class MarcRecord : IBox
    {
        // public ConvertTextFunc ConvertText { get; set; }

        // public Color BackColor { get; set; } = Color.Transparent;

        List<IBox> _fields = new List<IBox>();

        // 引用字段共同属性
        FieldProperty _fieldProperty;

        public MarcRecord(FieldProperty fieldProperty/*, ConvertTextFunc func_convertText*/)
        {
            _fieldProperty = fieldProperty;
            // ConvertText = func_convertText;
            /*
            if (_fields == null)
                _fields = new List<IBox>();

            _fields.Add(new MarcField(_fieldProperty));
            */
        }

        /*
        public MarcRecord()
        {
            //if (_fields == null)
            //    _fields = new List<IBox>();

            //_fields.Add(new MarcField(_fieldProperty));
        }
        */

        public int TextLength
        {
            get
            {
                if (_fields == null || _fields.Count == 0)
                    return 0;
                return _fields.Sum(f => f.TextLength) + _fields.Count - 1;
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
            info = new HitInfo();
            y += Line.GetLineHeight();
            if (y >= this.GetPixelHeight())
                return false;
            info = this.HitTest(x, y);
            return true;
        }

        public bool CaretMoveUp(int x, int y, out HitInfo info)
        {
            info = new HitInfo();
            y -= Line.GetLineHeight();
            if (y < 0)
                return false;
            info = this.HitTest(x, y);
            return true;
        }

        public void Clear()
        {
            _fields.Clear();
        }

        public int GetPixelHeight()
        {
            if (_fields == null)
                return 0;
            if (_fields.Count == 0)
                return Line.GetLineHeight();
            return _fields.Sum(field => field.GetPixelHeight());
        }

        public HitInfo HitTest(int x, int y)
        {
#if REMOVED
            // 点击到了左边 Caption 区域
            if (x < _fieldProperty.CaptionPixelWidth - MarcField.SplitterPixelWidth)
            {
                return new HitInfo
                {
                    X = x,
                    Y = y,
                    ChildIndex = -2, // -2 表示 caption 文字区域
                    TextIndex = 0,
                    Offs = 0,
                    LineHeight = Line.GetLineHeight(),
                    Area = Area.LeftBlank,
                };
            }

            // 点击到了 Caption 区域和 Name 区域的缝隙位置
            if (x < _fieldProperty.CaptionPixelWidth)
            {
                return new HitInfo
                {
                    X = x,
                    Y = y,
                    ChildIndex = -1, // -1 表示 caption 和 name 之间的缝隙
                    TextIndex = 0,
                    Offs = 0,
                    LineHeight = Line.GetLineHeight(),
                    Area = Area.LeftBlank,
                };
            }
#endif

            // 把小于 0 的 y 位置调整为 0，这样当作是点击在第一行的顶部
            if (y < 0)
                y = 0;
            var result = new HitInfo();
            int current_y = 0;
            int offs = 0;
            for (int i = 0; i < _fields.Count; i++)
            {
                var field = _fields[i];
                int return_length = (i == 0 ? 0 : 1); // 第一行没有字段结束符
                bool isLastField = (i == _fields.Count - 1);
                var height = field.GetPixelHeight();
                if (y < current_y)
                    break;
                if (y >= current_y && (y < current_y + height || isLastField))
                {
                    var result_info = field.HitTest(x,
    y - current_y);
                    return new HitInfo
                    {
                        X = result_info.X,
                        Y = current_y + result_info.Y,
                        ChildIndex = i,
                        // RangeIndex = hit_range_index,
                        TextIndex = result_info.Offs,
                        Offs = offs + result_info.Offs,
                        LineHeight = result_info.LineHeight,
                        Area = y < current_y + height ? Area.Text : Area.BottomBlank,
                    };
                }

                current_y += height;

                offs += field.TextLength + return_length;
            }

            // 空白内容
            return new HitInfo { Area = Area.BottomBlank };
        }

        public string MergeText(int start = 0, int end = int.MaxValue)
        {
            if (end <= start || end <= 0)
                return "";

            StringBuilder builder = new StringBuilder();
            int offs = 0;
            int i = 0;
            foreach (var field in _fields)
            {
                var current_length = field.TextLength;
                builder.Append(field.MergeText(start - offs, end - offs));
                offs += current_length;
                // 除了头标区以外，每个字段末尾都有一个字段结束符
                if (i > 0)
                {
                    if (InRange(offs, start, end))
                        builder.Append(FieldProperty.FieldEndCharDefault);
                    offs++;
                }
                if (offs > end)
                    break;
                i++;
            }

            return builder.ToString();

            bool InRange(int offs0, int start0, int end0)
            {
                return offs0 >= start0 && offs0 < end0;
            }
        }

        // 获得带有 Mask Char 的文本内容
        public string MergeTextMask(int start = 0, int end = int.MaxValue)
        {
            if (end <= start || end <= 0)
                return "";

            StringBuilder builder = new StringBuilder();
            int offs = 0;
            int i = 0;
            foreach (var field in _fields)
            {
                var current_length = field.TextLength;
                builder.Append((field as MarcField).MergeTextMask(start - offs, end - offs));
                offs += current_length;
                // 除了头标区以外，每个字段末尾都有一个字段结束符
                if (i > 0)
                {
                    if (InRange(offs, start, end))
                        builder.Append(FieldProperty.FieldEndCharDefault);
                    offs++;
                }
                if (offs > end)
                    break;
                i++;
            }

            return builder.ToString();

            bool InRange(int offs0, int start0, int end0)
            {
                return offs0 >= start0 && offs0 < end0;
            }
        }


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
        public int MoveByOffs(int offs_param, int direction, out HitInfo info)
        {
            info = new HitInfo();

            var infos = new List<HitInfo>();

            int offs = 0;
            IBox field = null;
            int start_y = 0;
            for (int i = 0; i < _fields.Count; i++)
            {
                // info.RangeIndex = 0;
                field = _fields[i];
                var line_text_length = field.TextLength;
                if (offs_param + direction >= offs && offs_param + direction <= offs + line_text_length)
                {
                    var ret = field.MoveByOffs(offs_param - offs,
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

                offs += line_text_length + (i == 0 ? 0 : 1);

                start_y += field.GetPixelHeight();
            }

            if (infos.Count > 0)
            {
                info = infos[infos.Count - 1];
                return 0;
            }

            // 没有任何 MarcField 的情况
            if (_fields.Count == 0
                && offs + direction == 0)
            {
                info.X = 0;
                info.Y = 0;
                info.ChildIndex = 0;
                info.Offs = offs + direction;
                info.TextIndex = 0;
                info.Area = Area.Text;
                info.LineHeight = Line.GetLineHeight();
                return 0;
            }

            // TODO: 定位到尽量靠后的一个 marcfield 末尾
            info.Area = Area.BottomBlank;
            return 1;
        }

        public void Paint(Gdi32.SafeHDC dc,
            IContext context,
            int x,
            int y,
            Rectangle clipRect,
            int blockOffs1,
            int blockOffs2,
            int virtual_tail_length)
        {
            int current_start_offs = 0;
            var block_start = Math.Min(blockOffs1, blockOffs2);
            var block_end = Math.Max(blockOffs1, blockOffs2);
            int i = 0;
            foreach (var field in _fields)
            {
                // 剪切区域下方的部分不必参与循环了
                if (y >= clipRect.Bottom)
                    break;

                int paragraph_height = field.GetPixelHeight();
                var rect = new Rectangle(x, y, Int32.MaxValue, paragraph_height);
                if (clipRect.IntersectsWith(rect))
                {
                    field.Paint(dc,
                        context,
                        x,
                        y,
                        clipRect,
                        block_start - current_start_offs,
                        block_end - current_start_offs,
                        i == 0 ? 0 : 1);
                }
                y += field.GetPixelHeight();
                current_start_offs += field.TextLength + (i == 0 ? 0 : 1);
                i++;
            }
        }

        // 替换一段文字
        // 若 start 和 end 为 0 -1 表示希望全部替换。-1 在这里表达“尽可能大”
        // 而 0 0 表示在偏移 0 插入内容，注意偏移 0 后面的原有内容会保留
        // parameters:
        //      dc  为初始化新行准备
        //      start   要替换的开始偏移
        //      end     要替换的结束偏移
        //      pixel_width   为初始化新行准备
        //      splitRange  为初始化新行准备
        // return:
        //      进行折行处理以后，所发现的最大行宽像素数。可能比 pixel_width 参数值要大
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

            // 先分别定位到 start 所在的 Paragraph，和 end 所在的 Paragraph
            // 观察被替换的范围 start - end，跨越了多少个 Paragraph
            // 对 text 进行 \r 字符切分。把这些 Paragraph 替换为由切割后的 text 构造的零到多个新 Paragraph

            var old_paragraphs = FindFields(
                _fields,
                start,
                out string left_text,
                end,
                out string right_text,
                out int first_paragraph_index,
                out replaced);

            if (start == 0 && end == -1 /*&& first_paragraph_index == -1*/)
            {
                first_paragraph_index = 0;
                // fields.Clear();
            }

            string content = left_text + text + right_text;

            var new_fields = new List<IBox>();
            int max_pixel_width = pixel_width;
            if (string.IsNullOrEmpty(content) == false)
            {
                // content = content.Replace("\r\n", "\r");

                // 去掉 right_text 末尾的 \r 字符。避免 SplitLine 多生成一个 field
                if (content.EndsWith(new string(FieldProperty.FieldEndCharDefault, 1)))
                    content = content.Substring(0, content.Length - 1);

                /*
                var lines = SimpleText.SplitLines(content,
                    FieldProperty.FieldEndCharDefault,
                    false).ToList();
                */
                /*
// lines[0] 切割为头标区和第一个字段
var first = lines[0];
var header_content = first.Substring(0, Math.Min(first.Length, 24));
var first_field_content = first.Substring(header_content.Length);
lines.RemoveAt(0);
lines.Insert(0, header_content);
if (string.IsNullOrEmpty(first_field_content) == false)
    lines.Insert(1, first_field_content);
*/
                var lines = SplitFields(content, start - left_text.Length);

                foreach (var line in lines)
                {
                    var new_field = new MarcField(_fieldProperty/*, this.ConvertText*/);
                    if (first_paragraph_index == 0 && new_fields.Count == 0)
                        new_field.IsHeader = true;
                    var width = new_field.ReplaceText(dc,
                        0,
                        -1,
                        line,
                        pixel_width,
                        context,
                        out string _,
                        out Rectangle update_rect1,
                        out Rectangle scroll_rect1,
                        out int scroll_distance1);

                    // var width = new_field.Initialize(dc, field, pixel_width, splitRange);
                    if (width > max_pixel_width)
                        max_pixel_width = width;
                    new_fields.Add(new_field);
                }
            }

            if (old_paragraphs.Count > 0)
                _fields.RemoveRange(first_paragraph_index, old_paragraphs.Count);
            if (new_fields.Count > 0)
            {
                Debug.Assert(first_paragraph_index >= 0);
                _fields.InsertRange(first_paragraph_index, new_fields);
            }

            // update_rect 用 old_paragraphs 和 new_fields 两个矩形的较大一个算出
            // 矩形宽度最后用 max_pixel_width 矫正一次
            int old_h = old_paragraphs.Sum(p => p.GetPixelHeight());
            int new_h = new_fields.Sum(p => p.GetPixelHeight());
            int y = SumHeight(_fields, first_paragraph_index);

            update_rect = new Rectangle(0,
                y,
                max_pixel_width,
                Math.Max(old_h, new_h));

            scroll_distance = new_h - old_h;
            if (scroll_distance != 0)
            {
                int move_height = SumHeight(_fields, first_paragraph_index, _fields.Count - first_paragraph_index);
                scroll_rect = new Rectangle(0,
        y + old_h,
        max_pixel_width,
        move_height);
            }
            return max_pixel_width;
        }

#if REMOVED
        // parameters:
        //      start   content 部分内容在整个文本中的开始偏移
        static List<string> SplitFields(string content,
            int start)
        {
            if (start < 0)
                throw new ArgumentException($"start 参数值不应小于 0 ({start})", nameof(start));

            var lines = SimpleText.SplitLines(content,
    FieldProperty.FieldEndCharDefault,
    false).ToList();

            if (start < 24)
            {
                // lines[0] 切割为头标区和第一个字段
                var first = lines[0];
                // 头标区内容
                var header_content = first.Substring(0, Math.Min(first.Length, 24 - start));
                // 第一个字段内容，通常是 001 字段
                var first_field_content = first.Substring(header_content.Length);
                lines.RemoveAt(0);
                lines.Insert(0, header_content);
                if (string.IsNullOrEmpty(first_field_content) == false)
                    lines.Insert(1, first_field_content);
            }

            return lines;
        }
#endif

        // parameters:
        //      start   content 部分内容在整个文本中的开始偏移
        public static List<string> SplitFields(string content,
            int start)
        {
            if (start < 0)
                throw new ArgumentException($"start 参数值不应小于 0 ({start})", nameof(start));
            if (content == null)
                throw new ArgumentException($"content 参数值不应为 null", nameof(content));
            string first_line = null;
            int first_length = 0;
            if (start < 24)
            {
                first_length = Math.Min(content.Length, 24 - start);
                first_line = content.Substring(0, first_length);
                content = content.Substring(first_length);
                if (string.IsNullOrEmpty(content))
                    content = null;
            }

            var lines = new List<string>();

            if (content != null)
            {
                lines = SplitLines(content,
        FieldProperty.FieldEndCharDefault,
        false).ToList();
            }

            if (start < 24 && first_line != null)
            {
                Debug.Assert(first_line.Length <= 24);
                lines.Insert(0, first_line);
            }

            /*
            if (lines.Count == 0)
                lines.Add("");
            */
            return lines;
        }

        // TODO: 加入单元测试
        // 将文本内容按行分割。至少会返回一行内容
        public static string[] SplitLines(string text,
    char delimiter = '\r',
    bool contain_return = true)
        {
            List<string> lines = new List<string>();
            StringBuilder line = new StringBuilder();
            foreach (var ch in text)
            {
                if (ch == delimiter)
                {
                    /*
                    if (field == null)
                        field = new StringBuilder();
                    */
                    if (contain_return)
                        line.Append(ch);
                    lines.Add(line.ToString());
                    // field = null;
                    line = new StringBuilder();
                }
                else
                {
                    /*
                    if (field == null)
                        field = new StringBuilder();
                    */
                    line.Append(ch);
                }
            }
            if (line != null)
                lines.Add(line.ToString());

            /*
            if (lines.Count == 0)
                lines.Add("");
            */
            return lines.ToArray();
        }

        // 根据 offs 范围获得相关的 IBox 列表
        // parameters:
        //      left_text [out] 返回命中的第一个 IBox 处于 start 位置之前的局部文字内容
        //      right_text [out] 返回命中的最后一个 IBox 处于 end 位置之后的局部文字内容
        //      first_paragraph_index   [out] 命中的第一个 IBox 的 index。若为 -1，表示没有命中的
        //      replaced    [out] 返回 start 和 end 之间即将被替换的部分内容
        // return:
        //      命中的 Paragraph 列表
        public static List<IBox> FindFields(
            List<IBox> fields,
            int start,
            out string left_text,
            int end,
            out string right_text,
            out int first_paragraph_index,
            out string replaced)
        {
            left_text = "";
            right_text = "";
            first_paragraph_index = 0;
            replaced = "";

            /*
            if (start == 0 && end == -1)
            {
                replaced = MergeText(fields);
                return new List<Paragraph>();
            }
            */

            if (end == -1)
                end = int.MaxValue;

            Debug.Assert(start <= end);
            if (start > end)
                throw new ArgumentException($"start {start} must less than end {end}");

            StringBuilder replaced_part = new StringBuilder();

            var paragraphs = new List<IBox>();
            int offs = 0;
            int i = 0;
            //bool extend_first = false;
            foreach (var field in fields)
            {
                if (offs > end
    /*&& extend_first == false*/)
                    break;

                bool is_first = IsFirstField(i);
                // 段落文字最后隐含的 \r 字符个数
                int return_length = is_first ? 0 : 1;
                int paragraph_text_length = field.TextLength;

                /*
                if (i == 0 && paragraph_text_length > 24)
                    paragraph_text_length = 24;
                */

                // 命中
                if ((offs <= end && offs + paragraph_text_length + return_length >= start)
                    /*|| (extend_first && i == 1)*//* 如果第一个字段命中，则要包含上第二个字段*/)
                {
                    var text = GetFieldText(i);
                    if (paragraphs.Count == 0)
                    {
                        left_text = text.Substring(0, start - offs);
                        first_paragraph_index = i;
                    }

                    // right_text 不断被更新，只要留下最后一次的值即可
                    if (offs + paragraph_text_length + return_length >= end)
                    {
                        right_text = text.Substring(end - offs);

                        /*
                        if (extend_first == false)
                            right_text = "";
                        right_text += text.Substring(end - offs);
                        */
                    }
                    paragraphs.Add(field);

                    {
                        var part_start = Math.Max(start - offs, 0);
                        var part_length = paragraph_text_length + return_length - part_start;
                        if (offs + paragraph_text_length + return_length >= end)
                            part_length = (end - offs) - part_start;
                        if (part_length > 0)
                            replaced_part.Append(text.Substring(part_start, part_length));
                    }

                    /*
                    if (i == 0)
                    {
                        extend_first = true;
                        if (end < int.MaxValue)
                            end = Math.Max(end, offs + paragraph_text_length + return_length);
                    }
                    else
                        extend_first = false;
                    */
                }

                offs += paragraph_text_length + return_length;
                i++;
            }

            // 如果 paragraphs.Count == 1，并且里面是头标区，则调整 paragraphs 和 right_text，加入头标区后的第一个字段进入其中
            if (paragraphs.Count == 1
                && fields.Count >= 2
                && paragraphs[0] == fields[0])
            {
                paragraphs.Add(fields[1]);
                right_text += GetFieldText(1);
            }

            replaced = replaced_part.ToString();
            return paragraphs;

            string GetFieldText(int index)
            {
                if (IsFirstField(index))
                    return fields[index].MergeText();
                return fields[index].MergeText() + FieldProperty.FieldEndCharDefault;
            }

            bool IsFirstField(int j)
            {
                return j == 0;
            }
        }

        static int SumHeight(List<IBox> lines, int count)
        {
            int height = 0;
            for (int i = 0; i < count; i++)
            {
                height += lines[i].GetPixelHeight();
            }
            return height;
        }

        static int SumHeight(List<IBox> lines, int start, int count)
        {
            int height = 0;
            for (int i = start; i < start + count; i++)
            {
                height += lines[i].GetPixelHeight();
            }
            return height;
        }

        // 获得连续的若干行的累计文本长度。注意，不计算范围最后一行的字段结束符
        // parameters:
        //      index   需要统计的最后一行的索引。注意统计是包含了这一行的
        static int SumTextLength(List<IBox> lines, int index)
        {
            int length = 0;
            int line_count = 0;
            for (int i = 0; i < Math.Min(lines.Count, index + 1); i++)
            {
                length += lines[i].TextLength;
                line_count++;
            }
            // 第一行没有字段结束符。最后一行的字段结束符也不算在内
            line_count -= 2;
            line_count = Math.Max(line_count, 0);
            return length + line_count;
        }

#if REMOVED

        public int ReplaceText(Gdi32.SafeHDC dc,
            int start,
            int end,
            string text,
            int pixel_width,
            SplitRange splitRange,
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

            var old_lines = FindFields(
                _fields.Cast<IBox>(),
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

            var new_fields = new List<MarcField>();
            int max_pixel_width = pixel_width;
            if (string.IsNullOrEmpty(content) == false)
            {
                new_fields = BuildFields(
                    _fieldProperty,
                    dc,
                    content,
                    pixel_width,
                    splitRange,
                    out max_pixel_width);
            }

            if (old_lines.Count > 0)
                _fields.RemoveRange(first_line_index, old_lines.Count);
            if (new_fields.Count > 0)
            {
                Debug.Assert(first_line_index >= 0);
                _fields.InsertRange(first_line_index, new_fields);
            }

            int max = old_lines.Count == 0 ? 0 : old_lines.Max(p => p.GetPixelWidth());
            if (max > max_pixel_width)
                max_pixel_width = max;

            // update_rect 用 old_lines 和 new_fields 两个矩形的较大一个算出
            // 矩形宽度最后用 max_pixel_width 矫正一次
            int old_h = old_lines.Sum(p => p.GetPixelHeight());
            int new_h = new_fields.Sum(p => p.GetPixelHeight());
            int y = SumHeight(_fields, first_line_index);

            update_rect = new Rectangle(0,
                y,
                max_pixel_width,
                Math.Max(old_h, new_h));

            scroll_distance = new_h - old_h;
            if (scroll_distance != 0)
            {
                int move_height = SumHeight(_fields, first_line_index, _fields.Count - first_line_index);
                scroll_rect = new Rectangle(0,
        y + old_h,
        max_pixel_width,
        move_height);
            }
            return max_pixel_width;
        }




        // 根据 offs 范围获得相关的 Line 列表
        // parameters:
        //      left_text [out] 返回命中的第一个 Line 处于 start 位置之前的局部文字内容
        //      right_text [out] 返回命中的最后一个 Line 处于 end 位置之后的局部文字内容
        //      first_line_index   [out] 命中的第一个 Line 的 index。若为 -1，表示没有命中的
        //      replaced    [out] 返回 start 和 end 之间即将被替换的部分内容
        // return:
        //      命中的 Line 列表
        public static List<IBox> FindFields(
            IEnumerable<IBox> _lines,
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

            var lines = new List<IBox>();
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

        static List<MarcField> BuildFields(
            FieldProperty fieldProperty,
SafeHDC dc,
string content,
int pixel_width,
SplitRange splitRange,
out int max_pixel_width)
        {
            max_pixel_width = 0;

            if (content == null)
            {
                return new List<MarcField>();
            }

            if (string.IsNullOrEmpty(content))
            {
                return new List<MarcField> { new MarcField(fieldProperty) };
            }

            // 将 content 按照 字段结束符 进行切分
            var parts = content.Split(new[] { fieldProperty.FieldEndChar },
                StringSplitOptions.RemoveEmptyEntries);
            var fields = new List<MarcField>();
            foreach (var part in parts)
            {
                if (string.IsNullOrEmpty(part))
                    continue;

                var field = new MarcField(fieldProperty);
                field.ReplaceText(dc,
                    0,
                    -1,
                    part,
                    pixel_width,
                    splitRange,
                    out string _,
                    out Rectangle update_rect,
                    out Rectangle scroll_rect,
                    out int scroll_distance);

                fields.Add(field);
            }

            return fields;
        }

#endif

        public int GetPixelWidth()
        {
            return _fields.Count == 0 ? 0 : _fields.Max(l => l.GetPixelWidth());
        }

        // 输入一个字符。根据当前位置，执行替换或者插入
        // parameters:
        //      action  输入的动作。
        //              "input" 表示输入一个字符
        //              "backspace" 表示回删一个字符
        //              "delete" 表示删除插入符右侧的一个字符
        //              "return" 表示插入一个换行符  
        public bool GetReplaceMode(HitInfo info,
            string action,
            out string fill_content)
        {
            fill_content = "";
            int fill_char_count = 0;

            if (action == "return")
                throw new ArgumentException("不允许使用 'return' action");

            var _global_offs = info.Offs;

            var replace = false;

            if (action == "input" || action == "delete")
            {
                // 如果在头标区的末尾，则调整为下一字符开头
                if (info.ChildIndex == 0 && info.TextIndex >= _fields[0].TextLength)
                {
                    info.ChildIndex++;
                    info.TextIndex = 0;
                }

                if (info.ChildIndex == 0)
                    replace = true;
                else
                {
                    var field = info.ChildIndex >= _fields.Count ? null : _fields[info.ChildIndex];
                    // var field_name = (field as MarcField).FieldName;
                    var is_control_field = field == null ? false : (field as MarcField).IsControlField;
                    if (is_control_field)
                    {
                        if (info.TextIndex < 3)
                            replace = true;
                    }
                    else
                    {
                        if (info.TextIndex < 5)
                            replace = true;
                    }
                }

                // replace 情况下检查已有内容字符是否足够
                if (replace)
                {
                    var length = SumTextLength(_fields, info.ChildIndex);
                    if (length <= _global_offs + 1)
                    {
                        fill_char_count = _global_offs + 1 - length;
                        fill_content = new string(' ', fill_char_count);
                    }
                }
            }
            else if (action == "backspace")
            {
                // 如果头标区后一字段的开头，则调整为头标区末尾。效果为抹去末尾这个字符为空
                if (info.TextIndex == 0 && info.ChildIndex == 1)
                {
                    return true;
                }
                // 如果为字段开头，则调整为上一字段末尾的 字段结束符右侧。那么 replace 就恒为 false
                if (info.TextIndex == 0 && info.ChildIndex > 1)
                {
                    /*
                    info.ChildIndex++;
                    var field = _fields[info.ChildIndex];
                    info.TextIndex = field.TextLength + 1;
                    */
                    return false;
                }

                if (info.ChildIndex == 0)
                    replace = true;
                else
                {
                    var field = _fields[info.ChildIndex];
                    // var field_name = (field as MarcField).FieldName;
                    var is_control_field = (field as MarcField).IsControlField;
                    if (is_control_field)
                    {
                        if (info.TextIndex > 0 && info.TextIndex < 3 + 1)
                            replace = true;
                    }
                    else
                    {
                        if (info.TextIndex > 0 && info.TextIndex < 5 + 1)
                            replace = true;
                    }
                }
            }
            else if (action == "return")
            {
                // 如果在头标区的末尾，则调整为下一字符开头
                if (info.ChildIndex == 0 && info.TextIndex >= _fields[0].TextLength)
                {
                    info.ChildIndex++;
                    info.TextIndex = 0;
                }
                else if (info.ChildIndex == 0 && info.TextIndex < 24)
                {
                    // 在头标区内回车，补足空格
                    fill_char_count = 24 - info.TextIndex;
                    fill_content = new string('_', fill_char_count);
                    info.ChildIndex++;
                    info.TextIndex = 0;
                    replace = true;
                    return replace;
                }

                // return 永远是插入
                replace = false;
            }

            return replace;
        }


        // 刷新所有字段的标题
        // parameters:
        //      update_rect [out] 返回实际需要更新的矩形区域
        public void UpdateAllCaption(SafeHDC dc,
            out Rectangle update_rect)
        {
            update_rect = System.Drawing.Rectangle.Empty;

            foreach (MarcField field in _fields)
            {
                field.RefreshCaptionText(dc,
    out Rectangle update_rect_caption);
                update_rect_caption.Offset(_fieldProperty.CaptionX, 0);
                update_rect = System.Drawing.Rectangle.Union(update_rect, update_rect_caption);
            }
        }
    }
}
