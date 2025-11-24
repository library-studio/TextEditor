using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;

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
        // public ConvertTextFunc ConvertText { get; set; }

        // public Color BackColor { get; set; } = Color.Transparent;

        Line _caption;
        Line _name;
        Line _indicator;
        Paragraph _content;

        // 引用字段共同属性
        FieldProperty _fieldProperty;

        public bool IsHeader { get; set; }

        public MarcField(FieldProperty property/*, ConvertTextFunc func_convertText*/)
        {
            _fieldProperty = property;
            //_name = new Line();
            //_indicator = new Line();
            //_content = new Paragraph();

            // this.ConvertText = func_convertText;
        }

        public int TextLength => (NameTextLength)
            + (IndicatorTextLength)
            + (ContentTextLength);

        public bool CaretMoveDown(int x, int y, out HitInfo info)
        {
            if (x < _fieldProperty.NamePixelWidth)
                return _name.CaretMoveDown(x, y, out info);
            x -= _fieldProperty.NamePixelWidth;
            if (x < _fieldProperty.IndicatorPixelWidth)
                return _indicator.CaretMoveDown(x, y, out info);
            x -= _fieldProperty.IndicatorPixelWidth;
            return _content.CaretMoveDown(x, y, out info);
        }

        public bool CaretMoveUp(int x, int y, out HitInfo info)
        {
            if (x < _fieldProperty.NamePixelWidth)
                return _name.CaretMoveUp(x, y, out info);
            x -= _fieldProperty.NamePixelWidth;
            if (x < _fieldProperty.IndicatorPixelWidth)
                return _indicator.CaretMoveUp(x, y, out info);
            x -= _fieldProperty.IndicatorPixelWidth;
            return _content.CaretMoveUp(x, y, out info);
        }

        public void Clear()
        {
            _caption?.Clear();
            _name?.Clear();
            _indicator?.Clear();
            _content?.Clear();
        }

        // TODO: 将部件的高度得到以后，取最大值
        public int GetPixelHeight()
        {
            if (_content != null)
                return _content.GetPixelHeight();
            if (_name != null)
                return _name.GetPixelHeight();
            if (_indicator != null)
                return _indicator.GetPixelHeight();
            return Line.GetLineHeight();
        }

        // 注意对于 _name _indicator _content 为 null 的时候，相应区域不应该允许点击，而要转移到最近相邻的一个区域返回点击信息
        public HitInfo HitTest(int x, int y)
        {
#if REMOVED
            // 点击到了左边 Caption 区域
            if (x < _fieldProperty.CaptionPixelWidth - SplitterPixelWidth)
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

            var start = _fieldProperty.CaptionPixelWidth;
            // must 表示“其它 box 为空，必须用当前这个来 hittest”
            var must = _indicator == null && _content == null;
            if (IsHeader == false && _name != null
                && (x < start + _fieldProperty.NamePixelWidth || must))
            {
                var info = _name.HitTest(x - start, y);
                info.X += start;
                info.ChildIndex = 0; // 0 表示 _name
                info.TextIndex = info.Offs;
                info.Offs += 0; // 保持原来的偏移量
                return info;
            }
            start += _fieldProperty.NamePixelWidth;
            must = _content == null;
            if (IsHeader == false && _indicator != null && this.IsControlField == false
                && (x < start + _fieldProperty.IndicatorPixelWidth || must))
            {
                var info = _indicator.HitTest(x - start, y);
                info.X += start;
                info.ChildIndex = 1; // 1 表示 _indicator
                info.TextIndex = info.Offs;
                info.Offs += NameTextLength;
                return info;
            }
            start += _fieldProperty.IndicatorPixelWidth;

            if (_content != null)
            {
                var info = _content.HitTest(x - start, y);
                info.X += start;
                info.ChildIndex = 2; // 2 表示 _content
                info.TextIndex = info.Offs;
                info.Offs += NameTextLength + IndicatorTextLength;
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

        int ContentTextLength
        {
            get
            {
                if (_content == null)
                    return 0;
                return _content.TextLength;
            }
        }

        // 返回的内容不包含字段结束符
        public string MergeText(int start = 0, int end = int.MaxValue)
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
        public string MergeTextMask(int start = 0, int end = int.MaxValue)
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
            name_text = GetMaskString(name_text);

            var indicator_text = _indicator?.MergeText(start, end);
            if (_indicator != null)
            {
                start -= _indicator.TextLength;
                if (end != -1)
                    end -= _indicator.TextLength;
            }
            indicator_text = GetMaskString(indicator_text);

            var content_text = _content?.MergeText(start, end);
            if (this.IsHeader)
                content_text = GetMaskString(content_text); // TODO: 可以改进为最多转换 24 char 成为 mask char

            return (name_text ?? "")
                + (indicator_text ?? "")
                + (content_text ?? "");

            string GetMaskString(string text)
            {
                if (text == null)
                    return null;
                return new string((char)0x01, text?.Length ?? 0);
            }
        }

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
            var start = _fieldProperty.CaptionPixelWidth;
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
                    && offs + direction == _name.TextLength)
                    goto DO_INDICATOR;
                var ret = _name.MoveByOffs(offs, direction, out info);
                info.X += start;
                info.ChildIndex = 0; // 0 表示 _name
                info.TextIndex = info.Offs;
                //info.Offs += 0;
                info.Offs += offs_original - offs; // 保持原来的偏移量
                return ret;
            }

        DO_INDICATOR:
            offs -= (NameTextLength);
            start += _fieldProperty.NamePixelWidth;
            if (_indicator != null
                && IsHeader == false && _indicator != null
                && offs + direction >= 0 && offs + direction <= _indicator.TextLength)
            {
                var ret = _indicator.MoveByOffs(offs, direction, out info);
                info.X += start;
                info.ChildIndex = 1; // 1 表示 _indicator
                info.TextIndex = info.Offs;
                //info.Offs += _name.TextLength;
                info.Offs += offs_original - offs; // 保持原来的偏移量
                return ret;
            }

            offs -= (IndicatorTextLength);
            start += _fieldProperty.IndicatorPixelWidth;
            if (_content != null
                && offs + direction >= 0 && offs + direction <= _content.TextLength)
            {
                var ret = _content.MoveByOffs(offs, direction, out info);
                info.X += start;
                info.ChildIndex = 2; // 2 表示 _content
                info.TextIndex = info.Offs;
                //info.Offs += _name.TextLength + _indicator.TextLength;
                info.Offs += offs_original - offs; // 保持原来的偏移量
                return ret;
            }


            info = new HitInfo();
            return 1;
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

        public void Paint(Gdi32.SafeHDC dc,
            IContext context,
            int x,
            int y,
            Rectangle clipRect,
            int blockOffs1,
            int blockOffs2,
            int virtual_tail_length)
        {
            // 在 _name _indicator _content 中最后一个使用 virtual_tail_length
            IBox right_most = _name;
            if (_indicator != null)
                right_most = _indicator;
            if (_content != null)
                right_most = _content;

            x = _fieldProperty.CaptionX;
            var rect = new Rectangle(x, y, _fieldProperty.CaptionPixelWidth, Line.GetLineHeight());

            if (_caption != null
                && clipRect.IntersectsWith(rect))
            {
                /*
                PaintBack(dc,
                    x,
                    y,
                    _caption,
                    Color.LightPink);
                */
                // TODO: 根据最大宽度剪裁
                rect = System.Drawing.Rectangle.Intersect(rect, clipRect);
                _caption.Paint(dc,
                    context,
                    x,
                    y,
                    rect,   // clipRect,
                    0,  //blockOffs1,
                    0,  //blockOffs2,
                    0);
            }

            x += _fieldProperty.CaptionPixelWidth;
            if (_name != null)
            {
                PaintBack(dc,
                    x,
                    y,
                    clipRect,
                    _name,
                    Color.LightPink);
                _name.Paint(dc,
                    context,
                    x,
                    y,
                    clipRect,
                    blockOffs1,
                    blockOffs2,
                    right_most == _name ? virtual_tail_length : 0);
                var delta = _name.TextLength;
                blockOffs1 -= delta;
                blockOffs2 -= delta;
            }

            x += _fieldProperty.NamePixelWidth;

            if (_indicator != null)
            {
                PaintBack(dc,
                    x,
                    y,
                    clipRect,
                    _indicator,
                    Color.Green);

                _indicator.Paint(dc,
                    context,
                    x,
                    y,
                    clipRect,
                    blockOffs1,
                    blockOffs2,
                    right_most == _indicator ? virtual_tail_length : 0);

                var delta = _indicator.TextLength;
                blockOffs1 -= delta;
                blockOffs2 -= delta;
            }

            x += _fieldProperty.IndicatorPixelWidth;

            if (_content != null)
                _content.Paint(dc,
                    context,
                    x,
                    y,
                    clipRect,
                    blockOffs1,
                    blockOffs2,
                    right_most == _content ? virtual_tail_length : 0);
        }

        // 刷新(更新)字段 Caption 文字
        // parameters:
        //      update_rect [out] 返回需要更新的矩形区域。坐标是本对象文档坐标
        public void RefreshCaptionText(
            SafeHDC dc,
            out Rectangle update_rect)
        {
            if (_caption == null)
                _caption = new Line();
            /*
            var caption = field_name;
            if (string.IsNullOrEmpty(caption) == false)
                caption += " ";
            */
            var caption = _fieldProperty.GetFieldCaption?.Invoke(this);
            _caption.ReplaceText(dc,
                0,
                -1,
                caption,
                int.MaxValue,   // _fieldProperty.CaptionPixelWidth,
                null,
                out string replaced,
                out update_rect,
                out Rectangle scroll_rect,
                out int scroll_distance);
            update_rect.Offset(_fieldProperty.CaptionX, 0);
        }

        public int ReplaceText(Gdi32.SafeHDC dc,
            int start,
            int end,
            string content,
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

            var text = this.MergeText();
            if (end == -1)
                end = text.Length;
            replaced = text.Substring(start, end - start);
            text = text.Substring(0, start) + content + text.Substring(end);
            var name_value = "";
            var indicator_value = "";
            var content_value = "";

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

            if (string.IsNullOrEmpty(name_value) == false)
            {
                if (_name == null)
                    _name = new Line();
                _name.ReplaceText(dc,
                    0,
                    -1,
                    name_value,
                    _fieldProperty.NamePixelWidth,
                    context,
                    out replaced,
                    out Rectangle update_rect_name,
                    out scroll_rect,
                    out scroll_distance);
                update_rect_name.Offset(_fieldProperty.NameX, 0);
                update_rect = System.Drawing.Rectangle.Union(update_rect, update_rect_name);
            }
            else
            {
                // 至少要有一个 _name
                if (this.IsHeader == false)
                    if (_name == null)
                        _name = new Line();
            }

            // 如果 Name 内容发生变化，要连带触发变化 Caption 的内容
            if (_fieldProperty.GetFieldCaption != null
                && name_value != old_name_value)
            {
                RefreshCaptionText(dc,
                    out Rectangle update_rect_caption); // 返回前 update_rect_caption 已经被平移过了
                update_rect = System.Drawing.Rectangle.Union(update_rect, update_rect_caption);
            }

            if (string.IsNullOrEmpty(indicator_value) == false)
            {
                if (_indicator == null)
                    _indicator = new Line();

                _indicator.ReplaceText(dc,
        0,
        -1,
        indicator_value,
        _fieldProperty.IndicatorPixelWidth,
        context,
        out replaced,
        out Rectangle update_rect_indicator,
        out scroll_rect,
        out scroll_distance);
                update_rect_indicator.Offset(_fieldProperty.IndicatorX, 0);
                update_rect = System.Drawing.Rectangle.Union(update_rect, update_rect_indicator);
            }

            int new_width = 0;
            if (string.IsNullOrEmpty(content_value) == false)
            {
                if (_content == null)
                    _content = new Paragraph();
                new_width = _content.ReplaceText(dc,
    0,
    -1,
    content_value,
    Math.Max(pixel_width - (_fieldProperty.ContentX), Line.GetAverageCharWidth() * 5),
    context,
    out replaced,
    out Rectangle update_rect_content,
    out scroll_rect,
    out scroll_distance);
                update_rect_content.Offset(_fieldProperty.ContentX,
                    0);
                update_rect = System.Drawing.Rectangle.Union(update_rect, update_rect_content);
            }

            return _fieldProperty.ContentX
                + new_width;
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


        public int GetPixelWidth()
        {
            return _fieldProperty.CaptionPixelWidth
                + _fieldProperty.NamePixelWidth
                + _fieldProperty.IndicatorPixelWidth
                + _content.GetPixelWidth();   // _fieldProperty.ContentPixelWidth;
        }

        public string FieldName
        {
            get
            {
                if (_name == null)
                    return null;
                return this._name.MergeText();
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
    }


    // 字段的一些共同参数
    public class FieldProperty
    {
        public GetFieldCaptionFunc GetFieldCaption { get; set; }

        // 字段名称注释的像素宽度
        public int CaptionPixelWidth { get; set; } = DefaultSplitterPixelWidth;

        // 字段名称的像素宽度
        public int NamePixelWidth { get; set; }

        // 指示符的像素宽度
        public int IndicatorPixelWidth { get; set; }

        // 内容的像素宽度
        // public int ContentPixelWidth { get; set; }

        // 字段指示符
        public char FieldEndChar { get; set; } = (char)30; // 字段结束符

        public const char FieldEndCharDefault = (char)30; // 字段结束符

        // 动态适应 client_width 的像素宽度
        public void Refresh(/*int client_width*/)
        {
            var averageCharWidth = Line.GetAverageCharWidth();
            NamePixelWidth = averageCharWidth * 3; // 默认 60 像素宽度
            IndicatorPixelWidth = averageCharWidth * 2; // 默认 30 像素宽度
            // ContentPixelWidth = client_width - NamePixelWidth - IndicatorPixelWidth - CaptionPixelWidth; // 默认 300 像素宽度
        }

        // 检测分割条和 Caption 区域
        // return:
        //      -2  Caption 区域
        //      -1  Splitter 区域
        //      0   其它区域(包括 name indicator 和 content 区域)
        public int TestSplitterArea(int x)
        {
            if (x < this.CaptionPixelWidth - this.SplitterPixelWidth)
                return -2;
            if (x < this.CaptionPixelWidth)
                return -1;
            return 0;
        }

        public static int DefaultSplitterPixelWidth = 4;

        public int SplitterPixelWidth = DefaultSplitterPixelWidth;

        public int CaptionX
        {
            get
            {
                return 0;
            }
        }

        public int NameX
        {
            get
            {
                return this.CaptionPixelWidth;
            }
        }

        public int IndicatorX
        {
            get
            {
                return this.CaptionPixelWidth + this.NamePixelWidth;
            }
        }

        public int ContentX
        {
            get
            {
                return this.CaptionPixelWidth
                    + this.NamePixelWidth
                    + this.IndicatorPixelWidth;
            }
        }

        public bool DeltaCaptionWidth(int delta)
        {
            var old_value = this.CaptionPixelWidth;
            this.CaptionPixelWidth += delta;
            this.CaptionPixelWidth = Math.Max(this.SplitterPixelWidth, this.CaptionPixelWidth);

            return this.CaptionPixelWidth != old_value;
        }
    }

    public delegate string GetFieldCaptionFunc(MarcField field);
}
