using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;

using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 一个简单的多行结构
    /// 以回车换行符拆分为若干 Paragraph
    /// </summary>
    public class SimpleText : IBox
    {
        public string Name { get; set; }

        IBox _parent = null;
        public IBox Parent => _parent;

        List<Paragraph> _paragraphs = new List<Paragraph>();

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

            int current_start_offs = 0;
            var block_start = Math.Min(blockOffs1, blockOffs2);
            var block_end = Math.Max(blockOffs1, blockOffs2);
            int i = 0;
            foreach (var paragraph in _paragraphs)
            {
                // 剪切区域下方的部分不必参与循环了
                if (y >= clipRect.Bottom)
                    break;

                // 如果选定的范围包括末尾的虚拟回车字符，则这个字符的选定背景色应该显示出来
                int virtual_return_length = 0;
                if (i != _paragraphs.Count - 1)
                    virtual_return_length = 1;   // 行末有个 \r 字符

                int paragraph_height = paragraph.GetPixelHeight();
                var rect = new Rectangle(x, y, Int32.MaxValue, paragraph_height);
                if (clipRect.IntersectsWith(rect))
                {
                    paragraph.Paint(
                        context,
                        dc,
                        x,
                        y,
                        clipRect,
                        block_start - current_start_offs,
                        block_end - current_start_offs,
                        virtual_return_length);
                }

                y += paragraph_height;
                current_start_offs += paragraph.TextLength + virtual_return_length; // 行末有个 \r 字符
                i++;
            }
        }

        // 切割为子字段。\ 符号和以后的属于一个完整子字段。切割在 \ 符号之前发生
        // 子字段符号独立切割出来
        public static string[] SplitSubfields(string text,
    char delimeter = '\\')
        {
            if (text == null)
                return new string[0];

            List<string> lines = new List<string>();
            StringBuilder line = null;  // new StringBuilder();
            foreach (var ch in text)
            {
                /*
                if (paragraph == null)
                    paragraph = new StringBuilder();
                */
                if (ch == delimeter)
                {
                    if (line != null && line.Length > 0)
                        lines.Add(line.ToString());
                    line = new StringBuilder();
                    line.Append(ch);
                    lines.Add(line.ToString());
                    line = null;
                }
                else
                {
                    if (line == null)
                        line = new StringBuilder();
                    line.Append(ch);
                }

            }
            if (line != null && line.Length > 0)
                lines.Add(line.ToString());

            if (lines.Count == 0)
                lines.Add("");

            return lines.ToArray();
        }

        public static Segment[] SegmentSubfields(string text,
            char delimeter = '\\',
            object delimeter_tag = null)
        {
            if (text == null)
                return new Segment[0];

            var lines = new List<Segment>();
            StringBuilder line = null;  // new StringBuilder();
            foreach (var ch in text)
            {
                /*
                if (paragraph == null)
                    paragraph = new StringBuilder();
                */
                if (ch == delimeter)
                {
                    if (line != null && line.Length > 0)
                        lines.Add(new Segment
                        {
                            Text = line.ToString(),
                            Tag = null,
                        });
                    line = new StringBuilder();
                    line.Append(ch);
                    lines.Add(new Segment
                    {
                        Text = line.ToString(),
                        Tag = delimeter_tag,    // 分隔符
                    });
                    line = null;
                }
                else
                {
                    if (line == null)
                        line = new StringBuilder();
                    line.Append(ch);
                }

            }
            if (line != null && line.Length > 0)
                lines.Add(new Segment
                {
                    Text = line.ToString(),
                    Tag = null,
                });

            if (lines.Count == 0)
                lines.Add(new Segment
                {
                    Text = "",
                    Tag = null,
                });

            return lines.ToArray();
        }


        // 子字段符号和子字段名粘连在一起
        public static string[] SplitSubfields2(string text,
    char delimeter = '\\',
    int name_chars = 2)
        {
            if (text == null)
                return new string[0];

            List<string> lines = new List<string>();
            StringBuilder line = null;
            bool is_head_delimeter = false; // 指示 line 内容中第一字符是为 delemiter
            foreach (var ch in text)
            {
                /*
                if (paragraph == null)
                    paragraph = new StringBuilder();
                */
                if (ch == delimeter)
                {
                    if (line != null && line.Length > 0)
                    {
                        lines.Add(line.ToString());
                    }

                    line = new StringBuilder();
                    line.Append(ch);
                    is_head_delimeter = true;
                }
                else
                {
                    if (line == null)
                    {
                        line = new StringBuilder();
                        is_head_delimeter = false;
                    }
                    line.Append(ch);
                }

                // 因为(分隔符引导的一段内容)长度到了，主动切割
                if (is_head_delimeter
                    && line.Length >= name_chars)
                {
                    lines.Add(line.ToString());
                    line = null;
                    is_head_delimeter = false;
                }
            }

            if (line != null)
                lines.Add(line.ToString());

            if (lines.Count == 0)
                lines.Add("");
            return lines.ToArray();
        }

        // 子字段符号和子字段名粘连在一起
        public static Segment[] SegmentSubfields2(string text,
    char delimeter = '\\',
    int name_chars = 2,
    object delimeter_tag = null)
        {
            if (text == null)
                return new Segment[0];

            var lines = new List<Segment>();
            StringBuilder line = null;
            bool is_head_delimeter = false; // 指示 line 内容中第一字符是为 delemiter
            foreach (var ch in text)
            {
                /*
                if (paragraph == null)
                    paragraph = new StringBuilder();
                */
                if (ch == delimeter)
                {
                    if (line != null && line.Length > 0)
                    {
                        lines.Add(new Segment
                        {
                            Text = line.ToString(),
                            Tag = is_head_delimeter ? delimeter_tag : null,
                        });
                    }

                    line = new StringBuilder();
                    line.Append(ch);
                    is_head_delimeter = true;
                }
                else
                {
                    if (line == null)
                    {
                        line = new StringBuilder();
                        is_head_delimeter = false;
                    }
                    line.Append(ch);
                }

                // 因为(分隔符引导的一段内容)长度到了，主动切割
                if (is_head_delimeter
                    && line.Length >= name_chars)
                {
                    lines.Add(new Segment
                    {
                        Text = line.ToString(),
                        Tag = is_head_delimeter ? delimeter_tag : null,
                    });
                    line = null;
                    is_head_delimeter = false;
                }
            }

            if (line != null)
                lines.Add(new Segment
                {
                    Text = line.ToString(),
                    Tag = is_head_delimeter ? delimeter_tag : null,
                });

            if (lines.Count == 0)
                lines.Add(new Segment
                {
                    Text = "",
                    Tag = null,
                });
            return lines.ToArray();
        }


        public static string[] SplitLines(string text,
            char delimeter = '\r',
            bool contain_return = true)
        {
            List<string> lines = new List<string>();
            StringBuilder line = new StringBuilder();
            foreach (var ch in text)
            {
                /*
                if (paragraph == null)
                    paragraph = new StringBuilder();
                */
                if (ch == delimeter)
                {
                    if (contain_return)
                        line.Append(ch);
                    lines.Add(line.ToString());
                    line = new StringBuilder();
                }
                else
                    line.Append(ch);

            }
            if (line != null/* && line.Length > 0*/)
                lines.Add(line.ToString());

            return lines.ToArray();
        }

        public int Initialize(SafeHDC dc,
            string content,
            int pixel_width,
            IContext context)
        {
            this.Clear();

            var lines = SplitLines(content.Replace("\r\n", "\r"),
                '\r',
                false);
            int max_pixel_width = pixel_width;
            foreach (var line in lines)
            {
                var paragraph = new Paragraph(this);
                var width = paragraph.Initialize(dc, line, pixel_width, context);
                if (width > max_pixel_width)
                    max_pixel_width = width;
                _paragraphs.Add(paragraph);
            }

            return max_pixel_width;
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
            var update_rect = System.Drawing.Rectangle.Empty;
            /*
            scroll_rect = System.Drawing.Rectangle.Empty;
            scroll_distance = 0;
            replaced = "";
            */
            var result = new ReplaceTextResult();

            // 先分别定位到 start 所在的 Paragraph，和 end 所在的 Paragraph
            // 观察被替换的范围 start - end，跨越了多少个 Paragraph
            // 对 text 进行 \r 字符切分。把这些 Paragraph 替换为由切割后的 text 构造的零到多个新 Paragraph

            var old_paragraphs = FindParagraphs(
                _paragraphs,
                start,
                out string left_text,
                end,
                out string right_text,
                out int first_paragraph_index,
                out string replaced);

            if (start == 0 && end == -1 /*&& first_paragraph_index == -1*/)
            {
                first_paragraph_index = 0;
                // _paragraphs.Clear();
            }

            result.ReplacedText = replaced;

            string content = left_text + text + right_text;

            var new_paragraphs = new List<Paragraph>();
            int max_pixel_width = pixel_width;
            if (string.IsNullOrEmpty(content) == false)
            {
                content = content.Replace("\r\n", "\r");

                // 去掉 right_text 末尾的 \r 字符。避免 SplitLine 多生成一个 Paragrapn
                if (content.EndsWith("\r"))
                    content = content.Substring(0, content.Length - 1);

                var lines = SplitLines(content,
                    '\r',
                    false);
                foreach (var line in lines)
                {
                    var paragraph = new Paragraph(this);
                    var width = paragraph.Initialize(dc, line, pixel_width, context);
                    if (width > max_pixel_width)
                        max_pixel_width = width;
                    new_paragraphs.Add(paragraph);
                }
            }

            if (old_paragraphs.Count > 0)
                _paragraphs.RemoveRange(first_paragraph_index, old_paragraphs.Count);
            if (new_paragraphs.Count > 0)
            {
                Debug.Assert(first_paragraph_index >= 0);
                _paragraphs.InsertRange(first_paragraph_index, new_paragraphs);
            }

            // update_rect 用 old_paragraphs 和 new_paragraphs 两个矩形的较大一个算出
            // 矩形宽度最后用 max_pixel_width 矫正一次
            int old_h = old_paragraphs.Sum(p => p.GetPixelHeight());
            int new_h = new_paragraphs.Sum(p => p.GetPixelHeight());
            int y = SumHeight(_paragraphs, first_paragraph_index);

            update_rect = new Rectangle(0,
                y,
                max_pixel_width,    // TODO: 这里有问题, max_pixel_width 统计时有没有可能没有能包括全部 Paragraph 的最大像素宽度
                Math.Max(old_h, new_h));

            result.ScrolledDistance = new_h - old_h;
            if (result.ScrolledDistance != 0)
            {
                int move_height = SumHeight(_paragraphs, first_paragraph_index, _paragraphs.Count - first_paragraph_index);
                result.ScrollRect = new Rectangle(0,
        y + old_h,
        max_pixel_width,
        move_height);
            }

            ProcessBaseline();
            result.MaxPixel = max_pixel_width;
            return result;
        }

        static int SumHeight(List<Paragraph> paragraphs, int count)
        {
            int height = 0;
            for (int i = 0; i < count; i++)
            {
                height += paragraphs[i].GetPixelHeight();
            }
            return height;
        }

        static int SumHeight(List<Paragraph> paragraphs, int start, int count)
        {
            int height = 0;
            for (int i = start; i < start + count; i++)
            {
                height += paragraphs[i].GetPixelHeight();
            }
            return height;
        }

        // 根据 offs 范围获得相关的 Paragraph 列表
        // parameters:
        //      left_text [out] 返回命中的第一个 Paragraph 处于 start 位置之前的局部文字内容
        //      right_text [out] 返回命中的最后一个 Paragraph 处于 end 位置之后的局部文字内容
        //      first_paragraph_index   [out] 命中的第一个 Paragraph 的 index。若为 -1，表示没有命中的
        //      replaced    [out] 返回 start 和 end 之间即将被替换的部分内容
        // return:
        //      命中的 Paragraph 列表
        public static List<Paragraph> FindParagraphs(
            List<Paragraph> _paragraphs,
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
                replaced = MergeText(_paragraphs);
                return new List<Paragraph>();
            }
            */

            if (end == -1)
                end = int.MaxValue;

            Debug.Assert(start <= end);
            if (start > end)
                throw new ArgumentException($"start {start} must less than end {end}");

            StringBuilder replaced_part = new StringBuilder();

            var paragraphs = new List<Paragraph>();
            int offs = 0;
            int i = 0;
            foreach (var paragraph in _paragraphs)
            {
                if (offs > end)
                    break;

                bool is_tail = IsTailParagraph(i);
                // 段落文字最后隐含的 \r 字符个数
                int return_length = is_tail ? 0 : 1;
                int paragraph_text_length = paragraph.TextLength;


                // 命中
                if (offs <= end && offs + paragraph_text_length + return_length >= start)
                {
                    var text = GetParagraphText(i);
                    if (paragraphs.Count == 0)
                    {
                        left_text = text.Substring(0, start - offs);
                        first_paragraph_index = i;
                    }

                    // right_text 不断被更新，只要留下最后一次的值即可
                    if (offs + paragraph_text_length + return_length >= end)
                        right_text = text.Substring(end - offs);
                    paragraphs.Add(paragraph);

                    {
                        var part_start = Math.Max(start - offs, 0);
                        var part_length = paragraph_text_length + return_length - part_start;
                        if (offs + paragraph_text_length + return_length >= end)
                            part_length = (end - offs) - part_start;
                        if (part_length > 0)
                            replaced_part.Append(text.Substring(part_start, part_length));
                    }
                }

                offs += paragraph_text_length + return_length;
                i++;
            }

            replaced = replaced_part.ToString();
            return paragraphs;

            string GetParagraphText(int index)
            {
                if (IsTailParagraph(index))
                    return _paragraphs[index].MergeText();
                return _paragraphs[index].MergeText() + "\r";
            }

            bool IsTailParagraph(int j)
            {
                return j == _paragraphs.Count - 1;
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

        // 以第一行 Line 的基线为基线
        void ProcessBaseline()
        {
            if (this._paragraphs == null || this._paragraphs.Count == 0)
            {
                _baseLine = 0;
                _below = 0;
                return;
            }
            _baseLine = this._paragraphs[0].BaseLine;
            _below = this._paragraphs[0].Below;  // TODO: 加上除第一行以外的所有行的高度?
        }



        public void Clear()
        {
            _paragraphs.Clear();
        }

        // 根据单行行高计算出 Paragraph 总的像素高度
        public int GetPixelHeight(/*int line_height*/)
        {
            if (_paragraphs == null)
                return 0;
            return _paragraphs.Sum(p => p.GetPixelHeight(/*line_height*/));
        }

        public int GetPixelWidth()
        {
            return _paragraphs.Count == 0 ? 0 : _paragraphs.Max(p => p.GetPixelWidth());
        }

        // parameters:
        //      int x   注意这是 Paragraph 内文档坐标
        public HitInfo HitTest(int x,
            int y/*,
            int line_height*/)
        {
            var result = new HitInfo();
            int current_y = 0;
            int paragraph_start_offs = 0;
            for (int i = 0; i < _paragraphs.Count; i++)
            {
                var paragraph = _paragraphs[i];
                var paragraph_height = paragraph.GetPixelHeight(/*line_height*/);
                bool isLastLine = (i == _paragraphs.Count - 1);
                if (y < current_y)
                    break;
                if (y >= current_y && (y < current_y + paragraph_height || isLastLine))
                {
                    var ret = paragraph.HitTest(x,
                        y - current_y/*,
                        line_height*/);
                    return new HitInfo
                    {
                        X = ret.X,
                        Y = current_y + ret.Y,
                        ChildIndex = i,
                        Area = ret.Area,    // y < current_y + paragraph_height ? Area.Text : Area.BottomBlank,
                        TextIndex = ret.Offs,
                        Offs = paragraph_start_offs + ret.Offs,
                        LineHeight = ret.LineHeight,
                    };
                }

                current_y += paragraph.GetPixelHeight();
                paragraph_start_offs += paragraph.TextLength;
                if (i < _paragraphs.Count - 1)
                    paragraph_start_offs++; // 自然段末尾 \n 字符
            }

            // 空白内容
            return new HitInfo { Area = Area.BottomBlank };
        }

#if REMOVED
        // 根据 info 对象的 .ChildIndex  和 .TextIndex 计算出全局偏移量
        public int GetGlobalOffs(HitInfo info)
        {
            int offs = 0;
            for (int i = 0; i < _paragraphs.Count; i++)
            {
                var paragraph = _paragraphs[i];
                if (i == info.ChildIndex)
                {
                    return offs + info.TextIndex;
                }
                offs += paragraph.TextLength; // 累加本行的文字长度
                if (i < _paragraphs.Count - 1)
                    offs++; // 自然段末尾包含一个 \n 字符
            }

            return offs; // 没有行命中，所以 info.TextIndex 不采纳
        }
#endif

        // **** 准备废弃
        // TODO: 如果抛出异常，应当捕获以后，用 bitmap 显示到窗口中
        // 注意抛出异常时，尽量保护好原有数据，让编辑器功能尽量正常。可以用一个特定长度就会触发的异常来进行测试验证
        // 以全局偏移量为参数，获得点击位置
        public HitInfo HitByGlobalOffs(int offs_param,
            bool trailing)
        {
            /*
            if (_paragraphs.Count > 0 && _paragraphs[0].TextLength > 10)
                throw new Exception("测试异常");
            */

            if (_paragraphs.Count == 0)
                return new HitInfo
                {
                    X = 0,
                    Y = 0,
                    Area = Area.BottomBlank,
                    ChildIndex = 0,
                    TextIndex = 0
                };

            HitInfo info = new HitInfo();
            int offs = 0;
            Paragraph paragraph = null;
            int start_x = 0;
            int start_y = 0;
            for (int i = 0; i < _paragraphs.Count; i++)
            {
                // info.RangeIndex = 0;
                var isLastParagraph = (i == _paragraphs.Count - 1);
                paragraph = _paragraphs[i];
                var paragraph_text_length = paragraph.TextLength;
                int tail_length = 0;
                if (i < _paragraphs.Count - 1)
                    tail_length = 1;    // 自然段末尾包含一个 \n 字符
                if (/*offs <= offs_param
                &&*/ offs + paragraph_text_length >= offs_param)
                {
                    var ret = paragraph.HitByGlobalOffs(offs_param - offs,
                        trailing);
                    // var paragraph_offs = paragraph.GetGlobalOffs(ret);
                    var paragraph_offs = ret.Offs;
                    ret.X += start_x;
                    ret.Y += start_y;
                    ret.ChildIndex = i;
                    ret.TextIndex = paragraph_offs;
                    return ret;
                }

#if REMOVED
                if (isLastParagraph
                    && /*offs <= offs_param
    && */offs + paragraph_text_length >= offs_param)
                {
                    var ret = paragraph.HitByGlobalOffs(offs_param - offs/*,
                        line_height*/);
                    var paragraph_offs = paragraph.GetGlobalOffs(ret);
                    ret.X += start_x;
                    ret.ChildIndex = i;
                    ret.TextIndex = paragraph_offs;
                    return ret;
                }
#endif

                if (i >= _paragraphs.Count - 1)
                    break;
                start_y += paragraph.GetPixelHeight(/*line_height*/);

                info.ChildIndex++;

                offs += paragraph_text_length + tail_length; // 累加本行的文字长度
            }



            info = paragraph.HitByGlobalOffs(offs,
                trailing);   // 只取 .X .Y
            info.X += start_x;
            info.Y += start_y;
            info.Area = Area.BottomBlank;
            info.ChildIndex = _paragraphs.Count - 1; // 最后一行
            info.TextIndex = paragraph.TextLength; // 最后一行的最后一个字符
            return info;
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
            Paragraph paragraph = null;
            int start_y = 0;
            for (int i = 0; i < _paragraphs.Count; i++)
            {
                // info.RangeIndex = 0;
                paragraph = _paragraphs[i];
                var paragraph_text_length = paragraph.TextLength;
                if (offs_param + direction >= offs && offs_param + direction <= offs + paragraph_text_length)
                {
                    var ret = paragraph.MoveByOffs(offs_param - offs,
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

                offs += paragraph_text_length;
                if (i != _paragraphs.Count - 1)
                    offs += 1;  // paragraph 末尾有一个 \n 字符

                start_y += paragraph.GetPixelHeight();
            }

            if (infos.Count > 0)
            {
                info = infos[infos.Count - 1];
                return 0;
            }

            info.Area = Area.BottomBlank;
            return 1;
        }

        // 获得一段文本显示范围的 Region
        public Region GetRegion(int start_offs = 0, 
            int end_offs = int.MaxValue,
            int virtual_tail_length = 0)
        {
            if (end_offs < start_offs)
                throw new ArgumentException($"start_offs ({start_offs}) 必须小于或等于 end_offs ({end_offs})");

            if (this._paragraphs?.Count == 0)
                return null;

            if (start_offs == end_offs)
                return null;
            if (end_offs <= 0)
                return null;
            if (start_offs >= this.TextLength)
                return null;

            Region region = null;
            int current_offs = 0;
            int y = 0;
            int i = 0;
            foreach (var line in this._paragraphs)
            {
                bool is_tail = IsTailParagraph(i);
                // 段落文字最后隐含的 \r 字符个数
                int return_length = is_tail ? 0 : 1;

                var result = line.GetRegion(start_offs - current_offs,
                    end_offs - current_offs,
                    is_tail ? 0 : virtual_tail_length);
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
                current_offs += line.TextLength + return_length;
                y += line.GetPixelHeight();
                i++;
            }

            return region;
            bool IsTailParagraph(int j)
            {
                return j == _paragraphs.Count - 1;
            }
        }


#if REMOVED
        // 注意函数中不要改变 info 内容
        public bool CanDown(HitInfo info)
        {
            if (info.ChildIndex < this._paragraphs.Count - 1)
                return true;
            if (info.ChildIndex == this._paragraphs.Count - 1)
            {
                var paragraph = _paragraphs[this._paragraphs.Count - 1];
                var start_y = GetStartY(paragraph);
                var temp_info = info.Clone();
                temp_info.Y -= start_y;
                return paragraph.CanDown(temp_info);
            }
            return false;
        }

        // 注意函数中不要改变 info 内容
        public bool CanUp(HitInfo info)
        {
            if (info.ChildIndex > 0)
                return true;
            if (info.ChildIndex == 0)
            {
                var paragraph = _paragraphs[0];
                var start_y = GetStartY(paragraph);
                var temp_info = info.Clone();
                temp_info.Y -= start_y;
                temp_info.ChildIndex = ?;
                temp_info.TextInfo = ?;
                return paragraph.CanUp(temp_info);
            }
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
            y += FontContext.DefaultFontHeight;
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
            y -= FontContext.DefaultFontHeight;
            if (y < 0)
                return false;
            info = this.HitTest(x, y);
            return true;
        }


#if REMOVED
        // parameters:
        //      x   最近一次左右移动插入符之后，插入符的 x 位置
        public bool CaretMoveDown(
            int x,
            ref HitInfo info,
            ref int global_offs)
        {
            //if (CanDown(info) == false)
            //    return false;
            var paragraph = _paragraphs[info.ChildIndex];
            var start_y = GetStartY(paragraph);

            var temp = paragraph.HitByGlobalOffs(info.TextIndex, false);

            var result = info.Clone();
            result.Y = info.Y - start_y;
            result.ChildIndex = temp.ChildIndex;
            result.TextIndex = temp.TextIndex;
            var ret = paragraph.CaretMoveDown(x,
                ref result,
                ref global_offs);
            if (ret == false)
            {
                if (info.ChildIndex >= _paragraphs.Count - 1)
                    return false;
                // 改为 HitTest paragraph 的下方一个 paragraph
                paragraph = _paragraphs[info.ChildIndex + 1];
                // 计划命中第一行
                var hit = paragraph.HitTest(x, 0);
                info.X = hit.X;
                info.Y = hit.Y + GetStartY(paragraph);
                info.ChildIndex = info.ChildIndex + 1;
                info.TextIndex = paragraph.GetGlobalOffs(hit);
                global_offs = GetGlobalOffs(info);
                return true;
            }
            info.Y = result.Y + start_y;
            info.X = result.X;
            // info.ChildIndex 不变
            info.TextIndex = paragraph.GetGlobalOffs(result);
            global_offs = GetGlobalOffs(info);
            return true;
        }

        // parameters:
        //      x   最近一次左右移动插入符之后，插入符的 x 位置
        public bool CaretMoveUp(
            int x,
            ref HitInfo info,
            ref int global_offs)
        {
            //if (CanUp(info) == false)
            //    return false;
            var paragraph = _paragraphs[info.ChildIndex];
            var start_y = GetStartY(paragraph);

            var temp = paragraph.HitByGlobalOffs(info.TextIndex, false);

            var result = info.Clone();
            result.Y = info.Y - start_y;
            result.ChildIndex = temp.ChildIndex;
            result.TextIndex = temp.TextIndex;
            var ret = paragraph.CaretMoveUp(x,
                ref result,
                ref global_offs);
            if (ret == false)
            {
                if (info.ChildIndex == 0)
                    return false;
                // 改为 HitTest paragraph 的上方一个 paragraph
                paragraph = _paragraphs[info.ChildIndex - 1];
                // 计划命中最后一行
                var hit = paragraph.HitTest(x, paragraph.GetPixelHeight() - 1);
                info.X = hit.X;
                info.Y = hit.Y + GetStartY(paragraph);
                info.ChildIndex = info.ChildIndex - 1;
                info.TextIndex = paragraph.GetGlobalOffs(hit);
                global_offs = GetGlobalOffs(info);
                return true;
            }
            info.Y = result.Y + start_y;
            info.X = result.X;
            // info.ChildIndex 不变
            info.TextIndex = paragraph.GetGlobalOffs(result);
            global_offs = GetGlobalOffs(info);
            return true;
        }

#endif

        // 获得一个 Paragraph 的起点 Y
        int GetStartY(Paragraph paragraph)
        {
            int y = 0;
            foreach (var current in _paragraphs)
            {
                if (current == paragraph)
                    return y;
                y += current.GetPixelHeight();
            }

            return y;
        }

        public int TextLength
        {
            get
            {
                if (_paragraphs == null)
                    return 0;
                int length = 0;
                int i = 0;
                foreach (var paragraph in _paragraphs)
                {
                    length += paragraph.TextLength;
                    if (i < _paragraphs.Count - 1)
                        length++;  // 自然段最后有一个 \n 字符
                    i++;
                }
                return length;
            }
        }

        public string MergeText(int start = 0, int end = int.MaxValue)
        {
            return MergeText(_paragraphs, start, end);
        }

        // 注意包含行末 \n
        public static string MergeText(List<Paragraph> _paragraphs,
            int start = 0, int end = int.MaxValue)
        {
            StringBuilder builder = new StringBuilder();
            int i = 0;
            int offs = 0;
            foreach (var paragraph in _paragraphs)
            {
                var current_length = paragraph.TextLength;
                builder.Append(paragraph.MergeText(start - offs, end - offs));
                offs += current_length;
                if (i < _paragraphs.Count - 1)
                {
                    if (InRange(offs, start, end))
                        builder.Append('\r');   // TODO: \r 全部用常量定义
                    offs++;
                }
                i++;
                if (offs > end)
                    break;
            }

            return builder.ToString();

            bool InRange(int offs0, int start0, int end0)
            {
                if (end0 == -1)
                    return offs0 >= start0;
                return offs0 >= start0 && offs0 < end0;
            }
        }

        public void ClearCache()
        {
            if (_paragraphs == null)
                return;
            foreach (var line in _paragraphs)
            {
                line.ClearCache();
            }
        }
        public int LineCount
        {
            get
            {
                return _paragraphs.Count;
            }
        }
    }
}
