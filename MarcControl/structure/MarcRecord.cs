using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;

using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 一个 MARC 记录编辑区域
    /// 由若干 MarcField 构成
    /// </summary>
    public class MarcRecord : IViewBox, IEnumerable<MarcField>, IDisposable
    {
        public string Name { get; set; }

        public IBox Parent
        {
            get
            {
                return (IBox)_marcControl;
            }
            set
            {
                _marcControl = value as MarcControl;
            }
        }

        internal MarcControl _marcControl = null;
        public MarcControl GetControl() { return _marcControl; }

        List<MarcField> _fields = new List<MarcField>();

        // 引用字段共同属性
        Metrics _fieldProperty;
        public Metrics Metrics
        {
            get
            {
                return _fieldProperty;
            }
            set
            {
                _fieldProperty = value;
            }
        }

        public ViewMode ViewMode { get; set; } = ViewMode.Expand;

        public MarcRecord(MarcControl control,
            Metrics fieldProperty)
        {
            _marcControl = control;
            _fieldProperty = fieldProperty;
        }

        // 包含了(除了头标区以外)每个字段的结束符
        public int TextLength
        {
            get
            {
                if (_fields == null || _fields.Count == 0)
                    return 0;
                // return _fields.Sum(f => f.PureTextLength) + _fields.Count - 1;
                return _fields.Sum(f => f.TextLength);
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
            /*
            info = new HitInfo();
            // 如果 x 在 Name 和 Indicator 区域，这里要能得到当前字段的 Pixel 高度
            y += Line.GetLineHeight();
            if (y >= this.GetPixelHeight())
                return false;
            info = this.HitTest(x, y);
            return true;
            */

            if (this._fields == null)
            {
                info = new HitInfo();
                return false;
            }

            // 注: 如果这里了解了 x 在 Field 的哪个个区域，可以用 HitTest() 实现移动
            // 但这样 Field 内部结构侵入了 MarcRecord 的代码，不太好 

            // 先定位当前字段的 index
            var temp = this.HitTest(x, y);
            var index = temp.ChildIndex;
            if (index < 0 || index >= _fields.Count)
            {
                info = new HitInfo();
                return false;
            }

            var start_y = SumHeight(_fields, index);

            var field = _fields[index];
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

            if (index >= this._fields.Count - 1)
            {
                // 到最后一个字段之下的 Name 区第一个字符位置
                if (index == this._fields.Count - 1)
                {
                    MoveByOffs(this.TextLength,
                        0,
                        out info);
                    return true;
                }

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

                // 如果越过整个 MarcRecord 下沿
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
            /*
            info = new HitInfo();
            y -= Line.GetLineHeight();
            if (y < 0)
                return false;
            info = this.HitTest(x, y);
            return true;
            */

            if (this._fields == null)
            {
                info = new HitInfo();
                return false;
            }

            // 先定位当前字段的 index
            var temp = this.HitTest(x, y);
            var index = temp.ChildIndex;
            // 当前正在最后一个字段以下位置。要移动到最后一个字段的 name 区第一字符位置
            if (index == _fields.Count)
            {
                int y0 = SumHeight(_fields, _fields.Count);
                info = this.HitTest(x, y0 - 1);
                return true;
                /*
                // 获得倒数第一个字段的 start offs
                this.GetContiguousFieldOffsRange(_fields.Count - 1,
                    1,
                    out int start_offs,
                    out _);
                MoveByOffs(start_offs, 0, out info);
                return true;
                */
            }
            if (index < 0 || index >= _fields.Count)
            {
                info = new HitInfo();
                return false;
            }

            var start_y = SumHeight(_fields, index);

            var field = _fields[index];
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
                    var prev_field = _fields[index - 1] as MarcField;
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

        public void Clear()
        {
            DisposeFields();
        }

        int _blankLineHeigh = 0;

        public int BlankLineHeight
        {
            get
            {
                return _blankLineHeigh;
            }
        }

        // 加上 FontContext.DefaultFontHeight 是为了让最后一行的 caret 可见
        public int GetPixelHeight()
        {
            _blankLineHeigh = FontContext.DefaultFontHeight;
            if (_fields == null
                || _fields.Count == 0)
                return _blankLineHeigh;
            return _fields.Sum(field => field.GetPixelHeight())
                + _blankLineHeigh;
        }

        public HitInfo HitTest(int x, int y)
        {
            // 把小于 0 的 y 位置调整为 0，这样当作是点击在第一行的顶部
            if (y < 0)
            {
                y = 0;
            }

            var result = new HitInfo();
            int current_y = 0;
            int offs = 0;
            for (int i = 0; i < _fields.Count; i++)
            {
                var field = _fields[i] as MarcField;
                Debug.Assert(field != null);
                int return_length = (i == 0 ? 0 : 1); // 第一行没有字段结束符
                // bool isLastField = (i == _fields.Count - 1);
                bool isLastField = false;
                var height = field.GetPixelHeight();
                if (y < current_y)
                    break;
                if (y >= current_y && (y < current_y + height || isLastField))
                {
                    var result_info = field.HitTest(x,
    y - current_y);
                    var area = Area.Text;
                    if (result_info.ChildIndex < 0)
                        area = Area.LeftBlank;
                    if (y < current_y + height)
                        area |= Area.Text;
                    else
                        area |= Area.BottomBlank;
                    return new HitInfo
                    {
                        X = result_info.X,
                        Y = current_y + result_info.Y,
                        ChildIndex = i,
                        // RangeIndex = hit_range_index,
                        TextIndex = result_info.Offs,
                        Offs = offs + result_info.Offs,
                        LineHeight = result_info.LineHeight,
                        Area = area,    // y < current_y + height ? Area.Text : Area.BottomBlank,
                        InnerHitInfo = result_info,
                    };
                }

                current_y += height;

                // TODO: 改为使用 .FullTextLength
                offs += field.PureTextLength + return_length;
            }

            this.MoveByOffs(offs,
                0,
                out HitInfo final_info);
            return final_info;

            // 空白内容
            // return new HitInfo { Area = Area.BottomBlank };
        }

        // 2025/12/13
        // 模拟保护性删除动作。
        // 把字符串中的 mask char 有条件保留替换，删除其余字符。
        // 遇到字段结束符的时候，要把之前的完整的 5 字符(或者 3 字符)的 mask 也丢弃
        // mask char 规则: 0x01~0x03 表示字段名位置, 0x04~0x05 表示指示符位置, 0x06 表示头标区位置(最多 24 个字符都是这个值)
        // parameters:
        public static string CompressMaskText(string text,
            char padding_char = ' ')
        {
            // 从头标区最后一个字符，看出当前是头标区的下一个字段开头
            char prev_char = (char)0;
            // 将 mask_text 中 0x01 字符替换为空格，其余内容丢弃
            var result = new StringBuilder();
            foreach (var ch in text)
            {
                if (ch == (char)0x06)
                    result.Append(ch);
                else if (ch == Metrics.FieldEndCharDefault)
                {
                    // TryRemoveTailNormal(result);
                    if (result.Length >= 5 && Last(result) == 5)
                    {
                        TryRemoveTailMask(result, 5);
                    }
                    else if (result.Length >= 3 && Last(result) == 3)
                    {
                        TryRemoveTailMask(result, 3);
                    }

                    // 字段结束符第一阶段要追加进去，用来阻挡 TryRemoveTail() 向左越过当前字段范围
                    result.Append(ch);
                }
                else if (ch >= (char)0x01 && ch <= (char)0x05)
                {
                    result.Append(ch);
                }

                prev_char = ch;
            }

            // 2026/1/14
            TryForwardRemoveMask(result);

            return Clean(result.ToString());

            // 尝试从头到尾遍历，移除邻接字段结束符的连续的一段 mask 字符
            // 如果 start_value 不为 0，则表示从该值开始检查连续性。如果为 0，表示每当中间遇到字段结束符时才开始计算增量
            void TryForwardRemoveMask(StringBuilder b, char start_value = (char)0)
            {
                if (b.Length == 0)
                {
                    return;
                }

                char value = start_value;
                bool in_range = value == (char)1;
                int index = 0;
                // 正着检查，看 value 是否连续
                while (index < result.Length)
                {
                    if (b[index] == Metrics.FieldEndCharDefault)
                    {
                        value = (char)1;
                        in_range = true;
                        index++;
                        continue;
                    }
                    if (b[index] != value)
                    {
                        value = (char)0; // 出现不连续的
                        in_range = false;
                        index++;
                        continue;
                    }


                    if (in_range)
                    {
                        b.Remove(index, 1);

                        value++;
                        if (value > 5)
                        {
                            value = (char)0;
                            in_range = false;
                        }
                    }
                    else
                    {
                        index++;
                    }
                }
            }

            // 尝试移除末尾连续的一段 mask 字符
            void TryRemoveTailMask(StringBuilder b, int len)
            {
                if (b.Length < len)
                {
                    return;
                }

                int s = b.Length - len;
                int e = b.Length - 1;
                char value = (char)len;
                // 倒着检查，看 value 是否连续
                for (int i = e; i >= s; i--)
                {
                    if (b[i] != value)
                    {
                        return; // 出现不连续的
                    }

                    value--;
                }
                b.Remove(s, b.Length - s);
            }

            // 把字段结束符去掉，并替换 0x01~0x06 字符为空格字符。其余字符保留
            string Clean(string s)
            {
                bool debug = false; // 是否调试输出。调试的意思是不用空格替换，而是用 ABCDEF 替换，便于观察
                StringBuilder b = new StringBuilder();
                foreach (var ch in s)
                {
                    if (ch >= 0x01 && ch <= 0x06)
                    {
                        if (debug)
                        {
                            b.Append((char)((int)'A' + (int)ch - 1));
                        }
                        else
                        {
                            b.Append(padding_char);
                        }
                    }
                    else if (ch == Metrics.FieldEndCharDefault)
                    {

                    }
                    else
                    {
                        b.Append(ch);
                    }
                }
                return b.ToString();
            }

            // 获得 StringBuilder 的最后一个字符
            char Last(StringBuilder b)
            {
                if (b.Length == 0)
                {
                    return (char)0;
                }

                return b[b.Length - 1];
            }

            // 获得 StringBuilder 的第一个字符
            char First(StringBuilder b)
            {
                if (b.Length == 0)
                {
                    return (char)0;
                }

                return b[0];
            }
        }

        public string MergeText(int start = 0, int end = int.MaxValue)
        {
            if (end <= start || end <= 0)
                return "";

            StringBuilder builder = new StringBuilder();
            int offs = 0;
            foreach (MarcField field in _fields)
            {
                var current_length = field.FullTextLength;
                // 包含字段结束符
                builder.Append(field.MergeFullText(start - offs, end - offs));
                offs += current_length;
                if (offs > end)
                    break;
            }

            return builder.ToString();

            bool InRange(int offs0, int start0, int end0)
            {
                return offs0 >= start0 && offs0 < end0;
            }
        }


#if OLD
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
                        builder.Append(Metrics.FieldEndCharDefault);
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
#endif

#if REMOVED
        // parameters:
        //      next_char_is_field_end  [out] end 位置之后的一个字符，如果存在这个字符，并且这个字符是字段结束符，则本参数返回 true
        public string MergeTextMask(int start,
            int end,
            out bool reach_field_end)
        {
            reach_field_end = false;
            var text1 = MergeTextMask(start, end);

            if (end != int.MaxValue)
            {
                int end1 = end + 1;
                var next_char = MergeTextMask(start + text1.Length, start + text1.Length + 1);
                if (next_char.Length == 1
                    && next_char[0] == Metrics.FieldEndCharDefault)
                {
                    reach_field_end = true;
                }
            }
            return text1;
        }
#endif

        // TODO: 单元测试
        // 获得带有 Mask Char 的文本内容
        public string MergeTextMask(int start = 0, int end = int.MaxValue)
        {
            if (end <= start || end <= 0)
                return "";

            StringBuilder builder = new StringBuilder();
            int offs = 0;
            int i = 0;
            foreach (MarcField field in _fields)
            {
                var current_length = field.FullTextLength;
                // mask char 规则: 0x01~0x03 表示字段名位置, 0x04~0x05 表示指示符位置, 0x06 表示头标区位置(最多 24 个字符都是这个值)
                builder.Append(field.MergeFullTextMask(start - offs, end - offs));
                offs += current_length;
                /*
                // 除了头标区以外，每个字段末尾都有一个字段结束符
                if (i > 0)
                {
                    if (InRange(offs, start, end))
                        builder.Append(Metrics.FieldEndCharDefault);
                    offs++;
                }
                */
                if (offs > end)
                    break;
                i++;
            }

            return builder.ToString();

            /*
            // TODO: 尝试改用 Utility.InRange
            bool InRange(int offs0, int start0, int end0)
            {
                return offs0 >= start0 && offs0 < end0;
            }
            */
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
        public int MoveByOffs(int offs_param,
            int direction,
            out HitInfo info)
        {
            info = new HitInfo();

            var infos = new List<HitInfo>();

            int offs = 0;
            MarcField field = null;
            int start_y = 0;
            for (int i = 0; i < _fields.Count; i++)
            {
                // info.RangeIndex = 0;
                field = _fields[i] as MarcField;
                // TODO: 改为使用 .FullTextLength
                var line_text_length = field.PureTextLength;
                var return_length = i == 0 ? 0 : 1;
                var text_length = line_text_length + return_length;
                if (offs_param + direction >= offs && offs_param + direction <= offs + text_length) // 2026/1/15 从 < 改为 <=
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

                offs += text_length;
                start_y += field.GetPixelHeight();
            }

            if (offs == offs_param + direction)
            {
                if (_fields.Count > 0)
                {
                    // 最后一个字段的结束符右侧
                    var last_field = _fields.Last();
                    // 找最后一个字段的第一个字符的 caret 位置
                    var ret = last_field.MoveByOffs(0,
        0,
        out HitInfo hit_info);
                    if (ret == 0)
                    {
                        // 头标区得到的 x 在 content 区，要调整到 name 区
                        if (this._fields.Count == 1)
                            hit_info.X = _fieldProperty.NameX;

                        var temp_info = new HitInfo
                        {
                            X = hit_info.X,
                            Y = hit_info.Y + start_y,
                            Area = hit_info.Area,
                            ChildIndex = _fields.Count,
                            TextIndex = hit_info.Offs,
                            Offs = offs + hit_info.Offs,
                            LineHeight = hit_info.LineHeight,
                            InnerHitInfo = hit_info,
                        };
                        info = temp_info;
                        return 0;
                    }
                }
                else
                {
                    // 连头标区都没有
                    info = new HitInfo
                    {
                        X = _fieldProperty.NameX,
                        Y = 0 + start_y,
                        Area = Area.BottomBlank,
                        ChildIndex = _fields.Count,
                        TextIndex = 0,
                        Offs = offs,
                        LineHeight = FontContext.DefaultFontHeight,
                        InnerHitInfo = null,
                    };
                    return 0;
                }
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
                info.LineHeight = 0;    //  Line.GetLineHeight();
                return 0;
            }

            // TODO: 定位到尽量靠后的一个 marcfield 末尾
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

            if (virtual_tail_length < 0
    || virtual_tail_length > 1)
                throw new ArgumentException($"virtual_tail_length ({virtual_tail_length}) 必须为 0 或 1");

            if (this._fields?.Count == 0)
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
            foreach (MarcField field in this._fields)
            {
                var result = field.GetRegion(start_offs - current_offs,
                    end_offs - current_offs,
                    i > 0 ? virtual_tail_length : 0);
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

                // TODO: 改为使用 .FullTextLength
                current_offs += field.PureTextLength + (i == 0 ? 0 : 1);

                y += field.GetPixelHeight();
                i++;
            }

            return region;
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
            int current_start_offs = 0;
            var block_start = Math.Min(blockOffs1, blockOffs2);
            var block_end = Math.Max(blockOffs1, blockOffs2);
            int i = 0;
            foreach (MarcField field in _fields)
            {
                // 剪切区域下方的部分不必参与循环了
                if (y >= clipRect.Bottom)
                    break;

                int paragraph_height = field.GetPixelHeight();
                var rect = new Rectangle(x, y, Int32.MaxValue, paragraph_height);
                if (clipRect.IntersectsWith(rect))
                {
                    field.Paint(
                        context,
                        dc,
                        x,
                        y,
                        clipRect,
                        block_start - current_start_offs,
                        block_end - current_start_offs,
                        i == 0 ? 0 : 1);
                }
                y += paragraph_height;
                // TODO: 改为使用 .FullTextLength
                current_start_offs += field.PureTextLength + (i == 0 ? 0 : 1);
                i++;
            }
        }

        public ReplaceTextResult ReplaceText(
    IContext context,
    SafeHDC dc,
    int start,
    int end,
    string text,
    int pixel_width)
        {
            throw new NotImplementedException();
        }

        // 替换一段文字
        // 若 start 和 end 为 0 -1 表示希望全部替换。-1 在这里表达“尽可能大”
        // 而 0 0 表示在偏移 0 插入内容，注意偏移 0 后面的原有内容会保留
        // parameters:
        //      dc  为初始化新行准备
        //      start   要替换的开始偏移
        //      end     要替换的结束偏移
        //      text    要替换成的内容。
        //              如果为 null，效果基本相当于 ""，但有一点不同，当修改导致 _fields 内只剩下最后一个头标区，并且头标区内容为空时，要自动删除这个头标区的 MarcField
        //      pixel_width   为初始化新行准备
        //      context.splitRange  为初始化新行准备
        // return:
        //      进行折行处理以后，所发现的最大行宽像素数。可能比 pixel_width 参数值要大
        public ReplaceTextResult ReplaceText(
            ViewModeTree view_mode_tree,
            IContext context,
            SafeHDC dc,
            int start,
            int end,
            string text,
            int pixel_width)
        {
            if (end != -1 && start > end)
            {
                throw new ArgumentException($"start ({start}) 必须小于 end ({end})");
            }

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

            var old_fields = FindFieldsV2(
                // _fields,
                start,
                out string left_text,
                end,
                out string right_text,
                text == null ? "" : text,
                out int first_paragraph_index,
                out string replaced);

            if (start == 0 && end == -1 /*&& first_paragraph_index == -1*/)
            {
                first_paragraph_index = 0;
                // fields.Clear();
            }

#if OLD
            // parameters:
            //      delta   整体变化多少。因为 start~end 之间和 replaced_text 可能长度不等，要精确计算需要本参数
            //              负数表示变少
            bool LessThan24(IEnumerable<MarcField> fields,
                int delta)
            {
                int count = delta;
                foreach (var field in fields)
                {
                    count += field.FullTextLength;
                    if (count >= 24)
                        return false;
                }
                return true;
            }

            // 头标区可能修改后缩短，不足 24 字符，需要拉上更多字段来一起 Build
            if (first_paragraph_index == 0)
            {
                // end 等于 -1 怎么办。似乎比较耗费资源
                int end0 = end == -1 ? this.TextLength : end;

                var delta = (text?.Length ?? 0) - (end - start);  // 字符数变化数量
                if (delta < 0)
                {
                    var index = first_paragraph_index + old_fields.Count;
                    while (LessThan24(old_fields, delta))
                    {
                        if (index >= _fields.Count)
                            break;
                        var current = _fields[index++];
                        old_fields.Add(current);
                        right_text += current.MergeFullText();
                    }
                }
                // 如果头标区变长超过 24 字符，要拉上后一个字段一起 Build。让后一个字段分担溢出的字符
                else if (delta > 0
                    && old_fields.Count == 1
                    && delta + old_fields[0].TextLength > 24)
                {
                    var index = first_paragraph_index + old_fields.Count;
                    if (index < _fields.Count)
                    {
                        var current = _fields[index++];
                        old_fields.Add(current);
                        right_text += current.MergeFullText();
                    }
                }
            }

#endif

            result.ReplacedText = replaced;
            result.NewText = text;

            // 如果最后只剩下一个头标区，并且内容为空，则彻底删除这个头标区 MarcField
            bool clear_on_empty = false;
            if (text == null)
            {
                clear_on_empty = true;
                text = "";
            }

            // 2025/11/28
            // 插入符在一个字段末尾键入的情形，优化
            if (text == "\u001e"
                && right_text == "\u001e"
                && replaced.Length == 0
                && old_fields.Count == 1)
            {
                first_paragraph_index++;
                // old_fields[0]?.Dispose();
                old_fields.RemoveAt(0);
                left_text = "";
                right_text = "";
            }

            int max_pixel_width = pixel_width;
            int old_h = old_fields.Sum(p => p.GetPixelHeight());
            int new_h = 0;
            int max_update_width = 0;
            bool update_all = false;    // 是否更新了全部内容?

            /*
            if (text.Contains(_fieldProperty.FieldEndChar) == false
                && replaced.Contains(_fieldProperty.FieldEndChar) == false
                && old_fields.Count == 1)
            * */

            // 优化
            // 若 text 中没有包含字段结束符，意味着不会产生字段增多(分裂)
            if (start > 24
                && old_fields.Count == 1
                && text.Contains(_fieldProperty.FieldEndChar) == false
                && replaced.Contains(_fieldProperty.FieldEndChar) == false)
            {
                var field = old_fields[0] as MarcField;

                // 去掉 right_text 末尾的 \r 字符。避免 SplitLine 多生成一个 field
                if (right_text.EndsWith(new string(Metrics.FieldEndCharDefault, 1)))
                    right_text = right_text.Substring(0, right_text.Length - 1);

                // return:
                //      0   未给出本次修改的像素宽度。需要调主另行计算
                //      其它  本次修改后的像素宽度
                var ret = field.ReplaceText(
                    view_mode_tree?.ChildViewModes?.ElementAtOrDefault(first_paragraph_index),
                    context,
                    dc,
    left_text.Length,
    field.PureTextLength - right_text.Length,   // ?? PureTextLength 正确么？
    text,
    pixel_width);

                if (clear_on_empty
    && _fields.Count == 1
    && _fields[0].PureTextLength == 0)
                    this.Clear();

                update_rect = ret.UpdateRect;
                // var width = new_field.Initialize(dc, field, pixel_width, splitRange);
                if (ret.MaxPixel > max_pixel_width)
                    max_pixel_width = ret.MaxPixel;

                new_h = old_fields.Sum(p => p.GetPixelHeight());
                if (old_h != new_h)
                {
                    // 搜集更新区域最大宽度
                    var current_update_width = update_rect.X + update_rect.Width;
                    if (max_update_width < current_update_width)
                        max_update_width = current_update_width;
                    goto END1;
                }

                ProcessBaseline();

                // 如果高度没有变化，则最小刷新区域
                int y0 = SumHeight(_fields, first_paragraph_index);
                Utility.Offset(ref update_rect, 0, y0);
                result.UpdateRect = update_rect;
                result.MaxPixel = max_pixel_width;
                return result;
            }

            string content = left_text + text + right_text;

            var new_fields = new List<MarcField>();
            if (string.IsNullOrEmpty(content) == false)
            {
                /*
                // 去掉 right_text 末尾的 \r 字符。避免 SplitLine 多生成一个 field
                if (content.EndsWith(new string(Metrics.FieldEndCharDefault, 1)))
                {
                    content = content.Substring(0, content.Length - 1);
                    if (string.IsNullOrEmpty(right_text)
                        && text.EndsWith(new string(Metrics.FieldEndCharDefault, 1)))
                        result.NewText = text.Substring(0, text.Length - 1);
                }
                else
                {
                    if (start >= 24)
                        result.NewText = text + new string(Metrics.FieldEndCharDefault, 1);
                }
                */

                if (content.EndsWith(new string(Metrics.FieldEndCharDefault, 1)))
                {
                }
                else
                {
                    if (start + text.Length > 24)
                        result.NewText = text + new string(Metrics.FieldEndCharDefault, 1);
                }

                // var lines = SplitFields(content, start - left_text.Length);
                var lines = SplitFields_v2(content, start - left_text.Length);

                foreach (var line in lines)
                {
                    var new_field = new MarcField(this,
                        _fieldProperty/*, this.ConvertText*/);
                    if (first_paragraph_index == 0 && new_fields.Count == 0)
                    {
                        // 头标区尺寸不足
                        if (line.Length < 24)
                        {
                        }
                        new_field.IsHeader = true;
                    }

                    // return:
                    //      0   未给出本次修改的像素宽度。需要调主另行计算
                    //      其它  本次修改后的像素宽度
                    var ret = new_field.ReplaceText(
                        view_mode_tree?.ChildViewModes?.ElementAtOrDefault(first_paragraph_index + new_fields.Count),
                        context,
                        dc,
                        0,
                        -1,
                        line,
                        pixel_width);

                    var update_rect1 = ret.UpdateRect;
                    // var width = new_field.Initialize(dc, field, pixel_width, splitRange);
                    if (ret.MaxPixel > max_pixel_width)
                        max_pixel_width = ret.MaxPixel;

                    // 搜集更新区域最大宽度
                    var current_update_width = update_rect1.X + update_rect1.Width + FontContext.DefaultReturnWidth;    // ?? 如果 ReplaceText() 已经考虑了回车符号部分，这里就不用增加了
                    if (max_update_width < current_update_width)
                        max_update_width = current_update_width;

                    new_fields.Add(new_field);
                }
            }

            // TODO: 这里的先 RemoveRange() 后 InsertRange()，如果正好新旧 MarcField 个数相等(特别是只有一个 MarcField 的情况)，
            // 可以考虑优化为，在原有 MarcField 对象基础上进行局部变化。
            // 至少当输入发生在 MarcField 的 Name 和 Indicator 部分的时候，通常是替换，不会引起剧烈变化。



            if (old_fields.Count > 0)
            {
                _fields.RemoveRange(first_paragraph_index, old_fields.Count);
            }
            if (new_fields.Count > 0)
            {
                Debug.Assert(first_paragraph_index >= 0);
                _fields.InsertRange(first_paragraph_index, new_fields);
            }

            if (first_paragraph_index == 0
                && (end == -1 || _fields.Count == new_fields.Count))
                update_all = true;


            // 用被删除的原有对象的宽度进行推动
            // 不应在这里额外增加 return 符号宽度，会让自动根据窗口宽度折行总是有点水平卷滚

            // 注: 这里统计 GetPixelWidth() 有问题。这是按照变化后的 _fieldProperty.ContentX 来进行的，无法得到以前的 max_pixel
            int old_max = old_fields.Count == 0 ? 0 : old_fields.Max(p => p.GetPixelWidth());
            if (old_max > max_update_width)
                max_update_width = old_max;

            RemoveFields(old_fields, 0, old_fields.Count);

            new_h = new_fields.Sum(p => p.GetPixelHeight());

        END1:
            ProcessBaseline();

            // update_rect 用 old_paragraphs 和 new_fields 两个矩形的较大一个算出
            // 矩形宽度最后用 max_pixel_width 矫正一次
            int y = SumHeight(_fields, first_paragraph_index);

            if (update_all)
            {
                result.UpdateRect = new Rectangle(0,
    0,
    Math.Max(max_pixel_width + FontContext.DefaultReturnWidth, max_update_width + FontContext.DefaultReturnWidth),
    Math.Max(old_h, new_h));
                // 因为更新全部内容，就不用卷动旧的内容到底部了
            }
            else
            {
                result.UpdateRect = new Rectangle(0,
                    y,
                    Math.Max(max_pixel_width + FontContext.DefaultReturnWidth, max_update_width + FontContext.DefaultReturnWidth),
                    Math.Max(old_h, new_h));

                result.ScrolledDistance = new_h - old_h;
                if (result.ScrolledDistance != 0)
                {
                    int move_height = SumHeight(_fields, first_paragraph_index, _fields.Count - first_paragraph_index);
                    result.ScrollRect = new Rectangle(0,
            y + old_h,
            Math.Max(max_pixel_width + FontContext.DefaultReturnWidth, max_update_width + FontContext.DefaultReturnWidth),
            move_height);
                }
            }

            result.MaxPixel = max_pixel_width;
            return result;
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

        // 旧版本。对字段结束符的处理有瑕疵。即将废弃。
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
        Metrics.FieldEndCharDefault,
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

        // 旧版本。对字段结束符的处理有瑕疵。即将废弃。
        // TODO: 加入单元测试
        // 将文本内容按行分割。至少会返回一行内容
        public static string[] SplitLines(string text,
    char delimeter = '\r',
    bool contain_return = true)
        {
            List<string> lines = new List<string>();
            StringBuilder line = new StringBuilder();
            foreach (var ch in text)
            {
                if (ch == delimeter)
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

        // parameters:
        //      start   content 部分内容在整个文本中的开始偏移
        public static List<string> SplitFields_v2(string content,
            int start)
        {
            if (start < 0)
                throw new ArgumentException($"start 参数值不应小于 0 ({start})", nameof(start));
            if (content == null)
                throw new ArgumentException($"content 参数值不应为 null", nameof(content));

            string header = null;
            if (start < 24)
            {
                var first_length = Math.Min(content.Length, 24 - start);
                header = content.Substring(0, first_length);
                content = content.Substring(first_length);
                if (string.IsNullOrEmpty(content))
                    content = null;
            }

            var lines = new List<string>();

            if (content != null)
            {
                lines = SplitLines_v2(content,
        Metrics.FieldEndCharDefault,
        contain_return: false).ToList();
            }

            if (start < 24 && header != null)
            {
                Debug.Assert(header.Length <= 24);
                lines.Insert(0, header);
            }

            /*
            if (lines.Count == 0)
                lines.Add("");
            */
            return lines;
        }

        // 将文本内容按行分割。至少会返回一行内容
        // "\r" 理解为最后正好一个空内容字段。
        // "\r2" 理解为最后一个字段内容为 ‘2’，但缺少了结束符。会自动补全
        // 问题: 因为 yield return 了，所以函数似乎不方便返回修正状态信息
        public static IEnumerable<string> SplitLines_v2(string text,
    char delimeter = '\r',
    bool contain_return = true,
    bool complete_delimeter = true)
        {
            List<string> lines = new List<string>();
            StringBuilder line = new StringBuilder();
            foreach (var ch in text)
            {
                if (ch == delimeter)
                {
                    if (line == null)
                        line = new StringBuilder();
                    if (contain_return)
                        line.Append(ch);
                    yield return line.ToString();
                    line = null;
                }
                else
                {
                    if (line == null)
                        line = new StringBuilder();
                    line.Append(ch);
                }
            }
            if (line != null)
            {
                if (complete_delimeter && contain_return)
                    line.Append(delimeter);

                yield return line.ToString();
            }
        }


        // 旧版本函数。有缺陷，不能识别 24 offs 以后的字段被删除结束符的情况(这时间应当把后一个字段连带返回)
        // 另外，借用了 LocateFields() 函数，并不能满足功能需要，但又不方便改变 LocateFields() 函数
        public List<MarcField> FindFields(
    // List<IBox> fields,
    int start,
    out string left_text,
    int end,
    out string right_text,
    out int first_paragraph_index,
    out string replaced)
        {
            var infos = LocateFields(start, end);
            if (infos.Length == 0)
            {
                // 头标区长度不足
                if (start >= this.TextLength && start < 24)
                {
                    left_text = this._fields.Count == 0 ? "" : this._fields[0].MergeFullText();
                    right_text = "";
                    first_paragraph_index = 0;
                    replaced = "";
                    if (this._fields.Count > 0)
                        return new List<MarcField>() { _fields[0] };
                    else
                        return new List<MarcField>();
                }
                else
                {
                    // 走到这里，一定是 start == end 并且在最后一个字符以右位置
                    left_text = "";
                    right_text = "";
                    first_paragraph_index = _fields.Count;
                    replaced = "";
                    return new List<MarcField>();
                }
            }
            var first = infos[0];
            var last = infos.Last();
            left_text = first.Field?.MergeFullText(0, first.StartLength) ?? "";
            var last_field_text_length = last.Field?.FullTextLength ?? 0;
            right_text = last.Field?.MergeFullText(last_field_text_length - last.EndLength, last_field_text_length) ?? "";
            first_paragraph_index = first.Index;
            Debug.Assert(first_paragraph_index != -1);
            replaced = this.MergeText(start, end);

            var result = infos.Select(o => o.Field).ToList();

            /*
            // 如果 paragraphs.Count == 1，并且里面是头标区，则调整 paragraphs 和 right_text，加入头标区后的第一个字段进入其中
            // var tail_index = last.Index + 1;
            while (first_paragraph_index == 0 && LessThan24(result))
            {
                var new_field = _fields[tail_index++];
                result.Add(new_field);
                right_text += new_field.MergeFullText();
            }
            */
            return result;

        }

        // 新版本函数。
        // 能识别 24 offs 以前的字符数减少情况，把后面的足够凑够 24 字符的若干字段拉上返回
        // 能识别 24 offs 以后的字段被删除结束符的情况，把后一个字段拉上返回

        public List<MarcField> FindFieldsV2(
            int start,
            out string left_text,
            int end,
            out string right_text,
            string new_text,
            out int first_paragraph_index,
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

            // 新的相对于旧的变化了多少。负数表示变短
            int delta = new_text.Length - (end - start);

            first_paragraph_index = -1;

            var results = new List<MarcField>();
            int offs = 0;
            int i = 0;
            StringBuilder left = new StringBuilder();
            StringBuilder middle = new StringBuilder();
            StringBuilder right = new StringBuilder();
            bool complete = false;
            foreach (MarcField field in _fields)
            {
                var length = field.FullTextLength;
                var compare_length = length;
                if (i == 0)
                    compare_length = Math.Max(24, length);
                if (Utility.Cross(start, end,
                    offs, offs + compare_length)
                    )
                {
                    var start_length = Math.Max(start - offs, 0);   // 防止负数
                    var end_length = Math.Max((offs + length) - end, 0);
                    if (start_length > 0)
                    {
                        var fragment = field.MergeFullText(0, start_length);
                        Debug.Assert(fragment.Length == start_length);
                        left.Append(fragment);
                    }
                    {
                        var fragment = field.MergeFullText(start_length, length - end_length);
                        Debug.Assert(fragment.Length == length - start_length - end_length);
                        middle.Append(fragment);
                    }
                    if (end_length > 0)
                    {
                        var fragment = field.MergeFullText(length - end_length, length);
                        Debug.Assert(fragment.Length == end_length);
                        right.Append(fragment);
                    }

                    results.Add(field);
                    if (results.Count == 1)
                        first_paragraph_index = i;

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
                    // 在头标区范围内，变短，拉上后面的若干字段
                    if (end + delta < 24 && delta < 0)
                    {
                        if (SumTextLength() + delta < 24)
                        {
                            right.Append(field.MergeFullText());
                            results.Add(field);
                        }
                        else
                        {
                            break;
                        }
                    }
                    // 在头标区范围内，变长，拉上后面的一个字段
                    // TODO: 这里的边界情况要单元测试一下
                    if (end + delta <= 24 && delta > 0)
                    {
                        right.Append(field.MergeFullText());
                        results.Add(field);
                        break;
                    }

                    if (end + delta >= 24)
                    {
                        if (IsRemoved())
                        {
                            right.Append(field.MergeFullText());
                            results.Add(field);
                            break;  // 只多添加一个字段到 results 中便结束
                        }
                        else
                        {
                            break;
                        }
                    }
                }

            CONTINUE:
                offs += length;
                i++;
            }

            if (first_paragraph_index == -1)
                first_paragraph_index = i;
            left_text = left.ToString();
            replaced = middle.ToString();
            right_text = right.ToString();
            return results;

            // 判断旧的内容被新内容替换后，是否最后一个位置的结束符被换掉了
            bool IsRemoved()
            {
                char middle_tail = middle.Length == 0 ? (char)0 : middle[middle.Length - 1];
                char new_tail = new_text.Length == 0 ? (char)0 : new_text[new_text.Length - 1];
                if (middle_tail == Metrics.FieldEndCharDefault
                    && new_tail != Metrics.FieldEndCharDefault)
                    return true;
                return false;
            }

            int SumTextLength()
            {
                if (results.Count == 0)
                    return 0;
                return results.Sum(r => r.FullTextLength);
            }
        }
#if OLD
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
            first_paragraph_index = -1; // 表示没有初始化
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

                int min_length = 0;
                if (is_first)
                    min_length = 24;
                /*
                if (i == 0 && paragraph_text_length > 24)
                    paragraph_text_length = 24;
                */

                // 命中
                if ((offs <= end
                    && offs + Math.Max(paragraph_text_length, min_length) + return_length > start)
                    /*|| (extend_first && i == 1)*//* 如果第一个字段命中，则要包含上第二个字段*/)
                {
                    var text = GetFieldText(i);
                    if (paragraphs.Count == 0)
                    {
                        // ?? start - offs 为负数?
                        left_text = text.Substring(0, Math.Max(0, start - offs));
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

            // 2025/12/1
            if (first_paragraph_index == -1)
                first_paragraph_index = i;

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
                return fields[index].MergeText() + Metrics.FieldEndCharDefault;
            }

            bool IsFirstField(int j)
            {
                return j == 0;
            }
        }
#endif
        public int GetFieldY(int index)
        {
            if (index == 0)
                return 0;
            return SumHeight(_fields, index);
        }

        public void PaintBack(
            IContext context,
            SafeHDC hdc,
            int x,
            int y,
            Rectangle clipRect,
            int caret_field_index = -1)
        {
            // 绘制背景色
            {
                {
                    var backColor = context?.GetBackColor?.Invoke(null, false) ?? SystemColors.Window;
                    var rect = clipRect;
                    // 注: 如果直接用 clipRect 来绘制背景，右下会漏一条线的面积没有填充到
                    rect.Width += 1;
                    rect.Height += 1;
                    MarcField.DrawSolidRectangle(hdc, rect, backColor);
                }

                // var height = this.AutoScrollMinSize.Height - FontContext.DefaultFontHeight;
                var height = this.GetPixelHeight() - this._blankLineHeigh;

                // 绘制提示文字区的底色
                {
                    var color = _fieldProperty?.CaptionBackColor ?? Metrics.DefaultCaptionBackColor;
                    if (color != Color.Transparent)
                    {
                        var left_rect = new Rectangle(
                                x,
                                y,
                                _fieldProperty.CaptionPixelWidth,
                                height);
                        MarcField.PaintBack(hdc, left_rect, clipRect, color);
                    }
                }

                // Solid 区
                {
                    var color = _fieldProperty?.SolidColor ?? Metrics.DefaultSolidColor;
                    if (color != Color.Transparent)
                    {
                        var left_rect = new Rectangle(
                            x + _fieldProperty.CaptionPixelWidth,
                            y,
                            _fieldProperty.ContentBorderX - _fieldProperty.CaptionPixelWidth,
                            height);
                        MarcField.PaintBack(hdc, left_rect, clipRect, color);
                    }
                }

                /*
                // 右侧全高的一根立体竖线
                {
                    MarcField.PaintLeftRightBorder(g,
                        x + _fieldProperty.SolidX,
                        y + 0,
                        _fieldProperty.SolidPixelWidth,
                        height,
                        _fieldProperty.BorderThickness);
                }
                */
                {
                    /*
                    var rect = new Rectangle(x + _fieldProperty.SolidX + _fieldProperty.SolidPixelWidth - 1,
                            y + 0,
                            _fieldProperty.BorderThickness,
                            height);
                    MarcField.DrawSolidRectangle(hdc, rect, backColor);
                    */
                    var backColor = _fieldProperty.BorderColor;
                    if (backColor != Color.Transparent)
                    {
                        var x0 = x + _fieldProperty.SolidX + _fieldProperty.SolidPixelWidth;
                        var line_rect = new Rectangle(x0, y, _fieldProperty.BorderThickness, height);
                        if (line_rect.IntersectsWith(clipRect))
                        {
                            MarcField.DrawVertLine(hdc,
        line_rect,
        (COLORREF)backColor);
                        }
                    }
                }
            }

            this.PaintSolidArea(hdc,
                x,
                y,
                clipRect,
                caret_field_index);
        }


        public void PaintSolidArea(SafeHDC hdc,
            int x,
            int y,
            Rectangle clipRect,
            int caret_field_index = -1)
        {
            int i = 0;
            foreach (var field in _fields)
            {
                if (y >= clipRect.Bottom)
                    break;
                field.PaintBackAndBorder(hdc,
x,
y,
clipRect,
i == caret_field_index);
                y += field.GetPixelHeight();
                i++;
            }
        }

        public Rectangle[] GetFocusedRect(
            int x,
            int y,
            int[] caret_field_indices)
        {
            if (caret_field_indices == null
                || caret_field_indices.Length == 0)
                return new Rectangle[0];
            List<Rectangle> results = new List<Rectangle>();
            int current_y = y;
            int i = 0;
            foreach (var field in _fields)
            {
                if (Array.IndexOf(caret_field_indices, i) != -1)
                {
                    results.Add(field.GetFocusedRect(x, current_y));
                    if (results.Count >= caret_field_indices.Length)
                        return results.ToArray();
                }
                current_y += field.GetPixelHeight();
                i++;
            }

            return results.ToArray();
        }

        static int SumHeight(IEnumerable<MarcField> lines, int count)
        {
            int height = 0;
            int i = 0;
            foreach (var line in lines)
            {
                if (i >= count)
                    break;
                height += line.GetPixelHeight();
                i++;
            }
            return height;
        }

        static int SumHeight(List<MarcField> lines, int start, int count)
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
        static int SumTextLength(List<MarcField> lines, int index)
        {
            int length = 0;
            int line_count = 0;
            for (int i = 0; i < Math.Min(lines.Count, index + 1); i++)
            {
                length += lines[i].PureTextLength;
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
                RemoveFields(first_line_index, old_lines.Count);
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

        // 以第一个 MarcField 的基线为基线
        void ProcessBaseline()
        {
            if (this._fields == null || this._fields.Count == 0)
            {
                _baseLine = 0;
                _below = 0;
                return;
            }
            _baseLine = this._fields[0].BaseLine;
            _below = this._fields[0].Below;  // TODO: 加上除第一行以外的所有行的高度?
        }


        public int GetPixelWidth()
        {
            return _fields.Count == 0 ? 0 : _fields.Max(l => l.GetPixelWidth());
        }

        public class InputInfo
        {
            public string Text { get; set; }
            public int Start { get; set; }
            public int End { get; set; }

            public int Caret { get; set; } = -1;
        }

        public InputInfo GetDeleteInfo(HitInfo info,
            DeleteKeyStyle style,
            char padding_char = (char)0)
        {
            var index = info.ChildIndex;

            // 在最后一个字段以后
            if (index >= _fields.Count)
            {
                var text_length = this.TextLength;
                if (padding_char != 0)
                {
                    // 如果头标区字符数不足，当前 index 正好位于头标区的下一个字段
                    if (index == 1 && _fields[0].FullTextLength < 24)
                    {
                        var header = _fields[0];
                        var header_length = header.FullTextLength;

                        var content = header.MergeFullText() + new string(padding_char, 24 - header_length) + new string(padding_char, 5);

                        return new InputInfo
                        {
                            Text = content.Substring(info.Offs),
                            Start = info.Offs,
                            End = header_length,
                            Caret = 24,
                        };
                    }
                    return new InputInfo
                    {
                        Text = (new string(padding_char, 5)),
                        Start = text_length,
                        End = text_length
                    };
                }

                // 无需动作
                return new InputInfo
                {
                    Text = "",
                    Start = text_length,
                    End = text_length
                };
            }

            Debug.Assert(index < _fields.Count);
            var field = _fields[index];

            if (this.GetFieldOffsRange(index, out int start, out int end) == false)
                return null;

            var caret_offs_in_field = info.Offs - start;
            // 头标区
            if (index == 0)
            {
                // 需要填充足够字符，让头标区达到 24 字符
                if (padding_char != 0
                    && end < 24)
                {
                    return Build24();
                }
                // 实际上在下一个字段的第一字符进行替换或插入
                if (end == caret_offs_in_field)
                {
                    if (_fields.Count == 1
                        || _fields[1].TextLength <= 1)
                    {
                        index = 1;  // 调整到头标区后第一个字段，重做
                        var temp = info.Clone();
                        temp.ChildIndex = 1;
                        return GetDeleteInfo(temp,
                            style,
                            padding_char);
                    }
                }
                else
                {
                    // 替换字符
                    return new InputInfo
                    {
                        Text = padding_char == 0 ? " " : new string(padding_char, 1),
                        Start = caret_offs_in_field,
                        End = caret_offs_in_field + 1
                    };
                }
            }

            InputInfo Build24()
            {
                Debug.Assert(padding_char != 0);
                var content = field.MergeFullText() + new string(padding_char, 24 - end);
                // 替换一个字符以后的全部文字
                content = content.Substring(0, caret_offs_in_field) + padding_char + content.Substring(caret_offs_in_field + 1);
                return new InputInfo
                {
                    Text = content.Substring(caret_offs_in_field, 24 - caret_offs_in_field),
                    Start = caret_offs_in_field,
                    End = end,
                };
            }

            // 将填充部分和需要输入的 ch 结合起来构造一个 replaceText() 动作
            // 比如字符数不足，但输入的位置不一定在字段末尾(指结束符之前的位置)，就是典型情况。输入和位置和要填充的一段有点距离，是断开的
            // parameters:
            //      length  试图要达到的字段全部文字长度。不包括结束符
            InputInfo Build(int length)
            {
                Debug.Assert(padding_char != 0);
                // 字段结束符左边的位置
                var pure_end = end - 1;
                // 填充后要达到的全部文字，注意排除了结束符。因为结束符本来就具备，替换和插入均不影响到它
                var content = field.MergePureText() + new string(padding_char, (start + length) - pure_end);
                // 再输入一个字符后要达到的全部文字，注意排除了结束符
                content = content.Substring(0, caret_offs_in_field) + padding_char + content.Substring(caret_offs_in_field + 1);
                return new InputInfo
                {
                    Text = content.Substring(caret_offs_in_field, (content.Length) - caret_offs_in_field),
                    Start = start + caret_offs_in_field,
                    End = pure_end, // 在结束符左边位置
                };
            }

            // 填充，控制字段和普通字段
            if (padding_char != 0)
            {
                if (field.IsControlField)
                {
                    if (end - start < 3 + 1)
                    {
                        return Build(3);
                    }
                }
                else
                {
                    if (end - start < 5 + 1)
                    {
                        return Build(5);
                    }
                }
            }

            // 在字段结束符以右，或者以左，都只能是无需动作
            if (end - 1 <= start + caret_offs_in_field)
            {
                // 要删除结束符
                if (style.HasFlag(DeleteKeyStyle.DeleteFieldTerminator))
                {
                    return new InputInfo
                    {
                        Text = "",
                        Start = end - 1,
                        End = end
                    };
                }
                // 无需动作
                return new InputInfo
                {
                    Text = "",
                    Start = end - 1,
                    End = end - 1
                };
            }
            else
            {
                // 观察是否处在字段名、指示符位置
                if ((field.IsControlField && caret_offs_in_field < 3)
                    || (field.IsControlField == false && caret_offs_in_field < 5))
                {
                    // 填充
                    return new InputInfo
                    {
                        Text = padding_char == 0 ? " " : new string(padding_char, 1),
                        Start = start + caret_offs_in_field,
                        End = start + caret_offs_in_field + 1
                    };
                }

                // 其余位置删除
                return new InputInfo
                {
                    Text = "",
                    Start = start + caret_offs_in_field,
                    End = start + caret_offs_in_field + 1
                };
            }
        }


        // 获得输入一个普通字符的操作信息
        // 注意字段名和字段指示符区域，是覆盖输入效果；内容区是插入字符效果
        // 本函数还可以根据要求，观察输入所在的区域，如果字符数不足规则要求，自动填充足量的字符
        public InputInfo GetInputInfo(HitInfo info,
            char ch,
            char padding_char = (char)0)
        {
            var index = info.ChildIndex;

            // 在最后一个字段以后
            if (index >= _fields.Count)
            {
                var text_length = this.TextLength;
                if (padding_char != 0)
                {
                    // 如果头标区字符数不足，当前 index 正好位于头标区的下一个字段
                    if (index == 1 && _fields[0].FullTextLength < 24)
                    {
                        var header = _fields[0];
                        var header_length = header.FullTextLength;

                        var content = header.MergeFullText() + new string(padding_char, 24 - header_length) + ch + new string(padding_char, 4);

                        return new InputInfo
                        {
                            Text = content.Substring(info.Offs),
                            Start = info.Offs,
                            End = header_length,
                            Caret = 24,
                        };
                    }
                    return new InputInfo
                    {
                        Text = (new string(ch, 1)) + (new string(padding_char, 4)),
                        Start = text_length,
                        End = text_length
                    };
                }

                return new InputInfo
                {
                    Text = new string(ch, 1),
                    Start = text_length,
                    End = text_length
                };
            }

            Debug.Assert(index < _fields.Count);
            var field = _fields[index];

            if (this.GetFieldOffsRange(index, out int start, out int end) == false)
                return null;

            var caret_offs_in_field = info.Offs - start;
            // 头标区
            if (index == 0)
            {
                // 需要填充足够字符，让头标区达到 24 字符
                if (padding_char != 0
                    && end < 24)
                {
                    return Build24();
                    /*
                    var content = field.MergeFullText() + new string(padding_char, 24 - end);
                    // 输入一个字符以后的全部文字
                    content = content.Substring(0, caret_offs_in_field) + ch + content.Substring(caret_offs_in_field + 1);
                    return new InputInfo
                    {
                        Text = content.Substring(caret_offs_in_field, 24 - caret_offs_in_field),
                        Start = caret_offs_in_field,
                        End = end,
                    };
                    */
                }
                // 实际上在下一个字段的第一字符进行替换或插入
                if (end == caret_offs_in_field)
                {
                    //if (_fields.Count == 1
                    //    || _fields[1].TextLength <= 1)
                    {
                        index = 1;  // 调整到头标区后第一个字段，重做
                        var temp = info.Clone();
                        temp.ChildIndex = 1;
                        return GetInputInfo(temp,
            ch,
            padding_char);
                        /*
                        return new InputInfo
                        {
                            Text = new string(ch, 1),
                            Start = end,
                            End = end
                        };
                        */
                    }
                }
                else
                {
                    // 普通替换
                    return new InputInfo
                    {
                        Text = new string(ch, 1),
                        Start = caret_offs_in_field,
                        End = caret_offs_in_field + 1
                    };
                }
            }

            InputInfo Build24()
            {
                var content = field.MergeFullText() + new string(padding_char, 24 - end);
                // 输入一个字符以后的全部文字
                content = content.Substring(0, caret_offs_in_field) + ch + content.Substring(caret_offs_in_field + 1);
                Debug.Assert(content.Length == 24);
                return new InputInfo
                {
                    Text = content.Substring(caret_offs_in_field, 24 - caret_offs_in_field),
                    Start = caret_offs_in_field,
                    End = end,
                };
            }

            // 将填充部分和需要输入的 ch 结合起来构造一个 replaceText() 动作
            // 比如字符数不足，但输入的位置不一定在字段末尾(指结束符之前的位置)，就是典型情况。输入和位置和要填充的一段有点距离，是断开的
            // parameters:
            //      length  试图要达到的字段全部文字长度。不包括结束符
            InputInfo Build(int length)
            {
                // 字段结束符左边的位置
                var pure_end = end - 1;
                // 填充后要达到的全部文字，注意排除了结束符。因为结束符本来就具备，替换和插入均不影响到它
                var content = field.MergePureText() + new string(padding_char, (start + length) - pure_end);
                // 再输入一个字符后要达到的全部文字，注意排除了结束符
                content = content.Substring(0, caret_offs_in_field) + ch + content.Substring(caret_offs_in_field + 1);
                return new InputInfo
                {
                    Text = content.Substring(caret_offs_in_field, (content.Length) - caret_offs_in_field),
                    Start = start + caret_offs_in_field,
                    End = pure_end, // 在结束符左边位置
                };
            }

            // 填充，控制字段和普通字段
            if (padding_char != 0)
            {
                if (field.IsControlField)
                {
                    if (end - start < 3 + 1)
                    {
                        return Build(3);
                    }
                }
                else
                {
                    if (end - start < 5 + 1)
                    {
                        return Build(5);
                    }
                }
            }

            // 在字段结束符以右，或者以左，都只能是插入
            if (end - 1 <= start + caret_offs_in_field)
            {
                return new InputInfo
                {
                    Text = new string(ch, 1),
                    Start = end - 1,
                    End = end - 1
                };
            }
            else
            {
                // 观察是否处在字段名、指示符位置
                if ((field.IsControlField && caret_offs_in_field < 3)
                    || (field.IsControlField == false && caret_offs_in_field < 5))
                {
                    // 替换
                    return new InputInfo
                    {
                        Text = new string(ch, 1),
                        Start = start + caret_offs_in_field,
                        End = start + caret_offs_in_field + 1
                    };
                }


                // 其余位置插入
                return new InputInfo
                {
                    Text = new string(ch, 1),
                    Start = start + caret_offs_in_field,
                    End = start + caret_offs_in_field// + 1
                };
            }
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
            char padding_char,
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
                if (info.ChildIndex == 0 && info.TextIndex >= 24/*_fields[0].TextLength*/)
                {
                    info.ChildIndex++;
                    info.TextIndex = 0;
                }

                if (info.ChildIndex == 0)
                {
                    // if (_fields[0].TextLength >= 24)
                    replace = true;
                }
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
                        fill_content = new string(padding_char, fill_char_count);
                    }
                }
            }
            else if (action == "backspace")
            {
                // 如果在头标区后一字段的开头，则调整为头标区末尾。效果为抹去末尾这个字符为空
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
                Debug.Assert(false, "不应这样调用本函数。功能已经由另一函数取代");

                // 如果在头标区的末尾，则调整为下一字符开头
                if (info.ChildIndex == 0 && info.TextIndex >= 24/*_fields[0].TextLength*/)
                {
                    info.ChildIndex++;
                    info.TextIndex = 0;
                }
                else if (info.ChildIndex == 0 && info.TextIndex < 24)
                {
                    // 在头标区内回车，补足空格
                    fill_char_count = 24 - info.TextIndex;
                    fill_content = new string(padding_char, fill_char_count);
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
        public void UpdateAllCaption(
            IContext context,
            SafeHDC dc,
            out Rectangle update_rect)
        {
            update_rect = System.Drawing.Rectangle.Empty;
            int y = 0;
            foreach (MarcField field in _fields)
            {
                field.RefreshCaptionText(
                    context,
                    dc,
                    out Rectangle update_rect_caption);

                Utility.Offset(ref update_rect_caption, _fieldProperty.CaptionX, y);
                update_rect = Utility.Union(update_rect, update_rect_caption);
                y += field.GetPixelHeight();
            }
        }

        #region 外部接口


        /*
变更说明（简短）
•	泛型枚举器：如果 _fields 为 null 则直接结束；遍历 _fields 并通过 is MarcField 过滤，按添加顺序返回 MarcField。这样可以避免对集合中可能存在的其它 IBox 实现抛出异常。
•	非泛型枚举器：直接返回泛型枚举器（它实现了 IEnumerator），实现简洁且一致。
如果你希望枚举器在集合被修改时抛出（类似 List 的行为），我可以改为返回 _fields.OfType<MarcField>().GetEnumerator() 或 (_fields.Cast<MarcField>()).GetEnumerator()（后者会在存在非 MarcField 元素时抛出）。现在实现更稳健：忽略非目标类型元素。         * */
        IEnumerator<MarcField> IEnumerable<MarcField>.GetEnumerator()
        {
            if (_fields == null)
                yield break;

            foreach (var box in _fields)
            {
                if (box is MarcField field)
                    yield return field;
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            // 非泛型版本直接复用泛型枚举器
            return ((IEnumerable<MarcField>)this).GetEnumerator();
        }

        public MarcField GetField(int index)
        {
            if (this._fields == null)
                return null;
            // 获得最后一个
            if (index == -1)
                return this._fields.LastOrDefault() as MarcField;

            if (index >= this._fields.Count)
                throw new ArgumentException($"index {index} 越过范围。({_fields.Count})");
            return this._fields[index] as MarcField;
        }

        public int FieldCount
        {
            get
            {
                return _fields?.Count ?? 0;
            }
        }


        // 获得一个字段的 offs 起止范围
        public bool GetFieldOffsRange(MarcField field,
    out int start,
    out int end)
        {
            int index = _fields.IndexOf(field);
            if (index == -1)
                throw new ArgumentException("指定的字段对象在 _fields 集合中没有找到");
            return GetFieldOffsRange(index,
            out start,
            out end);
        }

        // 获得一个字段的 offs 起止范围
        public bool GetFieldOffsRange(int field_index,
            out int start,
            out int end)
        {
            return GetContiguousFieldOffsRange(field_index,
                1,
                out start,
                out end);
        }

        // 获得若干连续字段的 offs 起止范围
        // parameters:
        //      field_index 开始的字段 index
        //      count   要获得多少个字段的范围
        //              可用 -1 表示希望尽可能多地获得
        public bool GetContiguousFieldOffsRange(int field_index,
            int count,
            out int start,
            out int end)
        {
            if (field_index == 0
                && count == 1
                && _fields.Count == 0)
            {
                start = 0;
                end = 0;
                return false;
            }

            // index 刚好在末尾。相当于追加效果
            if (field_index == _fields.Count)
            {
                start = this.TextLength;
                end = start;
                return false;
            }

            if (field_index < 0 || field_index >= _fields.Count)
                throw new ArgumentException($"field_index ({field_index}) 越过 字段数 {_fields.Count} 范围");

            if (count != -1 && field_index + count > _fields.Count)
                throw new ArgumentException($"field_index ({field_index}) + count ({count}) 越过 字段数 {_fields.Count}");

            start = -1;
            end = -1;

            int offs = 0;
            int i = 0;
            foreach (MarcField field in _fields)
            {
                // TODO: 改为使用 .FullTextLength
                var raw_text_length = field.PureTextLength;
                if (i == field_index)
                {
                    start = offs;
                }
                if (count != -1
                    && i >= field_index + count)
                {
                    end = offs;
                    return true;
                }
                offs += raw_text_length + (i == 0 ? 0 : 1);

                // start_y += field.GetPixelHeight();
                i++;
            }

            if (start == -1)
                throw new ArgumentException($"下标 {field_index} 在 _fields 集合中没有找到元素");

            end = offs;
            return true;
        }

        // 获得若干字段开头的 offs 列表
        // parameters:
        //      count   打算处理的字段个数。
        //              如果为 0，表示只获得 field_index 处的字段的开头 offs
        //              如果为 1，表示获得 field_index 处的字段的开头 offs 和下一字段开头的 offs，也就是获得两个 int
        //              如果为 -1，表示希望尽可能多地获得
        public List<int> GetFieldOffsList(int field_index,
    int count)
        {
            if (field_index == 0
                && count == 1
                && _fields.Count == 0)
            {
                return new List<int> { 0 };
            }

            // index 刚好在末尾。相当于追加效果
            if (field_index == _fields.Count)
            {
                return new List<int> { this.TextLength };
            }

            if (field_index < 0 || field_index >= _fields.Count)
                throw new ArgumentException($"field_index ({field_index}) 越过 字段数 {_fields.Count} 范围");

            if (count != -1 && field_index + count > _fields.Count)
                throw new ArgumentException($"field_index ({field_index}) + count ({count}) 越过 字段数 {_fields.Count}");

            if (count == -1)
                count = _fields.Count - field_index;

            var results = new List<int>();
            int offs = 0;
            int i = 0;
            foreach (MarcField field in _fields)
            {
                // TODO: 改为使用 .FullTextLength
                var raw_text_length = field.PureTextLength;
                if (i >= field_index)
                {
                    results.Add(offs);
                }
                if (count != -1
                    && i >= field_index + count)
                {
                    return results;
                }
                offs += raw_text_length + (i == 0 ? 0 : 1);
                i++;
            }

            if (results.Count > 0)
                results.Add(offs);
            return results;
        }


        // 根据指定的 offs 范围，定位经过的字段
        // parameters:
        //      return_virtual_field 是否返回最后一个不存在的虚拟字段?
        public LocateInfo[] LocateFields(int start,
    int end)
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

            var results = new List<LocateInfo>();
            int offs = 0;
            int i = 0;
            foreach (MarcField field in _fields)
            {
                // TODO: 改为使用 .FullTextLength
                var raw_text_length = field.PureTextLength;
                var length = raw_text_length + (i == 0 ? 0 : 1);
                // if (offs >= start && offs + length < end)
                if (Utility.Cross(start, end == -1 ? Int32.MaxValue : end,
                    offs, offs + length))
                {
                    var start_length = Math.Max(start - offs, 0);   // 防止负数
                    var end_length = end == -1 ? 0 : Math.Max((offs + length) - end, 0);
                    results.Add(new LocateInfo
                    {
                        Field = field,
                        Index = i,
                        Length = length,
                        StartLength = start_length, // caret
                        EndLength = end_length
                    });
                }

                offs += length;
                i++;
            }

            /*
            // 在 MARC 内容最后一个字符以右
            if (results.Count == 0)
            {
                if (offs < 24)
                {
                    // 算作头标区的一个部分
                    results.Add(new LocateInfo
                    {
                        Field = _fields.Count == 0 ? null : _fields[0],   // 表示这个字段并不存在
                        Index = 0,
                        Length = offs,
                        StartLength = offs, // caret
                        EndLength = 0,
                    });
                }
                else if (return_virtual_field)
                {
                    Debug.Assert(_fields.Count > 0);
                    Debug.Assert(start >= this.TextLength);
                    results.Add(new LocateInfo
                    {
                        Field = null,   // 表示这个字段并不存在
                        Index = i,
                        Length = 0,
                        StartLength = 0, // caret
                        EndLength = 0,
                    });
                }
            }
            */
            return results.ToArray();
        }

        public class LocateInfo
        {
            public MarcField Field { get; set; }

            // 下标
            public int Index { get; set; }

            // 字段 Text 长度。注意包含了字段结束符(头标区没有)
            public int Length { get; set; }

            // 开头未进入命中范围的长度
            public int StartLength { get; set; }

            // 结尾未进入命中范围的长度
            public int EndLength { get; set; }
        }

        // 根据指定的 offs 范围，定位经过的字段
        public bool LocateFields(int start,
int end,
out int field_index,
out int count)
        {
            var results = LocateFields(start,
    end);
            if (results.Length == 0)
            {
                field_index = -1;
                count = 0;
                return false;
            }
            field_index = results[0].Index;
            count = results.Length;
            return true;
        }

        public DomRecord GetDomRecord()
        {
            return new DomRecord(this);
        }

        #endregion

        // 用工作单格式构造 MARC 机内格式的内容字符串
        public static string BuildContent(string value,
            bool ensure_tail_field_end_char = true)
        {
            if (value.Length >= 26)
            {
                if (value[24] != '\r' || value[25] != '\n')
                    throw new ArgumentException($"头标区末尾应该有回车换行字符");
                value = value.Remove(24, 2)
                .Replace("\r\n", "\u001e")
                .Replace("$", "\u001f");
            }
            if (ensure_tail_field_end_char)
            {
                if (value.Length > 24
                    && value.Last() != '\u001e')
                    value += "\u001e";
            }
            return value;
        }

        public void ClearCache()
        {
            if (_fields == null)
                return;
            foreach (var field in _fields)
            {
                field.ClearCache();
            }
        }

        // return:
        //      false   没有完成校验。
        //      true    完成校验。
        public bool VerifyIndexAndLength(int index, int length,
            bool throw_exception = true)
        {
            if (index < 0)
            {
                if (throw_exception)
                    throw new ArgumentException($"index ({index}) 不应小于 0");
                return false;
            }
            if (index + length > this.FieldCount)
            {
                if (throw_exception)
                    throw new ArgumentException($"index ({index}) length ({length}) 越过字段总数 ({this.FieldCount})");
                return false;
            }
            return true;
        }

        public int GetFieldIndex(MarcField field)
        {
            return _fields.IndexOf(field);
        }

        // 按照字段名对所有字段重新排序
        public bool SortFields()
        {
            if (_fields.Count < 2)
            {
                return false;
            }
            // 头标区除外
            var header = _fields[0];
            var list = _fields.GetRange(1, _fields.Count - 1)
                .Select(o => new Tuple<string, MarcField>(o.FieldName, o))
                .ToList();
            list.Sort((a, b) =>
            {
                return string.CompareOrdinal(a.Item1, b.Item1);
            });
            var temp = new List<MarcField>();
            temp.Add(header);
            temp.AddRange(list.Select(o => o.Item2));
            _fields = temp;
            return true;
        }

        public void Dispose()
        {
            DisposeFields();
        }

        void DisposeFields()
        {
            if (_fields == null)
                return;
            foreach (var field in _fields)
            {
                field?.Dispose();
            }

            _fields.Clear();
        }

        // 避免 field 没有 Dispose() 就删除
        static void RemoveFields(List<MarcField> fields, int start, int count)
        {
            for (int i = start; i < start + count; i++)
            {
                fields[i]?.Dispose();
            }

            fields.RemoveRange(start, count);
        }

        public ReplaceTextResult ToggleExpand(HitInfo info,
            IContext context,
            Gdi32.SafeHDC dc,
            int pixel_width)
        {
            if (info.ChildIndex < 0
                && info.ChildIndex >= this.FieldCount)
                return new ReplaceTextResult();
            var field = this.GetField(info.ChildIndex);

            var y0 = SumHeight(_fields, 0, info.ChildIndex);
            var old_height = field.GetPixelHeight();
            var old_width = field.GetPixelWidth();
            var blow_height = SumHeight(_fields, info.ChildIndex, _fields.Count - info.ChildIndex);

            var ret = field.ToggleExpand(info.InnerHitInfo,
                context,
                dc,
                pixel_width);

            var new_height = field.GetPixelHeight();

            ret.Offset(0, y0);
            {
                var new_width = field.GetPixelWidth();
                var rect = ret.UpdateRect;
                rect.Height = new_height;
                rect.Width = Math.Max(old_width, new_width);
                ret.UpdateRect = rect;
            }

            ret.ScrolledDistance = new_height - old_height;
            ret.ScrollRect = new Rectangle(0, y0 + old_height, int.MaxValue, blow_height);

            return ret;
        }

        // 注: “我”自己的 ViewMode 是无所谓的，要靠父对象的 ViewMode 来定义
        public ViewModeTree GetViewModeTree()
        {
            var results = new List<ViewModeTree>();
            foreach (var child in _fields)
            {
                results.Add(child.GetViewModeTree());
            }
            return new ViewModeTree
            {
                Name = "!record",
                ViewMode = ViewMode.Expand,
                ChildViewModes = results
            };
        }
    }
}
