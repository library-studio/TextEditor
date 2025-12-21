using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// MARC 字段编辑区域
    /// 由一个 Line(字段名)，一个 Line(字段指示符)，一个 Paragraph (字段内容) 构成
    /// </summary>
    public class MarcField : IBox
    {
        public string Name { get; set; }

        public IBox Parent => _record;

        internal MarcRecord _record = null;

        Line _caption;
        Line _name;
        Line _indicator;
        Paragraph _content;

        // 引用字段共同属性
        Metrics _fieldProperty;

        public bool IsHeader { get; set; }

        void EnsureCaption()
        {
            if (_caption == null)
                _caption = new Line(this)
                {
                    Name = "caption",
                    TextAlign = TextAlign.None
                };
        }

        void EnsureName()
        {
            if (_name == null)
                _name = new Line(this)
                {
                    Name = "name",
                    TextAlign = TextAlign.Left | TextAlign.OverflowCenter
                };
        }

        void EnsureIndicator()
        {
            if (_indicator == null)
                _indicator = new Line(this)
                {
                    Name = "indicator",
                    TextAlign = TextAlign.Left | TextAlign.OverflowCenter
                };
        }

        void EnsureContent()
        {
            if (_content == null)
            {
                _content = new Paragraph(this)
                {
                    Name = "content",
                };
            }
        }

        public MarcField(MarcRecord record,
            Metrics property)
        {
            _record = record;
            _fieldProperty = property;
            // ProcessBaseline();  // testing
        }

        public int TextLength => (NameTextLength)
            + (IndicatorTextLength)
            + (ContentTextLength)
            + (this.IsHeader ? 0 : 1);

        public int PureTextLength => (NameTextLength)
            + (IndicatorTextLength)
            + (ContentTextLength);

        public int FullTextLength
        {
            get
            {
                if (this.IsHeader)
                    return ContentTextLength;
                return (NameTextLength)
            + (IndicatorTextLength)
            + (ContentTextLength)
            + 1;
            }
        }

        public bool CaretMoveDown(int x, int y, out HitInfo info)
        {
            if (y < 0)
                y = 0;

            var rect = GetNameRect();
            if (x < rect.Right
                && _name != null)
            {
                // ChildIndex 中要填入表示所在位置的数字
                var ret = _name.CaretMoveDown(x - rect.X,
                    y - rect.Y,
                    out info);
                info.X += rect.X;
                info.Y += rect.Y;
                info.ChildIndex = 0;
                info.LineHeight = FirstLineCaretHeight(_name);
                return ret;
            }

            rect = GetIndicatorRect();
            if (x < rect.Right
                && _indicator != null)
            {
                var ret = _indicator.CaretMoveDown(x - rect.X,
                    y - rect.Y,
                    out info);
                info.X += rect.X;
                info.Y += rect.Y;
                info.ChildIndex = 1;
                info.LineHeight = FirstLineCaretHeight(_indicator);
                return ret;
            }

            if (_content != null)
            {
                var x0 = GetContentX();
                var y0 = GetContentY();
                var ret = _content.CaretMoveDown(x - x0,
                    y - y0, // Math.Max(0, y - y0),    // 避免在上沿以上
                    out info);
                info.X += x0;
                info.Y += y0;
                info.ChildIndex = 2;
                // 保持 info.LineHeight
                return ret;
            }

            info = new HitInfo();
            return false;
        }

        public bool CaretMoveUp(int x, int y, out HitInfo info)
        {
            x -= GetNameX();
            if (x < _fieldProperty.NamePixelWidth
                && _name != null)
            {
                var rect = GetNameRect();
                var ret = _name.CaretMoveUp(x - rect.X,
                    y - rect.Y,
                    out info);
                info.X += rect.X;
                info.Y += rect.Y;
                info.ChildIndex = 0;
                info.LineHeight = FirstLineCaretHeight(_name);
                return ret;
            }
            x -= _fieldProperty.NamePixelWidth;
            if (x < _fieldProperty.IndicatorPixelWidth
                && _indicator != null)
            {
                var rect = GetIndicatorRect();
                var ret = _indicator.CaretMoveUp(x - rect.X,
                    y - rect.Y,
                    out info);
                info.X += rect.X;
                info.Y += rect.Y;
                info.ChildIndex = 1;
                info.LineHeight = FirstLineCaretHeight(_indicator);
                return ret;
            }

            if (_content != null)
            {
                var x0 = GetContentX();
                var y0 = GetContentY();
                x -= _fieldProperty.IndicatorPixelWidth;
                var ret = _content.CaretMoveUp(x - x0,
                    Math.Max(0, y - y0),
                    out info);
                info.X += x0;
                info.Y += y0;
                info.ChildIndex = 2;
                // 保持 info.LineHeight()
                return ret;
            }

            info = new HitInfo();
            return false;
        }

        public void Clear()
        {
            _caption?.Clear();
            _name?.Clear();
            _indicator?.Clear();
            _content?.Clear();
            ClearCacheHeight();
        }

        // 缓冲累加以后的值。当任何一个部分文字改变以后，清除缓冲
        int _cachePixelHeight = -1; // -1 表示为非法值，需要重新计算

        void ClearCacheHeight()
        {
            _cachePixelHeight = -1;
        }

        public int GetPixelHeight()
        {
            if (_cachePixelHeight == -1)
            {
                int height = 0; // Line.GetLineHeight();
                if (_content != null)
                    height = Math.Max(height, _content.GetPixelHeight() + GetContentY());
                if (this.IsHeader == false)
                {
                    if (_name != null)
                        height = Math.Max(height, _name.GetPixelHeight() + VerticalUnit() / 2 + _fieldProperty.BorderThickness * 2 + GetNameRect().Y);
                    if (_indicator != null && this.IsControlField == false)
                        height = Math.Max(height, _indicator.GetPixelHeight() + VerticalUnit() / 2 + _fieldProperty.BorderThickness * 2 + GetIndicatorRect().Y);
                }
                _cachePixelHeight = height;
                return height;
            }

            return _cachePixelHeight;
        }

        // 注意对于 _name _indicator _content 为 null 的时候，相应区域不应该允许点击，而要转移到最近相邻的一个区域返回点击信息
        public HitInfo HitTest(int x, int y)
        {
            int caption_area_hitted = 0;
            // 点击到了左边 Caption 区域
            if (x < _fieldProperty.CaptionPixelWidth - _fieldProperty.SplitterPixelWidth)
            {
                /*
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
                */
                caption_area_hitted = -2;
                // 继续向后处理，但在返回前有所区别
                // 效果就是好像点击了 name indicator content 等部位，只是 ChildIndex 指示了 -2
                // 这样做的目的是，让返回信息足够丰富，调主可以当作点击 name indicator content 等部位来设置 caret；也可以当作点击了提示区而做出特别处理
            }

            // 点击到了 Caption 区域和 Name 区域的缝隙位置
            if (x < _fieldProperty.CaptionPixelWidth)
            {
                /*
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
                */
                caption_area_hitted = -1;
            }

            var start = _fieldProperty.NameBorderX; // 这里包括了左侧边沿空间
            // must 表示“其它 box 为空，必须用当前这个来 hittest”
            var must = _indicator == null && _content == null;
            if (IsHeader == false && _name != null
                && (x < start + _fieldProperty.NamePixelWidth || must))
            {
                var rect = GetNameRect();
                var info = _name.HitTest(x - rect.X, y - rect.Y);
                info.X += rect.X;
                info.Y += rect.Y;
                info.ChildIndex = caption_area_hitted != 0 ? caption_area_hitted : 0; // 0 表示 _name
                info.TextIndex = info.Offs;
                info.Offs += 0; // 保持原来的偏移量
                info.LineHeight = FirstLineCaretHeight(_name);
                return info;
            }
            start += _fieldProperty.NamePixelWidth;
            Debug.Assert(start == _fieldProperty.IndicatorBorderX);
            must = _content == null;
            if (IsHeader == false && _indicator != null && this.IsControlField == false
                && (x < start + _fieldProperty.IndicatorPixelWidth || must))
            {
                var rect = GetIndicatorRect();
                var info = _indicator.HitTest(x - rect.X, y - rect.Y);
                info.X += rect.X;
                info.Y += rect.Y;
                info.ChildIndex = caption_area_hitted != 0 ? caption_area_hitted : 1; // 1 表示 _indicator
                info.TextIndex = info.Offs;
                info.Offs += NameTextLength;
                info.LineHeight = FirstLineCaretHeight(_indicator);
                return info;
            }
            start += _fieldProperty.IndicatorPixelWidth;
            Debug.Assert(start == _fieldProperty.ContentBorderX);
            if (_content != null)
            {
                var x0 = GetContentX();
                var y0 = GetContentY();
                // 如果 y-y0 小于 0，表示点击在 _content 的上沿一点空间内，要调整为 0 调用
                var info = _content.HitTest(x - x0, Math.Max(0, y - y0));
                info.X += x0;
                info.Y += y0;
                info.ChildIndex = caption_area_hitted != 0 ? caption_area_hitted : 2; // 2 表示 _content
                info.TextIndex = info.Offs;
                info.Offs += NameTextLength + IndicatorTextLength;
                // 保持 info.LineHeight
                return info;
            }

            /*
            // TODO: 这时需要从右向左探测，找到一个有内容的区域
            List<IBox> boxes = new List<IBox>();
            if (_content != null)
                boxes.Add(_content);
            if (_indicator != null)
                boxes.Add(_indicator);
            if (_name != null)
                boxes.Add(_name);

            foreach (var box in boxes)
            {
                var info = box.HitTest(x - start, y);
                info.X += start;
                info.ChildIndex = 2; // 2 表示 _content
                info.TextIndex = info.Offs;
                info.Offs += _name.TextLength + _indicator.TextLength;
                return info;
            }
            */
            return new HitInfo();
        }

        int NameTextLength
        {
            get
            {
                if (_name == null)
                    return 0;
                return _name.TextLength;
            }
        }

        int IndicatorTextLength
        {
            get
            {
                if (_indicator == null)
                    return 0;
                return _indicator.TextLength;
            }
        }

        // 不包含字段结束符
        int ContentTextLength
        {
            get
            {
                return _content?.TextLength ?? 0;
            }
        }

        // 包含结束符
        public string MergeFullText(int start = 0,
            int end = int.MaxValue)
        {
            int offs = 0;
            var name_text = _name?.MergeText(start, end);
            if (_name != null)
            {
                var current_length = _name.TextLength;
                start -= current_length;
                if (end != -1)
                    end -= current_length;
                offs += current_length;
            }
            var indicator_text = _indicator?.MergeText(start, end);
            if (_indicator != null)
            {
                var current_length = _indicator.TextLength;
                start -= current_length;
                if (end != -1)
                    end -= current_length;
                offs += current_length;
            }

            var content_text = _content?.MergeText(start, end);
            if (_content != null)
            {
                var current_length = _content?.TextLength ?? 0;
                start -= current_length;
                if (end != -1)
                    end -= current_length;
                offs += current_length;
            }

            // 除了头标区以外，每个字段末尾都有一个字段结束符
            var terminator = "";
            if (IsHeader == false)
            {
                if (InRange(0, start, end))
                    terminator = new string(Metrics.FieldEndCharDefault, 1);
                offs++;
            }

            return (name_text ?? "")
                + (indicator_text ?? "")
                + (content_text ?? "")
                + (terminator ?? "");

            bool InRange(int offs0, int start0, int end0)
            {
                return offs0 >= start0 && offs0 < end0;
            }
        }

        public string MergeText(int start = 0, int end = int.MaxValue)
        {
            return MergeFullText(start, end);
        }

        // 返回的内容不包含字段结束符
        public string MergePureText(int start = 0, int end = int.MaxValue)
        {
            var name_text = _name?.MergeText(start, end);
            if (_name != null)
            {
                start -= _name.TextLength;
                if (end != -1)
                    end -= _name.TextLength;
            }
            var indicator_text = _indicator?.MergeText(start, end);
            if (_indicator != null)
            {
                start -= _indicator.TextLength;
                if (end != -1)
                    end -= _indicator.TextLength;
            }
            var content_text = _content?.MergeText(start, end);
            return (name_text ?? "")
                + (indicator_text ?? "")
                + (content_text ?? "");

            // return _name?.MergeText() + _indicator?.MergeText() + _content?.MergeText();
        }

        // 获得带有 Mask Char 的文本内容
        // 注意 mask char 是从 0x01~0x05 之间的字符，
        // 0x01~0x03 表示字段名位置, 0x04~0x05 表示指示符位置, 0x06 表示头标区位置(最多 24 个字符都是这个值)
        // 从 mask char 的值很容易看出字段名部分和指示符部分是否完整
        public string MergePureTextMask(int start = 0, int end = int.MaxValue)
        {
            if (end <= start || end <= 0)
                return "";

            var name_text = _name?.MergeText(start, end);
            if (_name != null)
            {
                start -= _name.TextLength;
                if (end != -1)
                    end -= _name.TextLength;
            }
            // 按照字符所在的位置，mask char 值有所不同
            name_text = GetMaskString(start, (char)0x01, name_text?.Length ?? 0);

            var indicator_text = _indicator?.MergeText(start, end);
            if (_indicator != null)
            {
                start -= _indicator.TextLength;
                if (end != -1)
                    end -= _indicator.TextLength;
            }
            indicator_text = GetMaskString(start, (char)0x04, indicator_text?.Length ?? 0);

            var content_text = _content?.MergeText(start, end);
            if (this.IsHeader)
                content_text = GetHeaderMaskString(content_text?.Length ?? 0); // TODO: 可以改进为最多转换 24 char 成为 mask char

            return (name_text ?? "")
                + (indicator_text ?? "")
                + (content_text ?? "");

            // parameters:
            //      start_char  text 文本处在 Region 中的起始偏移位置
            string GetMaskString(int start_offs, char first_char, int length)
            {
                if (length <= 0)
                    return "";
                if (start_offs < 0)
                    start_offs = 0;
                StringBuilder result = new StringBuilder();
                for (int i = start_offs; i < start_offs + length; i++)
                {
                    result.Append((char)(i + (int)first_char));
                }
                return result.ToString();
            }

            string GetHeaderMaskString(int length)
            {
                return new string((char)0x06, length);
            }

#if REMOVED
            string GetMaskString(string text)
            {
                if (text == null)
                    return null;
                return new string((char)0x01, text?.Length ?? 0);
            }
#endif
        }

        public string MergeFullTextMask(int start = 0, int end = int.MaxValue)
        {
            if (end <= start || end <= 0)
                return "";

            var name_text = _name?.MergeText(start, end);
            if (_name != null)
            {
                start -= _name.TextLength;
                if (end != -1)
                    end -= _name.TextLength;
            }
            // 按照字符所在的位置，mask char 值有所不同
            name_text = GetMaskString(start, (char)0x01, name_text?.Length ?? 0);

            var indicator_text = _indicator?.MergeText(start, end);
            if (_indicator != null)
            {
                start -= _indicator.TextLength;
                if (end != -1)
                    end -= _indicator.TextLength;
            }
            indicator_text = GetMaskString(start, (char)0x04, indicator_text?.Length ?? 0);

            var content_text = _content?.MergeText(start, end);
            if (this.IsHeader)
                content_text = GetHeaderMaskString(content_text?.Length ?? 0); // TODO: 可以改进为最多转换 24 char 成为 mask char

            start -= content_text?.Length ?? 0;
            end -= content_text?.Length ?? 0;

            var terminator = "";
            if (Utility.InRange(0, start, end))
                terminator = new string(Metrics.FieldEndCharDefault, 1);
            return (name_text ?? "")
                + (indicator_text ?? "")
                + (content_text ?? "")
                + (this.IsHeader ? "" : terminator);

            // parameters:
            //      start_char  text 文本处在 Region 中的起始偏移位置
            string GetMaskString(int start_offs, char first_char, int length)
            {
                if (length <= 0)
                    return "";
                if (start_offs < 0)
                    start_offs = 0;
                StringBuilder result = new StringBuilder();
                for (int i = start_offs; i < start_offs + length; i++)
                {
                    result.Append((char)(i + (int)first_char));
                }
                return result.ToString();
            }

            string GetHeaderMaskString(int length)
            {
                return new string((char)0x06, length);
            }

#if REMOVED
            string GetMaskString(string text)
            {
                if (text == null)
                    return null;
                return new string((char)0x01, text?.Length ?? 0);
            }
#endif
        }


        // 注: 当 Name 或 Indicator 内容为空时，要有停留在它们上面进行键盘输入的机会，不能一律跳过
        // return:
        //      -1  越过左边
        //      0   成功
        //      1   越过右边
        public int MoveByOffs(int offs,
            int direction,
            out HitInfo info)
        {
            var offs_original = offs;
            if (offs + direction < 0)
            {
                info = new HitInfo();
                return -1;
            }

            var infos = new List<HitInfo>();

            // TODO: 可以考虑当 Header 时 令 _name _indicator 为 null
            if (_name != null
                && IsHeader == false && _name != null
                && offs + direction >= 0 && offs + direction <= _name.TextLength)
            {
                // 如果(向左移动) caret 到了 _name 的末尾，需要调整到下一个区域的等同位置
                // 注: 向右情况不用调整。因为匹配顺序是先处理 _name
                // 注: 向左的意思是 caret 尽量停留在右侧后侧等同位置。向右的意思是 caret 尽量停留在左侧或者前侧等同位置。也就是比较滞留的特性
                if (direction < 0
                    && (_indicator != null || _content != null)
                    && offs + direction == _name.TextLength
                    && _name.TextLength >= 3)
                    goto DO_INDICATOR;
                var rect = GetNameRect();
                var ret = _name.MoveByOffs(offs, direction, out info);
                info.X += rect.X;
                info.Y += rect.Y;
                info.ChildIndex = 0; // 0 表示 _name
                info.TextIndex = info.Offs;
                //info.Offs += 0;
                info.Offs += offs_original - offs; // 保持原来的偏移量
                info.LineHeight = FirstLineCaretHeight(_name);
                if (_name.TextLength < 3)
                    return ret;
                infos.Add(info);
            }

        DO_INDICATOR:
            offs -= (NameTextLength);
            if (_indicator != null
                && IsHeader == false && _indicator != null
                && offs + direction >= 0 && offs + direction <= _indicator.TextLength)
            {
                var rect = GetIndicatorRect();
                var ret = _indicator.MoveByOffs(offs, direction, out info);
                info.X += rect.X;
                info.Y += rect.Y;
                info.ChildIndex = 1; // 1 表示 _indicator
                info.TextIndex = info.Offs;
                //info.Offs += _name.TextLength;
                info.Offs += offs_original - offs; // 保持原来的偏移量
                info.LineHeight = FirstLineCaretHeight(_indicator);
                if (_indicator.TextLength < 2 && this.IsControlField == false)
                    return ret;
                infos.Add(info);
            }
            else
            {
                if (infos.Count > 0
                    && this.IsControlField == false/*控制字段要延迟返回，等 _content 判断了再说*/)
                {
                    info = infos[infos.Count - 1];
                    return 0;
                }
            }

            offs -= (IndicatorTextLength);
            if (_content != null
                && offs + direction >= 0 && offs + direction <= _content.TextLength)
            {
                var x0 = GetContentX();
                var y0 = GetContentY();
                var ret = _content.MoveByOffs(offs, direction, out info);
                info.X += x0;
                info.Y += y0;
                info.ChildIndex = 2; // 2 表示 _content
                info.TextIndex = info.Offs;
                //info.Offs += _name.TextLength + _indicator.TextLength;
                info.Offs += offs_original - offs; // 保持原来的偏移量
                                                   // 保持 info.LineHeight

                // return ret;
                infos.Add(info);
            }
            else
            {
                if (infos.Count > 0)
                {
                    info = infos[infos.Count - 1];
                    return 0;
                }
            }

            if (infos.Count > 0)
            {
                info = infos[infos.Count - 1];
                return 0;
            }

            info = new HitInfo();
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

            if (virtual_tail_length < 0
                || virtual_tail_length > 1)
                throw new ArgumentException($"virtual_tail_length ({virtual_tail_length}) 必须为 0 或 1");

            if (start_offs == end_offs)
                return null;
            if (end_offs <= 0)
                return null;
            // TODO: 改为使用 .FullTextLength
            if (start_offs >= this.PureTextLength + virtual_tail_length)
                return null;

            var array = new List<Point>();
            var boxes = new List<IBox>();
            GetBoxes();
            var tail_box = GetRightMost(); // boxes.LastOrDefault();
            Region region = null;
            int current_offs = 0;
            int i = 0;
            foreach (var box in boxes)
            {
                var start = start_offs - current_offs;
                var end = end_offs - current_offs;
                var result = box.GetRegion(
                    start,
                    end,
                    (box == tail_box ? virtual_tail_length : 0)
                    );
                if (result != null)
                {
                    var p = array[i];
                    result.Offset(p.X, p.Y);
                    if (region == null)
                        region = result;
                    else
                    {
                        region.Union(result);
                        result.Dispose();
                    }
                }
                current_offs += box.TextLength;
                i++;
            }

            return region;

            void GetBoxes()
            {
                if (_name != null)
                {
                    boxes.Add(_name);
                    var rect = GetNameRect();
                    array.Add(new Point(rect.X, rect.Y));
                }
                if (_indicator != null)
                {
                    boxes.Add(_indicator);
                    var rect = GetIndicatorRect();
                    array.Add(new Point(rect.X, rect.Y));
                }
                if (_content != null)
                {
                    boxes.Add(_content);
                    array.Add(new Point(GetContentX(), GetContentY()));
                }
            }
        }

        IBox GetRightMost()
        {
            IBox right_most = _name;
            if (_indicator != null && _indicator.TextLength > 0)
                right_most = _indicator;
            if (_content != null /*&& _content.TextLength > 0*/)
                right_most = _content;
            return right_most;
        }

        #region 绘制字段名区域背景

        public static void PaintBack(SafeHDC hdc,
    Rectangle rect,
    Rectangle clipRect,
    Color color)
        {
            if (rect.IntersectsWith(clipRect))
            {
                DrawSolidRectangle(hdc,
    rect.Left,
    rect.Top,
    rect.Right,
    rect.Bottom,
    new COLORREF(color));
            }
        }

        // 绘制输入框的边框和背景
        public static void PaintEdit(SafeHDC hdc,
Rectangle rect,
Rectangle clipRect,
Color back_color,
int border_thickness,
Color border_color)
        {
            if (rect.IntersectsWith(clipRect))
            {
                DrawRectangle(hdc,
    rect.Left,
    rect.Top,
    rect.Right,
    rect.Bottom,
    (COLORREF)back_color,
    border_thickness,
    (COLORREF)border_color);
            }
        }

        public static void DrawRectangle(SafeHDC hdc,
int left,
int top,
int right,
int bottom,
COLORREF back_color,
int border_thickness,
COLORREF border_color)
        {
            // 创建实心画刷
            var hBrush = Gdi32.CreateSolidBrush(back_color);
            var oldBrush = Gdi32.SelectObject(hdc, hBrush);

            var hPen = Gdi32.CreatePen((int)Gdi32.PenStyle.PS_SOLID,
                border_thickness, border_color);
            var hOldPen = Gdi32.SelectObject(hdc, hPen);

            try
            {
                Gdi32.Rectangle(hdc,
                    left,
                    top,
                    right,
                    bottom);
            }
            finally
            {
                // 恢复原画刷并释放资源
                Gdi32.SelectObject(hdc, oldBrush);
                Gdi32.SelectObject(hdc, hOldPen);
                Gdi32.DeleteObject(hBrush);
                Gdi32.DeleteObject(hPen);
            }
        }

        public static void DrawSolidRectangle(SafeHDC hdc,
    int left,
    int top,
    int right,
    int bottom,
    COLORREF color)
        {
            // 创建实心画刷
            var hBrush = Gdi32.CreateSolidBrush(color);
            var oldBrush = Gdi32.SelectObject(hdc, hBrush);

            var hPen = Gdi32.CreatePen((int)Gdi32.PenStyle.PS_NULL, 1, color);
            var hOldPen = Gdi32.SelectObject(hdc, hPen);

            // 绘制实心矩形
            Gdi32.Rectangle(hdc,
                left,
                top,
                right + 1,
                bottom + 1);    // +1 的原因是想要让上下相邻的 rect 看起来连续。但要注意 GetRegion() 接口中得到的区域要向右下也扩大一个像素，避免刷新时漏掉一些线

            // 恢复原画刷并释放资源
            Gdi32.SelectObject(hdc, oldBrush);
            Gdi32.SelectObject(hdc, hOldPen);
            Gdi32.DeleteObject(hBrush);
            Gdi32.DeleteObject(hPen);
        }


        public void PaintBackAndBorder(SafeHDC hdc,
int x,
int y,
Rectangle clipRect)
        {
            // 调用前已经用可编辑背景色清除好了

            // 头标区，下部绘制全宽背景
            // 001 等控制字段，上部右侧，和下部绘制背景
            // 其余字段，下部绘制背景

            var sep = _fieldProperty.GapThickness;

            var name_rect = GetNameBorderRect(x, y);
            name_rect.Width -= sep;

            var solid_x = _fieldProperty.SolidX + _fieldProperty.BorderThickness;

            var indicator_rect = GetIndicatorBorderRect(x, y);
            indicator_rect.Width -= sep;

            //var first_line_height = Math.Max(name_rect.Bottom - y, indicator_rect.Bottom - y);
            //var first_line_width = _fieldProperty.SolidPixelWidth - _fieldProperty.BorderThickness * 2;

            // 控制字段
            if (this.IsControlField)
            {
                PaintRegion(name_rect);
            }
            else if (this.IsHeader == true)
            {
                // 头标区
                return;
            }
            else if (this.IsHeader == false)
            {
                PaintRegion(name_rect);
                PaintRegion(indicator_rect);
            }


            void PaintRegion(Rectangle rect0)
            {
                PaintWindow(rect0);
                /*
                PaintBorder(hdc,
                    rect0.X,
                    rect0.Y,
                    rect0.Width,
                    rect0.Height,
                    _fieldProperty.BorderThickness);
                */
            }

            void PaintSolid(int x0, int y0, int width0, int height0)
            {
                var rect = new Rectangle(x0, y0, width0, height0);
                PaintBack(hdc,
                    rect,
                    clipRect,
                    _fieldProperty?.SolidColor ?? SystemColors.Control);
                /*
                if (height0 > 0 && width0 > 0)
                {
                    using (var brush = new SolidBrush(_fieldProperty?.SolidColor ?? SystemColors.Control))
                    {
                        g.FillRectangle(brush, x0, y0, width0, height0);
                    }
                }
                */
            }

            void PaintWindow(Rectangle rect00)
            {
                bool isReadOnly = _fieldProperty.GetReadOnly?.Invoke(null) ?? false;
                // 如果控件为 Readonly 状态
                Color back_color;
                if (isReadOnly)
                    back_color = _fieldProperty?.ReadOnlyBackColor ?? SystemColors.Control;
                else
                    back_color = _fieldProperty?.BackColor ?? SystemColors.Window;
                    PaintEdit(hdc,
    rect00,
    clipRect,
    back_color,
    _fieldProperty.BorderThickness,
    _fieldProperty?.BorderColor ?? SystemColors.ControlDark);
                /*
                PaintBack(hdc,
                    rect00,
                    clipRect,
                    _fieldProperty?.BackColor ?? SystemColors.Window
                    );
                */
            }
        }

#if OLD
        // 以前版本
        public void PaintBackAndBorder(Graphics g,
int x,
int y)
        {
            // 调用前已经用可编辑背景色清除好了

            // 头标区，下部绘制全宽背景
            // 001 等控制字段，上部右侧，和下部绘制背景
            // 其余字段，下部绘制背景

            var sep = _fieldProperty.GapThickness;

            /*
            // 右侧全高的一根立体竖线
            {
                PaintRightBorder(g,
    x + _fieldProperty.ContentBorderX - _fieldProperty.BorderThickness,
    y + 0,
    this.GetPixelHeight(),
    _fieldProperty.BorderThickness);
            }
            */

            var name_rect = GetNameBorderRect(x, y);

            var solid_x = _fieldProperty.SolidX + _fieldProperty.BorderThickness;

            //name_rect.X -= _fieldProperty.BlankUnit;
            //name_rect.Width += _fieldProperty.BlankUnit;
            var indicator_rect = GetIndicatorBorderRect(x, y);
            var first_line_height = Math.Max(name_rect.Bottom - y, indicator_rect.Bottom - y);
            var first_line_width = _fieldProperty.SolidPixelWidth - _fieldProperty.BorderThickness * 2;

            // 控制字段
            if (this.IsControlField)
            {
                // 把 Indicator 区域填充为 solid 颜色
                int x0 = x + _fieldProperty.IndicatorBorderX;
                int width = _fieldProperty.IndicatorPixelWidth - _fieldProperty.BorderThickness;
                int height = first_line_height;
                using (var brush = new SolidBrush(_fieldProperty?.SolidColor ?? SystemColors.Control))
                {
                    g.FillRectangle(brush, x0, y, width, height);
                }

                // name 最上面很窄的一条 solid
                PaintSolid(solid_x,
                    y,
                    first_line_width,
                    name_rect.Y - y);

                PaintName();
            }
            else if (this.IsHeader == true)
            {
                // 头标区

                // 整个 solid 部分，包括下部
                int x0 = _fieldProperty.NameBorderX;    // ??
                PaintSolid(solid_x,
                    y,
                    first_line_width,
                    this.GetPixelHeight());
                return;
            }
            else if (this.IsHeader == false)
            {
                // 普通字段

                // 最上面很窄的一条 solid
                PaintSolid(solid_x,
                    y,
                    first_line_width,
                    name_rect.Y - y);

                // 左边的 border + sep 已经被 OnPaint() 画了


                PaintName();
                PaintIndicator();
            }

            // 下部 SolidColor
            {
                int height = this.GetPixelHeight() - first_line_height;
                PaintSolid(solid_x,
                    y + first_line_height,
                    first_line_width,
                    height);
            }

            // 左侧 solid 开始 border + sep
            {
                PaintSolid(solid_x,
    y,
    _fieldProperty.GapThickness,
    this.GetPixelHeight());
            }

            void PaintName()
            {
                /*
                int x0 = x + _fieldProperty.NameBorderX;
                int width = _fieldProperty.NamePixelWidth;
                int height = Line.GetLineHeight();
                PaintBorder(g, x0, y, width, height);
                */
                name_rect.Width -= sep;
                PaintSolid(name_rect.Right,
                    name_rect.Y,
                    sep,
                    name_rect.Height);
                PaintBorder(g,
                    name_rect.X,
                    name_rect.Y,
                    name_rect.Width,
                    name_rect.Height,
                    _fieldProperty.BorderThickness);
            }

            void PaintIndicator()
            {
                /*
                int x0 = x + _fieldProperty.IndicatorBorderX;
                int width = _fieldProperty.IndicatorPixelWidth;
                int height = Line.GetLineHeight();
                PaintBorder(g, x0, y, width, height);
                */

                indicator_rect.Width -= _fieldProperty.BorderThickness + sep;
                PaintSolid(indicator_rect.Right,
    indicator_rect.Y,
    sep,
    indicator_rect.Height);
                PaintBorder(g,
                    indicator_rect.X,
                    indicator_rect.Y,
                    indicator_rect.Width,
                    indicator_rect.Height,
                    _fieldProperty.BorderThickness);
            }

            void PaintSolid(int x0, int y0, int width0, int height0)
            {
                if (height0 > 0 && width0 > 0)
                {
                    using (var brush = new SolidBrush(_fieldProperty?.SolidColor ?? SystemColors.Control))
                    {
                        g.FillRectangle(brush, x0, y0, width0, height0);
                    }
                }
            }
        }

#endif


#if OLD
        // 以前的版本
        static void PaintBorder(Graphics g,
    int x,
    int y,
    int width,
    int height,
    int thickness)
        {
            // 四边边框
            {
                Rectangle temp_rect = new Rectangle(x, y, width, height);
                ControlPaint.DrawBorder(g,
                    temp_rect,
                    SystemColors.Control, thickness, System.Windows.Forms.ButtonBorderStyle.Inset,
                    SystemColors.Control, thickness, System.Windows.Forms.ButtonBorderStyle.Inset,
                    SystemColors.Control, thickness, System.Windows.Forms.ButtonBorderStyle.Inset,
                    SystemColors.Control, thickness, System.Windows.Forms.ButtonBorderStyle.Inset);
            }
        }
#endif
        // 以前的版本
        static void PaintBorder(SafeHDC hdc,
    int x,
    int y,
    int width,
    int height,
    int thickness)
        {
            // 四边边框
            {
                Rectangle temp_rect = new Rectangle(x, y, width, height);
                DrawBorder(hdc,
                    temp_rect,
                    SystemColors.ButtonShadow, thickness,
                    SystemColors.ButtonShadow, thickness,
                    SystemColors.ButtonHighlight, thickness,
                    SystemColors.ButtonHighlight, thickness);
            }
        }

        public static void DrawBorder(SafeHDC hdc,
            Rectangle bounds,
            Color leftColor,
            int leftWidth,
            Color topColor,
            int topWidth,
            Color rightColor,
            int rightWidth,
            Color bottomColor,
            int bottomWidth)
        {
            // 计算每一侧对应的填充矩形（使用文档坐标）
            // 注意：使用 Rectangle API 时右、下参数为坐标而不是宽高的结束值

            // top
            if (topWidth > 0)
            {
                int tx = bounds.X;
                int ty = bounds.Y;
                int tw = bounds.Width;
                int th = topWidth;
                DrawSolidRectangle(hdc, tx, ty, tx + tw, ty + th, topColor);
            }

            // bottom
            if (bottomWidth > 0)
            {
                int bx = bounds.X;
                int by = bounds.Y + bounds.Height - bottomWidth;
                int bw = bounds.Width;
                int bh = bottomWidth;

                DrawSolidRectangle(hdc, bx, by, bx + bw, by + bh, bottomColor);
            }

            // left
            if (leftWidth > 0)
            {
                int lx = bounds.X;
                int ly = bounds.Y + topWidth;
                int lw = leftWidth;
                int lh = Math.Max(0, bounds.Height - topWidth - bottomWidth);
                if (lh > 0)
                {
                    DrawSolidRectangle(hdc, lx, ly, lx + lw, ly + lh, leftColor);
                }
            }

            // right
            if (rightWidth > 0)
            {
                int rx = bounds.X + bounds.Width - rightWidth;
                int ry = bounds.Y + topWidth;
                int rw = rightWidth;
                int rh = Math.Max(0, bounds.Height - topWidth - bottomWidth);
                if (rh > 0)
                {
                    DrawSolidRectangle(hdc, rx, ry, rx + rw, ry + rh, rightColor);
                }
            }

        }


        public static void PaintLeftRightBorder(Graphics g,
int x,
int y,
int width,
int height,
int thickness)
        {
            // 四边边框
            {
                Rectangle temp_rect = new Rectangle(x, y, width, height);
                System.Windows.Forms.ControlPaint.DrawBorder(g,
                    temp_rect,
                    SystemColors.Control, thickness, System.Windows.Forms.ButtonBorderStyle.Outset,
                    SystemColors.Control, 0, System.Windows.Forms.ButtonBorderStyle.None,
                    SystemColors.Control, thickness, System.Windows.Forms.ButtonBorderStyle.Outset,
                    SystemColors.Control, 0, System.Windows.Forms.ButtonBorderStyle.None);
            }
        }


        void PaintBack(SafeHDC hdc,
            int x,
            int y,
            Rectangle clipRect,
            IBox box,
            Color color)
        {
            if (box == null)
                return;
            PRECT rect_back = new PRECT(x, y,
                x + box.GetPixelWidth(),
                y + box.GetPixelHeight());
            Line.DrawSolidRectangle(hdc,
rect_back.left,
rect_back.top,
rect_back.right,
rect_back.bottom,
new COLORREF(color),
clipRect);
        }

        #endregion

        // 获得 Name 外围边框区域的 Rectangle
        Rectangle GetNameBorderRect(int x = 0, int y = 0)
        {
            return new Rectangle(x + _fieldProperty.NameBorderX,
                (int)(y + _baseLine - (_name?.BaseLine ?? 0)) - VerticalUnit() / 2,
                _fieldProperty.NamePixelWidth,
                VerticalUnit() + (_name?.GetPixelHeight() ?? 0));
        }


        // 获得 Name 可编辑区域的 Rectangle
        Rectangle GetNameRect(int x = 0, int y = 0)
        {
            return new Rectangle(x + _fieldProperty.NameX,
                (int)(y + _baseLine - (_name?.BaseLine ?? 0)) + 0,
                _fieldProperty.NamePixelWidth,
                _name?.GetPixelHeight() ?? 0);
        }

        int VerticalUnit()
        {
            return _fieldProperty.BlankUnit / 2;
        }

        int GetNameX()
        {
            return _fieldProperty.NameX;
        }

        Rectangle GetIndicatorRect(int x = 0, int y = 0)
        {
            // _indicator 中暂无内容
            if (_indicator == null || _indicator.BaseLine == 0)
            {
                // 借用 _name 的一些参数。
                return new Rectangle(x + _fieldProperty.IndicatorX,
    (int)(y + _baseLine - (_name?.BaseLine ?? 0)) + 0,
    _fieldProperty.IndicatorPixelWidth,
    _name?.GetPixelHeight() ?? 0);

            }
            return new Rectangle(x + _fieldProperty.IndicatorX,
                (int)(y + _baseLine - (_indicator?.BaseLine ?? 0)) + 0,
                _fieldProperty.IndicatorPixelWidth,
                _indicator?.GetPixelHeight() ?? 0);
        }

        Rectangle GetIndicatorBorderRect(int x = 0, int y = 0)
        {
            // _indicator 中暂无内容
            if (_indicator == null || _indicator.BaseLine == 0)
            {
                // 借用 _name 的一些参数。
                return new Rectangle(x + _fieldProperty.IndicatorBorderX,
                    (int)(y + _baseLine - (_name?.BaseLine ?? 0)) - VerticalUnit() / 2,
                    _fieldProperty.IndicatorPixelWidth,
                    VerticalUnit() + _name?.GetPixelHeight() ?? 0);
            }
            return new Rectangle(x + _fieldProperty.IndicatorBorderX,
                (int)(y + _baseLine - (_indicator?.BaseLine ?? 0)) - VerticalUnit() / 2,
                _fieldProperty.IndicatorPixelWidth,
                VerticalUnit() + _indicator?.GetPixelHeight() ?? 0);
        }

        int GetIndicatorX()
        {
            return _fieldProperty.IndicatorX;
        }

        int GetContentX(int x0 = 0)
        {
            return x0 + _fieldProperty.ContentX;
        }

        int GetContentY(int y0 = 0)
        {
            // _indicator 中暂无内容
            if (_content == null || _content.BaseLine == 0)
            {
                if (IsHeader)
                    return 0;
                // 借用 _name 的一些参数。
                return (int)(y0 + _baseLine - (_name?.BaseLine ?? 0)) + 0;
            }
            return y0 + (int)(_baseLine - this._content.BaseLine);
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
            // 在 _name _indicator _content 中最后一个使用 virtual_tail_length
            IBox right_most = GetRightMost();

            // 绘制定制背景
            if (context.PaintBack != null)
            {
                var width = _fieldProperty.ContentX + _content?.GetPixelWidth() ?? 0;
                var height = Math.Max(
                    _name?.GetPixelHeight() ?? 0,
                    _content?.GetPixelHeight() ?? 0
                    );
                var field_rect = new Rectangle(x, y, width, height);
                context.PaintBack?.Invoke(this, dc, field_rect, clipRect);
            }

            Rectangle rect;
            int x0 = x;
            x = x0 + _fieldProperty.CaptionX;
            if (_caption != null)
            {
                // Debug.WriteLine($"_caption y={y}");
                rect = new Rectangle(x,
                    y + (this.IsHeader ? GetContentY() : GetNameRect().Y),
                    Math.Max(0, _fieldProperty.CaptionPixelWidth - _fieldProperty.GapThickness),
                    _caption?.GetPixelHeight() ?? 0);

                if (rect.Width > 0
                    && clipRect.IntersectsWith(rect))
                {
                    //context.PaintBack?.Invoke(_caption, dc, rect, clipRect);
                    /*
                    PaintBack(dc,
                        x,
                        y,
                        _caption,
                        Color.LightPink);
                    */
                    // TODO: 根据最大宽度剪裁
                    var getforecolor = context.GetForeColor;
                    context.GetForeColor = (box, highlight) =>
                    {
                        return _fieldProperty.CaptionForeColor;
                    };
                    try
                    {
                        // TODO: 实现文字接近边沿逐渐淡色的效果
                        _caption.Paint(
                            context,
                            dc,
                            rect.X,
                            rect.Y,
                            System.Drawing.Rectangle.Intersect(rect, clipRect),   // clipRect,
                            0,  //blockOffs1,
                            0,  //blockOffs2,
                            0);
                    }
                    finally
                    {
                        context.GetForeColor = getforecolor;
                    }
                }
            }

            x = x0 + _fieldProperty.NameX;
            // rect = new Rectangle(x, y, _fieldProperty.NamePixelWidth, Line.GetLineHeight());
            rect = GetNameRect(x0, y);

            if (_name != null)
            {
                //context.PaintBack?.Invoke(_name, dc, rect, clipRect););
                /*
                PaintBack(dc,
                    x,
                    y,
                    clipRect,
                    _name,
                    Color.LightPink);
                */
                // TODO: Intersect() 判断的矩形可以是包括边框的更大矩形。这样一些溢出的情况也能得以刷新显示
                if (clipRect.IntersectsWith(rect))
                    _name.Paint(
                        context,
                        dc,
                        rect.X,
                        rect.Y,
                        clipRect,
                        blockOffs1,
                        blockOffs2,
                        right_most == _name ? virtual_tail_length : 0);
                var delta = _name.TextLength;
                blockOffs1 -= delta;
                blockOffs2 -= delta;
            }

            x += _fieldProperty.NamePixelWidth;
            Debug.Assert(x - x0 == _fieldProperty.IndicatorX);
            // rect = new Rectangle(x, y, _fieldProperty.IndicatorPixelWidth, Line.GetLineHeight());
            rect = GetIndicatorRect(x0, y);

            if (_indicator != null)
            {
                //context.PaintBack?.Invoke(_indicator, dc, rect, clipRect););
                /*
                PaintBack(dc,
                    x,
                    y,
                    clipRect,
                    _indicator,
                    Color.Green);
                */
                if (clipRect.IntersectsWith(rect))
                    _indicator.Paint(
                        context,
                        dc,
                        rect.X,
                        rect.Y,
                        clipRect,
                        blockOffs1,
                        blockOffs2,
                        right_most == _indicator ? virtual_tail_length : 0);

                var delta = _indicator.TextLength;
                blockOffs1 -= delta;
                blockOffs2 -= delta;
            }

            x += _fieldProperty.IndicatorPixelWidth;
            Debug.Assert(x - x0 == _fieldProperty.ContentX);
            //rect = new Rectangle(x, y, _content.GetPixelWidth(), _content.GetPixelHeight());

            if (_content != null/*
                && clipRect.IntersectsWith(rect)*/)
            {
                //context.PaintBack(_content, rect, dc);

                _content.Paint(
                    context,
                    dc,
                    GetContentX(x0),
                    y + GetContentY(),
                    clipRect,
                    blockOffs1,
                    blockOffs2,
                    right_most == _content ? virtual_tail_length : 0);
            }
        }

        // 刷新(更新)字段 Caption 文字
        // parameters:
        //      update_rect [out] 返回需要更新的矩形区域。坐标是本对象文档坐标
        public void RefreshCaptionText(
            IContext context,
            SafeHDC dc,
            out Rectangle update_rect)
        {
            EnsureCaption();
            /*
            var caption = field_name;
            if (string.IsNullOrEmpty(caption) == false)
                caption += " ";
            */
            var caption = _fieldProperty.GetFieldCaption?.Invoke(this);
            var ret = _caption.ReplaceText(
                context,
                dc,
                0,
                -1,
                caption,
                int.MaxValue   // _fieldProperty.CaptionPixelWidth,
                /*,
                out string replaced,
                out update_rect,
                out Rectangle scroll_rect,
                out int scroll_distance*/);
            update_rect = ret.UpdateRect;
            if (update_rect != System.Drawing.Rectangle.Empty)
                update_rect.Offset(_fieldProperty.CaptionX, 0);
        }

        // TODO: 如果替换过程中实际上增加了最后一个字段的结束符，那要
        // 考虑给 replaced 最后减少一个结束符，以免 Undo 的时候发生不一致
        // return:
        //      0   未给出本次修改的像素宽度。需要调主另行计算
        //      其它  本次修改后的像素宽度
        public ReplaceTextResult ReplaceText(
            IContext context,
            SafeHDC dc,
            int start,
            int end,
            string content,
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
            */
            var result = new ReplaceTextResult();

            this.ClearCacheHeight();

            // TODO: 这里要改造为使用 MergeFullText()。本函数内后面的代码相关的也要改
            var text = this.MergePureText();
            if (end == -1)
                end = text.Length;
            result.ReplacedText = text.Substring(start, end - start);
            text = text.Substring(0, start) + content + text.Substring(end);
            var name_value = "";
            var indicator_value = "";
            var content_value = "";
            int max_pixel_width = 0;

            if (this.IsHeader == false)
            {
                name_value = text.Substring(0, Math.Min(3, text.Length));
                text = text.Substring(name_value.Length);

                if (text.Length > 0)
                {
                    if (IsControlFieldName(name_value) == false)
                    {
                        indicator_value = text.Substring(0, Math.Min(2, text.Length));
                        text = text.Substring(indicator_value.Length);
                    }
                    if (text.Length > 0)
                        content_value = text;
                }
            }
            else
            {
                content_value = text;
            }

            var old_name_value = this.FieldName;

            if (true
                //string.IsNullOrEmpty(name_value) == false
                )
            {
                //var func = context.GetFont;
                //context.GetFont = (p, o) => {
                //    return func?.Invoke(_name, o);
                //};
                try
                {
                    EnsureName();
                    // var width =
                    var ret = _name.ReplaceText(
                        context,
                        dc,
                        0,
                        -1,
                        name_value,
                        _fieldProperty.NameTextWidth);

                    var r = GetNameRect();
                    var update_rect_name = Utility.Offset(ret.UpdateRect, r.X, r.Y);

                    update_rect = Utility.Union(update_rect, update_rect_name);
                    max_pixel_width = Math.Max(max_pixel_width,
                        _fieldProperty.NameX + ret.MaxPixel);
                }
                finally
                {
                    //context.GetFont = func;
                }
            }
            else
            {
                // 至少要有一个 _name
                if (this.IsHeader == false)
                {
                    EnsureName();
                    _name.Clear();
                    // TODO: update rect
                }
            }

            // 如果 Name 内容发生变化，要连带触发变化 Caption 的内容
            if (_fieldProperty?.GetFieldCaption != null
                && name_value != old_name_value)
            {
                //var func = context.GetFont;
                //context.GetFont = (p, o) => {
                //    return func?.Invoke(_caption, o);
                //};
                try
                {
                    RefreshCaptionText(
                    context,
                    dc,
                    out Rectangle update_rect_caption); // 返回前 update_rect_caption 已经被平移过了
                    update_rect_caption.Width = Math.Min(update_rect_caption.Width, _fieldProperty.CaptionPixelWidth);
                    update_rect = Utility.Union(update_rect, update_rect_caption);
                    max_pixel_width = Math.Max(max_pixel_width,
                        _fieldProperty.NameBorderX);
                }
                finally
                {
                    //context.GetFont = func;
                }
            }

            if (true
                // string.IsNullOrEmpty(indicator_value) == false
                )
            {
                EnsureIndicator();

                //var func = context.GetFont;
                //context.GetFont = (p, o) => {
                //    return func?.Invoke(_indicator, o);
                //};
                try
                {
                    var ret = _indicator.ReplaceText(
                    context,
                    dc,
        0,
        -1,
        indicator_value,
        _fieldProperty.IndicatorTextWidth);

                    var r = GetIndicatorRect();
                    var update_rect_indicator = Utility.Offset(
                        ret.UpdateRect,
                        r.X,
                        r.Y);
                    update_rect = Utility.Union(update_rect, update_rect_indicator);

                    max_pixel_width = Math.Max(max_pixel_width,
                        _fieldProperty.IndicatorX + ret.MaxPixel);
                }
                finally
                {
                    //context.GetFont = func;
                }
            }
            else
            {
                if (this.IsHeader == false
                    && this.IsControlField == false)
                {
                    EnsureIndicator();
                    _indicator.Clear();
                }
            }

            if (true
                // string.IsNullOrEmpty(content_value) == false
                )
            {
                EnsureContent();

                if (content_value != _content.MergeText())
                {
                    //var func = context.GetFont;
                    //context.GetFont = (p, o) => {
                    //    return func?.Invoke(_content, o);
                    //};
                    try
                    {
                        var ret = _content.ReplaceText(
                        context,
                        dc,
        0,
        -1,
        content_value,
        pixel_width == -1 ? -1 : Math.Max(pixel_width - (_fieldProperty.ContentX), _fieldProperty.MinFieldContentWidth/* 最小不小于 5 char 宽度*/)
        /*,
        out replaced,
        out Rectangle update_rect_content,
        out scroll_rect,
        out scroll_distance*/);
                        var y0 = GetContentY();
                        var update_rect_content = Utility.Offset(
                            ret.UpdateRect,
                            GetContentX(),
                            y0);
                        /*
                        // 注: 右侧加上虚拟回车符号的宽度。避免失效区域不足
                        if (update_rect_content.IsEmpty == false)
                            update_rect_content.Width += Line.ReturnWidth();
                        if (scroll_rect.IsEmpty == false)
                            scroll_rect.Width += Line.ReturnWidth();
                        */

                        /*
                        // 若 Paragraph 中 Line 数变化，就要连左边的矩形一起刷新和滚动
                        if (scroll_rect.IsEmpty == false)
                        {
                            update_rect.Width += update_rect.X;
                            update_rect.X = 0;
                        }
                        */
                        update_rect = Utility.Union(update_rect, update_rect_content);

                        max_pixel_width = Math.Max(max_pixel_width,
                            _fieldProperty.ContentX + ret.MaxPixel);
                    }
                    finally
                    {
                        //context.GetFont = func;
                    }
                }
                else
                {
                    // 搜集清除前的 update_rect
                    Rectangle update_rect_content = GetRect(_content);
                    Utility.Offset(ref update_rect_content,
                        _fieldProperty.ContentX,
                        0);
                    update_rect = Utility.Union(update_rect, update_rect_content);

                    ClearEmpty();
                    ProcessBaseline();
                    result.UpdateRect = update_rect;
                    result.MaxPixel = _fieldProperty.ContentX + (_content?.GetPixelWidth() ?? 0);
                    return result;
                }
            }
            else
            {
                EnsureContent();
                _content.Clear();
            }

            ClearEmpty();
            ProcessBaseline();
            result.UpdateRect = update_rect;
            result.MaxPixel = max_pixel_width;
            return result;

            void ClearEmpty()
            {
                // 空内容情况下，使 HitTest() 偏左
                if (string.IsNullOrEmpty(content_value))
                {
                    if (indicator_value.Length < 2
                        && this.IsControlField == false /*控制字段保留 _content，可避免向右移动到不了 content 的尴尬效果*/)
                        _content = null;
                    if (string.IsNullOrEmpty(indicator_value))
                        _indicator = null;
                }

                this.ClearCacheHeight();
            }
        }

        // 报错文字中所说的字符数是指不包括字段结束符的字符数
        // parameters:
        //      start   起点位置。以当前字段第一字符为 0
        //      length  需要增加的字符数。如果为负数，表示要在 start 位置向左删除这么多字符
        public delegate void delegate_fix(MarcField field,
            int start,
            int length);

        public IEnumerable<string> Verify(delegate_fix func_fix)
        {
            var errors = new List<string>();
            if (this.IsHeader)
            {
                if (this.TextLength != 24)
                {
                    int delta = 24 - this.TextLength;
                    func_fix?.Invoke(this, this.TextLength, delta);
                    errors.Add($"头标区字符数不正确，应为 24 字符(但现在是 {this.TextLength})");
                }
            }
            else if (this.IsControlField)
            {
                // 理论上执行不到这里。因为不足 3 字符是没法判定为控制字段的
                if (this.PureTextLength < 3)
                {
                    int delta = 3 - this.PureTextLength;
                    func_fix?.Invoke(this, this.PureTextLength, delta);
                    errors.Add($"{this.Name} 字段字符数不足，应为至少 3 字符(但现在是 {this.PureTextLength})");
                }
            }
            else
            {
                if (this.PureTextLength < 5)
                {
                    int delta = 5 - this.PureTextLength;
                    func_fix?.Invoke(this, this.PureTextLength, delta);
                    errors.Add($"{this.Name} 字段字符数不足，应为至少 5 字符(但现在是 {this.PureTextLength})");
                }
            }

            return errors;
        }

#if REMOVED
        // 补足字符
        // 一个问题是如何通知窗口更新
        public bool TryRightPad(
            IContext context,
            SafeHDC hdc,
            out Rectangle update_rect)
        {
            if (this.IsHeader)
            {
                if (this._content.TextLength < 24)
                {
                    this._content.ReplaceText(context,
                        hdc,)
                }

            }
        }
#endif

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

        List<IBox> GetBoxes()
        {
            var boxes = new List<IBox>();
            if (_name != null)
            {
                boxes.Add(_name);
            }
            if (_indicator != null)
            {
                boxes.Add(_indicator);
            }
            if (_content != null)
            {
                boxes.Add(_content);
            }
            return boxes;
        }


        // 综合三个区域的基线计算最大基线。还要考虑 Name 和 Indicator 的(上方、下方)竖向空白
        void ProcessBaseline()
        {
            var boxes = GetBoxes();
            if (boxes.Count == 0)
            {
                // TODO: 利用一个空格检测
                // _baseLine = Line.GetLineHeight();
                _baseLine = 0;
                _below = 0;
                return;
            }
            _baseLine = boxes.Max(x => x.BaseLine) + VerticalUnit() / 2;
            _below = boxes.Max(y => y.Below) + VerticalUnit() / 2;
        }

        int FirstLineCaretHeight(IBox box = null)
        {
            int ret = 0;
            if (box == null)
                ret = (int)(this.BaseLine + this.Below);
            else
                ret = box.GetPixelHeight();
            //if (ret < Line.GetLineHeight())
            //    return Line.GetLineHeight();
            return ret;
        }

        static Rectangle GetRect(IBox box)
        {
            if (box == null)
                return System.Drawing.Rectangle.Empty;
            var w = box.GetPixelWidth();
            var h = box.GetPixelHeight();
            return new Rectangle(0, 0, w, h);
        }

        public static bool IsControlFieldName(string strFieldName)
        {
            if (String.Compare(strFieldName, "hdr", true) == 0)
                return true;

            if (String.Compare(strFieldName, "###", true) == 0)
                return true;

            if (
                (
                String.Compare(strFieldName, "001") >= 0
                && String.Compare(strFieldName, "009") <= 0
                )

                || String.Compare(strFieldName, "-01") == 0
                || strFieldName == "FMT"    // 2024/3/8
                )
                return true;

            return false;
        }

        public static void ParseFieldParts(string text,
            bool isHeader,
            bool pad_right,
            out string name,
            out string indicator,
            out string content)
        {
            if (isHeader)
            {
                name = "";
                indicator = "";
                if (pad_right)
                    content = text.PadRight(24, ' ');
                else
                    content = text;
                return;
            }
            if (pad_right && text.Length < 3)
                text = text.PadRight(3, ' ');
            if (text.Length >= 3)
            {
                name = text.Substring(0, 3);
                text = text.Substring(3);
            }
            else
            {
                name = text;
                indicator = "";
                content = "";
                return;
            }

            if (IsControlFieldName(name) == false)
            {
                if (pad_right && text.Length < 2)
                    text = text.PadRight(2, ' ');

                if (text.Length >= 2)
                {
                    indicator = text.Substring(0, 2);
                    text = text.Substring(2);
                }
                else
                {
                    indicator = text;
                    content = "";
                    return;
                }
            }
            else
                indicator = "";

            content = text;
        }


        public int GetPixelWidth()
        {
            return _fieldProperty.ContentX + (_content?.GetPixelWidth() ?? 0);
        }

        public string FieldName
        {
            get
            {
                return this._name?.MergeText();
            }
        }

        public bool IsControlField
        {
            get
            {
                if (_name == null)
                    return false;
                return IsControlFieldName(this.FieldName);
            }
        }

        public delegate string GetFieldCaptionFunc(string fieldName);

        #region 外部 API 接口

        public string GetName()
        {
            return _name?.MergeText() ?? "";
        }

        public string GetIndicator()
        {
            return _indicator?.MergeText() ?? "";
        }

        public string GetContent()
        {
            return _content?.MergeText() ?? "";
        }

        // 一种方法是直接修改 _name 的内容，然后触发 MarcControl 更新指定范围的显示
        // 另外一种方法是把修改位置换算为 start~end offs，触发 MarcControl 的全局性 ReplaceText
        // 但第二种方法的缺点是，触发 ReplaceText 后，可能 MarcField 本身被重建了，原有对象不再有效
        public string ChangeName(string name)
        {
            if (IsHeader)
                throw new InvalidOperationException($"头标区不允许修改 name");

            if (string.IsNullOrEmpty(name) == true)
                throw new ArgumentException("name 值不允许为空");

            if (name.Length != 3)
                throw new ArgumentException($"name 值 '{name}' 不合法，字符数应为 3 字符");

            var old_name = _name.MergeText();

            // 获得起止 offs，并触发更新
            _record.GetFieldOffsRange(this,
                out int field_start,
                out _);
            this.GetNameOffsRange(out int name_start,
                out int name_end);
            GetControl().ReplaceText(field_start + name_start,
                field_start + name_end,
                name,
                false);
            return old_name;
        }

        // 修改字段的 Indicator 部分
        public string ChangeIndicator(string indicator)
        {
            if (IsHeader)
                throw new InvalidOperationException($"头标区不允许修改 indicator");

            if (IsControlField)
                throw new InvalidOperationException($"控制字段不允许修改 indicator");


            if (string.IsNullOrEmpty(indicator) == true)
                throw new ArgumentException("indicator 值不允许为空");

            if (indicator.Length != 2)
                throw new ArgumentException($"indicator 值 '{indicator}' 不合法，字符数应为 2 字符");

            var old_indicator = _indicator.MergeText();

            // 获得起止 offs，并触发更新
            _record.GetFieldOffsRange(this,
                out int field_start,
                out _);
            this.GetIndicatorOffsRange(out int indicator_start,
                out int indicator_end);
            GetControl().ReplaceText(field_start + indicator_start,
                field_start + indicator_end,
                indicator,
                false);
            return old_indicator;
        }

        // 修改字段的 Content 部分
        public string ChangeContent(string content)
        {
            if (content == null)
                throw new ArgumentException("content 参数值不允许为 null");

            if (this.IsHeader && content.Length != 24)
                throw new ArgumentException($"content 参数值字符数必须为 24 (但现在是 {content.Length})");

            var old_content = _content.MergeText();

            // 获得起止 offs，并触发更新
            _record.GetFieldOffsRange(this,
                out int field_start,
                out _);

            // 获得一个字段的内容 offs 范围。注意内容部分不包括结尾的字段结束符
            this.GetContentOffsRange(out int content_start,
                out int content_end);
            GetControl().ReplaceText(field_start + content_start,
                field_start + content_end,
                content,
                false);
            return old_content;
        }

        // 修改全部内容
        public string ChangeText(string text)
        {
            if (text == null)
                throw new ArgumentException("text 值不允许为 null");

            var old_content = _name.MergeText();

            // 获得起止 offs，并触发更新
            _record.GetFieldOffsRange(this,
                out int field_start,
                out int field_end);
            // TODO: 注意测试 text 尾部包含和不包含字段结束符会发生什么
            GetControl().ReplaceText(field_start,
                field_end,
                text,
                false);
            return old_content;
        }


        // 获得 Name 部分的起止 offs
        public bool GetNameOffsRange(out int start,
            out int end)
        {
            start = 0;
            end = start;
            if (_name == null || IsHeader)
                return false;

            end = start + _name.TextLength;
            return true;
        }

        public bool GetIndicatorOffsRange(out int start,
    out int end)
        {
            start = _name?.TextLength ?? 0;
            end = start;
            if (_indicator == null || IsControlField == true)
                return false;

            end = start + _indicator.TextLength;
            return true;
        }

        // 获得一个字段的内容 offs 范围。注意内容部分不包括结尾的字段结束符
        public bool GetContentOffsRange(
            out int start,
            out int end)
        {
            start = (_name?.TextLength ?? 0) + (_indicator?.TextLength ?? 0);
            if (_content == null)
            {
                end = start + (IsHeader ? 0 : 1);
                return false;
            }

            end = start + _content.TextLength;  // + (IsHeader ? 0 : 1);
            return true;
        }

        public MarcControl GetControl()
        {
            return _record?._marcControl;
        }

        #endregion

        public class Tag
        {
            // 子字段名
            public char Name { get; set; }
            public bool Delimeter { get; set; }
        }

        // 子字段符号和子字段名粘连在一起
        public static Segment[] SegmentSubfields(string text,
    char delimeter = '\\',
    int name_chars = 2)
        {
            if (text == null)
                return new Segment[0];

            Tag delimeter_tag;
            Tag content_tag;
            ClearDelimeterTag();
            ClearContentTag();
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
                            Tag = is_head_delimeter ? delimeter_tag : content_tag,
                        });
                    }

                    line = new StringBuilder();
                    line.Append(ch);
                    {
                        is_head_delimeter = true;
                        // 清除两个 tag 的 Name 内容
                        ClearDelimeterTag();
                        ClearContentTag();
                    }
                }
                else
                {
                    if (line == null)
                    {
                        line = new StringBuilder();
                        {
                            is_head_delimeter = false;
                            ClearDelimeterTag();
                            // content_tag.Name 继续保留
                        }
                    }
                    line.Append(ch);
                }

                // 因为(分隔符引导的一段内容)长度到了，主动切割
                if (is_head_delimeter)
                {
                    content_tag.Name = ch;
                    delimeter_tag.Name = ch;
                    if (line.Length >= name_chars)
                    {
                        lines.Add(new Segment
                        {
                            Text = line.ToString(),
                            Tag = is_head_delimeter ? delimeter_tag : content_tag,
                        });
                        line = null;
                        {
                            is_head_delimeter = false;
                            ClearDelimeterTag();
                            // content_tag.Name 继续保留
                        }
                    }
                }
            }

            if (line != null)
                lines.Add(new Segment
                {
                    Text = line.ToString(),
                    Tag = is_head_delimeter ? delimeter_tag : content_tag,
                });

            if (lines.Count == 0)
                lines.Add(new Segment
                {
                    Text = "",
                    Tag = new Tag { Delimeter = false },
                });
            return lines.ToArray();

            void ClearContentTag()
            {
                content_tag = new Tag
                {
                    Delimeter = false,
                };
            }

            void ClearDelimeterTag()
            {
                delimeter_tag = new Tag
                {
                    Delimeter = true,
                };
            }
        }

        // 子字段周边信息
        public class SubfieldBound
        {
            public string Name { get; set; }
            public int StartOffs { get; set; }

            public int ContentStartOffs { get; set; }
            public int EndOffs { get; set; }

            public int CaretOffs { get; set; }

            public bool Found { get; set; }

            public override string ToString()
            {
                return $"Name={Name}, StartOffs={StartOffs}, ContentStartOffs={ContentStartOffs}, EndOffs={EndOffs}, CaretOffs={CaretOffs}, Found={Found}";
            }
        }

        // parameters:
        //      right_most    当插入符处在内容末端时，是否认为命中最后一个子字段
        public SubfieldBound GetSubfieldBounds(
            int offs,
            bool right_most = false)
        {
            var subfld = Metrics.SubfieldCharDefault;

            if (this.IsHeader || this.IsControlField)
                return new SubfieldBound { Found = false };
            if (offs < 5)
                return new SubfieldBound { Found = false };
            var text_length = this.PureTextLength;
            if (offs > text_length)
            {
                return new SubfieldBound { Found = false };
            }

            // 只需要得到不包含结束符的文本
            var text = this.MergePureText();
            var start = -1;

            if (offs >= text_length)
            {
                if (right_most == false)
                    return new SubfieldBound { Found = false };
                if (text_length > 5)
                {
                    // 特殊处理插入符在内容末端的情况
                    start = GetStartOffs(text_length - 1);
                }
                else
                {
                    return new SubfieldBound { Found = false };
                }
            }

            var current_name = "";

            if (start == -1)
            {
                start = GetStartOffs(offs);
            }

            if (start < 5)
                return new SubfieldBound { Found = false };

            var end = GetEndOffs(offs);
            if (end > start + 1)
                current_name = text.Substring(start + 1, 1);

            var found = text[start] == subfld;
            if (found == false)
                return new SubfieldBound { Found = false };

            return new SubfieldBound
            {
                Name = current_name,
                StartOffs = start,
                ContentStartOffs = Math.Min(end, start + 2),
                EndOffs = end,
                CaretOffs = offs,
                Found = found,
            };

            int GetEndOffs(int c)
            {
                int j = c;
                foreach (var ch in text.Substring(c))
                {
                    if (ch == subfld && j > c)
                        break;
                    j++;
                }
                return j;
            }

            int GetStartOffs(int c)
            {
                for (; c >= 0; c--)
                {
                    var ch = text[c];
                    if (text[c] == subfld)
                        break;
                }
                return c;
            }
        }

        public void ClearCache()
        {
            _name?.ClearCache();
            _indicator?.ClearCache();
            _content?.ClearCache();
        }
    }
}
