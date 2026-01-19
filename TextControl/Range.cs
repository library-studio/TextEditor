using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.Usp10;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 一个最小文字单元
    /// </summary>
    public class Range : IBox, IDisposable
    {
        public string Name { get; set; }

        public IBox Parent { get; set; }

        public SCRIPT_ITEM Item;

        public SCRIPT_ANALYSIS a;   // 从 this.Item.a 复制过来

        // 记载 ScriptShape() 实际用到的字体
        Font _font = null;
        public Font Font
        {
            get
            {
                return _font;
            }
            set
            {
                _font = value;
            }
        }

        IntPtr _font_handle = IntPtr.Zero;
        /*
        public IntPtr FontHandle
        {
            get
            {
                if (_font_handle == IntPtr.Zero)
                {
                    _font_handle = _font?.ToHfont() ?? IntPtr.Zero;
                }
                return _font_handle;
            }
        }
        */

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

        // public int Left; // 左边界 x 坐标
        public int PixelWidth; // 该 Range 的像素宽度

        // public int Y; // 该 Range 的左上角 y 坐标

        public SCRIPT_VISATTR[] sva; // ScriptShape() 返回的视觉属性
        public ushort[] glfs;
        public int[] advances; // ScriptPlace() 返回的 advances
        public ushort[] logClust; // ScriptShape() 返回的 logClust
        public GOFFSET[] pGoffset; // ScriptPlace() 返回的 pGoffset
        public ABC pABC;

        public float Ascent;    // 上部
        public float Spacing;   // 中部
        public float Descent;   // 下部

        public object Tag { get; set; }

        public int TextLength => _text?.Length ?? 0;

        public Range()
        {
            Text = "";
            DisplayText = "";   // 2025/11/27
        }

        public Range(string text)
        {
            Text = text;
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

            Paragraph.InitializeUspEnvironment();

            // 获得当前内容 Rectangle
            Rectangle old_rect = System.Drawing.Rectangle.Empty;
            {
                var w = this.GetPixelWidth();
                var h = this.GetPixelHeight();
                old_rect = new Rectangle(0, 0, w, h);
                // old_rect = GetBoundRect();
            }


            string old_content = this.MergeText();

            if (end == -1)
                end = old_content.Length;
            var content = old_content.Substring(0, start) + text + old_content.Substring(end);

            // 如果没有发生变化
            if (content == old_content)
            {
                result.MaxPixel = this.GetPixelWidth();
                return result;
            }

            this.Clear();

            if (string.IsNullOrEmpty(content))
            {
                this.Clear();
                this.Tag = null;
                result.UpdateRect = old_rect;
                result.MaxPixel = 0;
                return result;
            }

            var contents = context?.SplitRange?.Invoke(this, content)
                ?? new Segment[] {
                    new Segment
                    {
                        Text = content,
                        Tag = null
                    }
                };

            if (contents.Length > 1)
                throw new ArgumentException($"content 内容 '{content}' 不符合 Range 构建要求。要求必须是一段不可分割的文本");

            foreach (var seg in contents)
            {
                var segment = seg.Text;
                var segment_text = segment.Length == 0 ? " " : segment;
                int cMaxItems = segment_text.Length + 1;
                var pItems = new SCRIPT_ITEM[cMaxItems + 1];

                var result0 = ScriptItemize(convertText(segment_text),
                    segment_text.Length,
                    cMaxItems,
                    Paragraph.sc,
                    Paragraph.ss,
                    pItems,
                    out int pcItems);
                result0.ThrowIfFailed();

                Array.Resize(ref pItems, pcItems);

                if (pcItems > 1)
                    throw new ArgumentException($"content 内容 '{content}' 不符合 Range 构建要求。要求必须是一段不可分割的文本");

                {
                    var item = pItems[0];
                    string str = segment;
                    this.Text = content;
                    this.DisplayText = context?.ConvertText?.Invoke(str) ?? str;
                    this.Item = item;
                    this.a = item.a;
                    this.Tag = seg.Tag;
                }
                break;
            }

            {
                Refresh(
                    (p, o) =>
                    {
                        return context?.GetFont?.Invoke(this, this.Tag);
                    },
                    context,
                    dc,
                    pixel_width);
            }

            // 获得新内容 Rectangle
            Rectangle new_rect = System.Drawing.Rectangle.Empty;
            {
                var w = this.GetPixelWidth();
                var h = this.GetPixelHeight();
                new_rect = new Rectangle(0, 0, w, h);
                // new_rect = GetBoundRect();
            }

            // update_rect = new Rectangle(0, 0, Math.Max(old_rect.Width, new_rect.Width), Math.Max(old_rect.Height, new_rect.Height));
            result.UpdateRect = Utility.Union(new_rect, old_rect);
            result.MaxPixel = this.PixelWidth;
            return result;

            string convertText(string t)
            {
                return context?.ConvertText?.Invoke(t) ?? t;
            }
        }

        void Refresh(
    GetFontFunc func_getfont,
    IContext context,
    SafeHDC hdc,
    int pixel_width)
        {
            //using (var cache = new SafeSCRIPT_CACHE())
            {
                var range = this;

                Font used_font = range.Font;
                var a = range.a;
                var ret = Line.ShapeAndPlace(
                    func_getfont,
                    context,
                    hdc,
                    ref a,
                    //cache,
                    range.DisplayText,
                    out ushort[] glfs,
                    out int[] piAdvance,
                    out GOFFSET[] pGoffset,
                    out ABC pABC,
                    out SCRIPT_VISATTR[] sva,
                    out ushort[] log,
                    ref used_font);
                if (ret == 1)
                {
                    throw new ArgumentException($"内容 '{range.DisplayText}' 中出现了缺乏字形的字符");
                }

                if (range.Font == null)
                    range.Font = used_font; // 记录实际使用的字体

                range.sva = sva; // sva 在 SplitLines() 中尚未计算，是在这里首次计算的。TODO: 将来可以改为在 SplieLines() 结束前计算
                range.advances = piAdvance; // 记录 advances
                range.glfs = glfs;
                range.logClust = log;
                range.pABC = pABC;

                range.PixelWidth = (int)(pABC.abcA + pABC.abcB + pABC.abcC);
                if (true && pABC.abcA < 0)
                {
                    range.PixelWidth += -pABC.abcA;
                }
                if (true && pABC.abcC < 0)
                {
                    range.PixelWidth += -pABC.abcC;
                }

                range.pGoffset = pGoffset; // 记录 pGoffset
                range.a = a;
            }
        }


        public virtual void Clear()
        {
            Item = new SCRIPT_ITEM();
            a = new SCRIPT_ANALYSIS();
            Font = null;
            _text = null;
            _displayText = null;
            PixelWidth = 0;
            sva = null;
            glfs = null;
            advances = null;
            logClust = null;
            pGoffset = null;
            pABC = new ABC();
        }

        public virtual int GetPixelHeight()
        {
            return this.Font?.Height ?? 0;
        }

        public virtual int GetPixelWidth()
        {
            return PixelWidth;
        }

        // TODO: 单元测试
        public string MergeText(int start = 0, int end = int.MaxValue)
        {
            if (_text == null)
                return "";
            if (start == 0 && end >= _text.Length)
                return _text;
            if (start >= _text.Length)
                return "";
            var length = Math.Max(_text.Length - start, end - start);
            return _text.Substring(start, length);
        }

        public bool CaretMoveDown(int x, int y, out HitInfo info)
        {
            info = new HitInfo();
            return false;
        }

        public bool CaretMoveUp(int x, int y, out HitInfo info)
        {
            info = new HitInfo();
            return false;
        }

        public HitInfo HitTest(int x, int y)
        {
            throw new NotImplementedException();
        }

        // return:
        //      -1  越过左边
        //      0   成功
        //      1   越过右边
        public int MoveByOffs(int offs, int direction, out HitInfo info)
        {
            int ret = 0;
            info = new HitInfo();

            Debug.Assert(this._text.Length == this._displayText.Length);

            // 越过左侧
            if (offs + direction < 0)
            {
                offs = 0;
                direction = 0;
                ret = -1;   // 表示越过左侧，但依然要按照 offs + direction 为 0 向后处理
            }
            else if (offs + direction > this._displayText.Length)
            {
                offs = this._displayText.Length;
                direction = 0;
                ret = 1; // 表示越过右侧，但依然要按照 offs + direction 为 _displayText.Length 向后处理
            }

            // 没有任何文本的情况
            if (this._displayText.Length == 0
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

            var range = this;
            {
                if (offs + direction >= 0 && offs + direction <= range.DisplayText.Length)
                {
                    var pos = offs;
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

                        info.X = hit_x_in_range; // + current_x;
                        info.Y = 0;
                        info.ChildIndex = (offs + direction); // Range 的 Child 理解为 char
                        info.Box = range;
                        info.InnerHitInfo = new HitInfo { X = hit_x_in_range };

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

                    info.Offs = offs1 + trailing1;
                    info.TextIndex = offs1;
                    info.Area = Area.Text;
                    info.LineHeight = this.GetPixelHeight();
                    return ret;
                }
            }

            // 越过右侧
            throw new ArgumentException("不应该走到这里");
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
            throw new NotImplementedException();
        }

        // 获得一段文本显示范围的 Region
        // return:
        //      null    空区域
        //      其它      可用 Region
        public virtual Region GetRegion(int start_offs = 0,
            int end_offs = int.MaxValue,
            int virtual_tail_length = 0)
        {
            var rect = GetRect(start_offs, end_offs, virtual_tail_length);
            if (rect.IsEmpty)
                return null;
            return new Region(rect);
#if REMOVED
            if (_text == null)
                return null;

            Debug.Assert(this._text.Length == this._displayText.Length);

            if (start_offs == end_offs)
                return null;
            if (end_offs <= 0)
                return null;
            if (start_offs >= _text.Length)
                return null;
            Rectangle rect;
            if (start_offs == 0 && end_offs >= _text.Length)
                rect = new Rectangle(0, 0, PixelWidth, GetPixelHeight());
            else
            {
                // return:
                //      -1  越过左边
                //      0   成功
                //      1   越过右边
                MoveByOffs(start_offs, 0, out HitInfo info1);
                // 注意，要求 int.MaxValue 也能正常返回
                MoveByOffs(end_offs, 0, out HitInfo info2);
                var left = Math.Min(info1.X, info2.X);
                var right = Math.Max(info1.X, info2.X);
                if (left == right)
                    return null;
                rect = new Rectangle(left,
                    0,
                    right - left,
                    GetPixelHeight());
            }
            if (rect.IsEmpty)
                return null;
            return new Region(rect);
#endif
        }


        public virtual Rectangle GetRect(int start_offs = 0,
            int end_offs = int.MaxValue,
            int virtual_tail_length = 0)
        {
            if (_text == null)
                return Utility.EmptyRect;

            Debug.Assert(this._text.Length == this._displayText.Length);

            if (start_offs == end_offs)
                return Utility.EmptyRect;
            if (end_offs <= 0)
                return Utility.EmptyRect;
            if (start_offs >= _text.Length + virtual_tail_length)
                return Utility.EmptyRect;
            Rectangle rect;
            if (start_offs <= 0 && end_offs >= _text.Length)
                rect = new Rectangle(0, 0, PixelWidth, GetPixelHeight());
            else
            {
                // return:
                //      -1  越过左边
                //      0   成功
                //      1   越过右边
                MoveByOffs(start_offs, 0, out HitInfo info1);
                // 注意，要求 int.MaxValue 也能正常返回
                MoveByOffs(end_offs, 0, out HitInfo info2);
                var left = Math.Min(info1.X, info2.X);
                var right = Math.Max(info1.X, info2.X);
                if (left == right
                    && virtual_tail_length == 0)
                    return Utility.EmptyRect;
                rect = new Rectangle(left,
                    0,
                    right - left,
                    GetPixelHeight());
            }
            if (virtual_tail_length > 0
                && start_offs <= _text.Length
                && end_offs >= _text.Length + virtual_tail_length)
            {
                Debug.Assert(rect.IsEmpty == false);
                // TODO: 移入 Metrics 中
                // 代表回车换行符号字符的像素宽度

                rect.Width += FontContext.DefaultReturnWidth;
            }

            if (rect.IsEmpty || rect.Width == 0)
                return Utility.EmptyRect;
            return rect;
        }


        // 设置好本 Range 的文字基线
        public void ProcessBaseline(IContext context,
            Font default_font)
        {
            var range = this;
            var font = range.Font;
            if (font == null)
                font = default_font;

            var metrics = context.GetFontCache(font).FontMetrics;
            this.Ascent = metrics.Ascent;
            this.Spacing = metrics.Spacing;
            this.Descent = metrics.Descent;

#if REMOVED
            var fontFamily = font.FontFamily;
            var height = font.GetHeight();

            var ascent = fontFamily.GetCellAscent(font.Style);
            var descent = fontFamily.GetCellDescent(font.Style);
            var line_spacing = fontFamily.GetLineSpacing(font.Style);

            var em_height = line_spacing;
            var spacing = em_height - (ascent + descent);

            var up_height = height * ascent / em_height;
            var spacing_height = height * spacing / em_height;
            var below_height = height * descent / em_height;

            // Debug.WriteLine($"{fontFamily.Name} height={height} em_height={em_height} spacing={spacing} ascent={ascent} descent={descent} up_height={up_height} blow_height={below_height} spacing_height={spacing_height}");

            this.Ascent = up_height;
            this.Spacing = spacing_height;
            this.Descent = below_height;
#endif
        }

        public virtual void ClearCache()
        {

        }

        public void Dispose()
        {
            if (_font_handle != IntPtr.Zero)
            {
                Gdi32.DeleteObject(_font_handle);
                _font_handle = IntPtr.Zero;
            }
        }

        public float BaseLine
        {
            get
            {
                return this.Ascent + this.Spacing;
            }
        }

        public float Below
        {
            get
            {
                return this.Descent;
            }
        }
    }

}
