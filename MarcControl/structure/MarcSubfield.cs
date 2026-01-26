using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;

using Vanara.PInvoke;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.Window;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// MARC 子字段
    /// 有两种模式: 平面、表格
    /// 表格模式下，如果 _name 内容为空，表示这是一个字段的指示符后面到第一个子字段之间的文本。这部分文本没有子字段名 
    /// </summary>
    public class MarcSubfield : IViewBox, ICaption, IDisposable
    {
        Line _caption = null;
        public Line Caption
        {
            get
            {
                return _caption;
            }
        }

        Paragraph _content = null;

        FixedLine _name = null;
        Template _template = null;

        public Metrics Metrics { get; set; }

        public MarcSubfield()
        {

        }

        public MarcSubfield(IBox parent, Metrics metrics)
        {
            Parent = parent;
            Metrics = metrics;
        }

        ViewMode _viewMode = ViewMode.None;
        public ViewMode ViewMode
        {
            get
            {
                return _viewMode;
            }
            set
            {
                _viewMode = value;
            }
        }

        public string Name { get; set; }

        // 子字段节点的父节点可能是一个 MarcRecord 节点，也可能是一个 MarcField 节点(比如 UNIMARC 4XX 情形)
        public IBox Parent { get; set; }


        public int TextLength
        {
            get
            {
                if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
                {
                    return _content?.TextLength ?? 0;
                }
                if (_viewMode == ViewMode.Expand)
                {
                    // EnsureNameAndTemplate();
                    return (_name?.TextLength ?? 0) + (_template?.TextLength ?? 0);
                }
                return 0;
            }
        }

        public float BaseLine => _content?.BaseLine ?? 0;

        public float Below => _content?.Below ?? 0;

        int GetContentX(int x0 = 0)
        {
            // Debug.Assert(Metrics != null);
            int button_width = Metrics?.ButtonWidth ?? 0;
            int caption_width = Metrics?.GetCaptionPixelWidth(this) ?? 0;
            int gap_width = Metrics?.GapThickness ?? 0;
            return x0 + caption_width + button_width + gap_width;
        }

        int GetContentY(int y0 = 0)
        {
            return y0;
        }

        int GetTemplateY(int y0 = 0)
        {
            return y0 + _name?.GetPixelHeight() ?? 0;
        }

        Rectangle GetButtonRect(int x = 0, int y = 0)
        {
            int caption_width = Metrics?.GetCaptionPixelWidth(this) ?? 0;

            IBox box;
            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
            {
                // box = _content;
                return new Rectangle(x + caption_width,
y,
Metrics?.ButtonWidth ?? FontContext.DefaultReturnWidth,
FontContext.DefaultFontHeight);
            }
            else
            {
                box = _name;
            }

            {

                var height = FontContext.DefaultFontHeight;
                return new Rectangle(x + caption_width,
        y,
        Metrics?.ButtonWidth ?? FontContext.DefaultReturnWidth,
        height);
            }
        }


        public bool CaretMoveDown(int x, int y, out HitInfo info)
        {
            Debug.Assert(_viewMode != ViewMode.None);

            var x0 = GetContentX();
            var y0 = GetContentY();

            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
            {
                var ret = _content.CaretMoveDown(x - x0, y - y0, out HitInfo sub_info);
                info = sub_info.Clone();
                info.X += x0;
                info.Y += y0;
                info.ChildIndex = (int)FieldRegion.Content;
                // 保持 info.LineHeight
                info.Box = this;
                info.InnerHitInfo = sub_info;
                return ret;
            }
            else if (_viewMode == ViewMode.Expand)
            {
                int name_height = GetTemplateY();
                if (y < name_height)
                {
                    var ret = _name.CaretMoveDown(x - x0,
                        y - y0,
                        out HitInfo sub_info);
                    if (ret == true)
                    {
                        info = sub_info.Clone();
                        info.X += x0;
                        info.Y += y0;
                        info.ChildIndex = (int)FieldRegion.Content;
                        // 保持 info.LineHeight
                        info.Box = this;
                        info.InnerHitInfo = sub_info;
                        return ret;
                    }
                    info = HitTest(x - x0,
                        name_height);
                    return true;
                }
                else
                {
                    var ret = _template.CaretMoveDown(x - x0,
                        y - name_height,
                        out HitInfo sub_info);
                    info = sub_info.Clone();
                    info.X += x0;
                    info.Y += name_height;
                    info.Offs += 2;
                    info.ChildIndex = (int)FieldRegion.Content;
                    // 保持 info.LineHeight
                    info.Box = this;
                    info.InnerHitInfo = sub_info;
                    return ret;
                }
            }

            info = new HitInfo { Box = this };
            return false;
        }

        public bool CaretMoveUp(int x, int y, out HitInfo info)
        {
            Debug.Assert(_viewMode != ViewMode.None);

            var x0 = GetContentX();
            var y0 = GetContentY();

            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
            {
                var ret = _content.CaretMoveUp(x - x0,
                    y - y0,
                    out HitInfo sub_info);
                info = sub_info.Clone();
                info.X += x0;
                info.Y += y0;
                info.ChildIndex = (int)FieldRegion.Content;
                // 保持 info.LineHeight
                info.Box = this;
                info.InnerHitInfo = sub_info;
                return ret;
            }
            if (_viewMode == ViewMode.Expand)
            {
                int name_height = GetTemplateY();
                if (y < name_height)
                {
                    var ret = _name.CaretMoveUp(x - x0,
                        y - y0,
                        out HitInfo sub_info);
                    info = sub_info.Clone();
                    info.X += x0;
                    info.Y += y0;
                    info.ChildIndex = (int)FieldRegion.Content;
                    // 保持 info.LineHeight
                    info.Box = this;
                    info.InnerHitInfo = sub_info;
                    return ret;
                }
                else
                {
                    var ret = _template.CaretMoveUp(x - x0,
                        y - name_height,
                        out HitInfo sub_info);
                    if (ret == true)
                    {
                        info = sub_info.Clone();
                        info.X += x0;
                        info.Y += name_height;
                        info.Offs += 2;
                        info.ChildIndex = (int)FieldRegion.Content;
                        // 保持 info.LineHeight
                        info.Box = this;
                        info.InnerHitInfo = sub_info;
                        return ret;
                    }
                    info = HitTest(x - x0,
    name_height - 1);
                    return true;
                }
            }

            info = new HitInfo { Box = this };
            return false;
        }

        public void Clear()
        {
            _caption?.Clear();

            _content?.Clear();

            _name?.Clear();
            _template?.Clear();
        }

        public void ClearCache()
        {
            _caption?.ClearCache();

            _content?.ClearCache();

            _name?.ClearCache();
            _template?.ClearCache();
        }

        public void Dispose()
        {
            _caption?.Dispose();
            _caption = null;

            _content?.Dispose();
            _content = null;

            _name?.Dispose();
            _name = null;

            _template?.Dispose();
            _template = null;
        }

        public int GetPixelHeight()
        {
            int height1 = 0;
            int height2 = 0;
            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
            {
                if (_content == null)
                    return 0;
                height1 = _content.GetPixelHeight();
            }
            if (_viewMode == ViewMode.Expand)
            {
                // EnsureNameAndTemplate();
                height2 = (_name?.GetPixelHeight() ?? 0) + (_template?.GetPixelHeight() ?? 0);
            }
            return height1 + height2;
        }

        public int GetPixelWidth()
        {
            var x0 = GetContentX();

            int width1 = 0;
            int width2 = 0;
            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
            {
                if (_content == null)
                    return 0;
                width1 = _content.GetPixelWidth();
            }
            if (_viewMode == ViewMode.Expand)
            {
                // EnsureNameAndTemplate();
                width2 = Math.Max(_name?.GetPixelWidth() ?? 0, _template?.GetPixelWidth() ?? 0);
            }

            return x0 + Math.Max(width1, width2);
        }

        public Region GetRegion(int start_offs = 0, int end_offs = int.MaxValue, int virtual_tail_length = 0)
        {
            Debug.Assert(_viewMode != ViewMode.None);

            var x0 = GetContentX();
            var y0 = GetContentY();
            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
            {
                if (_content == null)
                {
                    return null;
                }

                var region = _content.GetRegion(start_offs, end_offs, virtual_tail_length);
                region?.Offset(x0, y0);
                return region;
            }
            else if (_viewMode == ViewMode.Expand)
            {
                if (_name != null && _template != null)
                {
                    int name_length = _name.TextLength;
                    Region region1 = null;
                    Region region2 = null;
                    if (start_offs < name_length)
                    {
                        region1 = _name.GetRegion(start_offs,
                            Math.Min(end_offs, name_length),
                            virtual_tail_length);
                        region1?.Offset(x0, y0);
                    }
                    if (end_offs > name_length)
                    {
                        region2 = _template.GetRegion(Math.Max(0, start_offs - name_length),
                            end_offs - name_length,
                            virtual_tail_length);
                        region2?.Offset(x0 + 0, y0 + GetTemplateY());
                    }
                    if (region1 == null)
                    {
                        return region2;
                    }

                    if (region2 == null)
                    {
                        return region1;
                    }

                    region1.Union(region2);
                    region2?.Dispose();
                    return region1;
                }
            }

            return null;
        }

        public HitInfo HitTest(int x, int y)
        {
            Debug.Assert(_viewMode != ViewMode.None);

            var x0 = GetContentX();
            var y0 = GetContentY();
            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
            {
                if (x < x0)
                {
                    var button_rect = GetButtonRect();
                    if (Utility.PtInRect(new Point(x, y), button_rect))
                    {
                        return new HitInfo
                        {
                            ChildIndex = (int)FieldRegion.Button,
                            Box = this
                        };
                    }
                    else if (x > button_rect.Left - Metrics?.SplitterPixelWidth)
                    {
                        return new HitInfo
                        {
                            ChildIndex = (int)FieldRegion.Splitter,
                            Box = this
                        };
                    }
                }

                if (_content == null)
                    return new HitInfo { Box = this };
                var sub_info = _content?.HitTest(x - x0,
                    y - y0) ?? null;
                var info = sub_info.Clone();
                info.X += x0;
                info.Y += y0;
                info.ChildIndex = (int)FieldRegion.Content;
                info.Box = this;
                info.InnerHitInfo = sub_info;
                return info;
            }
            else if (_viewMode == ViewMode.Expand)
            {
                var caption_pixel = Metrics?.GetCaptionPixelWidth(this) ?? 0;
                int caption_area_hitted = 0;
                // 点击到了左边 Caption 区域
                if (x < caption_pixel - Metrics?.SplitterPixelWidth)
                {
                    caption_area_hitted = (int)FieldRegion.Caption;
                }

                // 点击到了 Caption 区域和 Name 区域的缝隙位置
                else if (x < caption_pixel)
                {
                    caption_area_hitted = (int)FieldRegion.Splitter;    // -1 表示 caption 和 name 之间的缝隙
                }

                // 点击到了字段名左边的按钮
                else if (x < caption_pixel + Metrics?.ButtonWidth)
                {
                    var button_rect = GetButtonRect();
                    if (Utility.PtInRect(new Point(x, y), button_rect))
                    {
                        caption_area_hitted = (int)FieldRegion.Button;
                    }
                    else if (x > button_rect.Left - Metrics?.SplitterPixelWidth)
                    {
                        caption_area_hitted = (int)FieldRegion.Splitter;
                    }
                }

                if (_name != null && _template != null)
                {
                    int name_height = GetTemplateY();
                    if (y - y0 < name_height)
                    {
                        var sub_info = _name.HitTest(x - x0,
                            y - y0);
                        var info = sub_info.Clone();
                        info.X += x0;
                        info.Y += y0;
                        info.ChildIndex = caption_area_hitted != 0 ? caption_area_hitted : (int)FieldRegion.Content;
                        info.Box = this;
                        info.InnerHitInfo = sub_info;
                        return info;
                    }
                    else
                    {
                        var sub_info = _template.HitTest(x - x0,
                            y - name_height);
                        var info = sub_info.Clone();
                        info.X += x0;
                        info.Y += y0 + name_height;
                        info.Offs += 2;
                        info.ChildIndex = caption_area_hitted != 0 ? caption_area_hitted : (int)FieldRegion.Content;
                        info.Box = this;
                        info.InnerHitInfo = sub_info;
                        return info;
                    }
                }
            }

            return null;
        }

        // 注: _viewMode 和三个组件之间的对应关系可能扭曲。只能说确保 _content 和 _name+_tamplte 两组组件之间只有一组不为 null 即可
        public string MergeText(int start = 0, int end = int.MaxValue)
        {
            if (_content != null)
                return _content.MergeText(start, end);
            string name_fragment = _name?.MergeText(start, end) ?? "";
            var name_text_length = _name?.TextLength ?? 0;
            // 注意 name_fragment 可能只是 _name 文字中一部分，其长度可能比整个长度要短
            string template_fragment = _template?.MergeText(start - name_text_length, end - name_text_length) ?? "";
            return name_fragment + template_fragment;
            /*
            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
            {
                if (_content == null)
                {
                    Debug.Assert(false);
                    return "";
                }
                return _content.MergeText(start, end);
            }
            else if (_viewMode == ViewMode.Expand)
            {
                Debug.Assert(_name != null);
                Debug.Assert(_template != null);
                // EnsureNameAndTemplate();
                string name_text = _name?.MergeText(start, end) ?? "";
                string template_text = _template?.MergeText(start - name_text.Length, end - name_text.Length) ?? "";
                return name_text + template_text;
            }
            return "";
            */
        }

        public virtual string MergeTextMask(int start = 0, int end = int.MaxValue)
        {
            if (_content != null)
                return _content.MergeText(start, end);

            // name 中的字符都是允许删除的
            string name_fragment = _name?.MergeText(start, end) ?? "";
            var name_text_length = _name?.TextLength ?? 0;
            // 注意 name_fragment 可能只是 _name 文字中一部分，其长度可能比整个长度要短

            // 模板中可能存在不允许删除的区段
            string template_fragment = _template?.MergeTextMask(start - name_text_length, end - name_text_length) ?? "";
            return name_fragment + template_fragment;
        }


        // parameters:
        //      offs    插入符在当前对象中的偏移
        //      direction   -1 向左 0 原地 1 向右
        // return:
        //      -1  越过左边
        //      0   成功
        //      1   越过右边
        public int MoveByOffs(int offs, int direction, out HitInfo info)
        {
            Debug.Assert(_viewMode != ViewMode.None);

            var x0 = GetContentX();
            var y0 = GetContentY();
            // 疑问: 如果两个视图模式都启用了怎么办？插入符只能在其中一个上面
            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
            {
                if (_content != null)
                {
                    var ret = _content.MoveByOffs(offs, direction, out HitInfo sub_info);
                    info = sub_info.Clone();
                    info.X += x0;
                    info.Y += y0;
                    info.ChildIndex = (int)FieldRegion.Content;
                    // 保持 info.LineHeight
                    info.Box = this;
                    info.InnerHitInfo = sub_info;
                    return ret;
                }
            }
            else if (_viewMode == ViewMode.Expand)
            {
                if (_name != null && _template != null)
                {
                    int name_length = _name.TextLength;
                    if (offs + direction < name_length)
                    {
                        var ret = _name.MoveByOffs(offs, direction, out HitInfo sub_info);
                        info = sub_info.Clone();
                        info.X += x0;
                        info.Y += y0;
                        info.ChildIndex = (int)FieldRegion.Content;
                        // 保持 info.LineHeight
                        info.Box = this;
                        info.InnerHitInfo = sub_info;
                        return ret;
                    }
                    else
                    {
                        var ret = _template.MoveByOffs(offs - name_length, direction, out HitInfo sub_info);
                        // info.Offs += name_length;
                        info = sub_info.Clone();
                        info.X += x0;
                        info.Y += GetTemplateY();
                        info.Offs += name_length;
                        info.ChildIndex = (int)FieldRegion.Content;
                        // 保持 info.LineHeight
                        info.Box = this;
                        info.InnerHitInfo = sub_info;
                        return ret;
                    }
                }
            }

            info = new HitInfo { Box = this };
            return 0;
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
            var x0 = GetContentX();
            var y0 = GetContentY();

            // 绘制提示文字
            TemplateItem.PaintCaption(
                context,
                dc,
                x,
                y + y0,
                clipRect,
                _caption,
                Metrics);

            // 绘制 展开/收缩 按钮
            if (this._viewMode != ViewMode.Plane)
            {
                var rect = GetButtonRect(x, y);
                if (clipRect.IntersectsWith(rect))
                {
                    // FontContext.PaintExpandButton(dc, rect.X, rect.Y, this._viewMode == ViewMode.Table);
                    FontContext.DrawExpandIcon(dc, rect, 2,
                        Metrics?.BorderColor ?? Color.White,
                        this._viewMode == ViewMode.Expand);
                }
            }

            int y_offs = 0;
            if ((_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
                && _content != null)
            {
                _content.Paint(context,
                    dc,
                    x + x0,
                    y + y0,
                    clipRect,
                    blockOffs1,
                    blockOffs2,
                    virtual_tail_length);
                // y_offs += _content.GetPixelHeight();
            }

            if (_viewMode == ViewMode.Expand
                && _name != null && _template != null)
            {
                _name.Paint(context,
dc,
x + x0,
y + y0 + y_offs,
clipRect,
blockOffs1,
blockOffs2,
virtual_tail_length);
                _template.Paint(context,
    dc,
    x + x0,
    y + GetTemplateY() + y_offs,
    clipRect,
    blockOffs1 - 2,
    blockOffs2 - 2,
    virtual_tail_length);
            }
        }

        public ReplaceTextResult ReplaceText(IContext context,
    Gdi32.SafeHDC dc,
    int start,
    int end,
    string content,
    int pixel_width)
        {
            throw new NotFiniteNumberException();
        }

        public string SubfieldName
        {
            get
            {
                var name = GetSubfieldName();
                return NormalizeName(name);
            }
        }

        public static string NormalizeName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "";
            if (name[0] != Metrics.SubfieldCharDefault)
                return "";
            if (name.Length < 2)
                return "";
            return name.Substring(1, 1);
        }

        public string GetSubfieldName()
        {
            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
                return _content?.MergeText(0, 2) ?? "";
            if (_viewMode == ViewMode.Expand)
                return _name?.MergeText(0, 2);
            return "";
        }

        bool EnsureContent(string name)
        {
            bool NewParagraph()
            {
                _name?.Dispose();
                _name = null;
                _template?.Dispose();
                _template = null;
                if (_content == null || !(_content is Paragraph))
                {
                    _content?.Dispose();
                    _content = new Paragraph(this/*, _fieldProperty*/)
                    {
                        Name = "!content",
                    };
                    return true;
                }
                return false;
            }

            bool NewNameAndTemplate(UnitInfo info)
            {
                bool changed = false;
                if (_name == null)
                {
                    _name?.Dispose();
                    _name = new FixedLine(this)
                    {
                        Name = "!name",
                    };
                    changed = true;
                }
                if (_template == null)
                {
                    _template?.Dispose();
                    _template = new Template(this, Metrics)
                    {
                        Name = "!template",
                        StructureInfo = info,
                    };
                    changed = true;
                }

                _content?.Dispose();
                _content = null;
                return changed;
            }

            // 应要求，不支持展开。或者暂时为收缩状态
            if (_viewMode == ViewMode.Plane || _viewMode == ViewMode.Collapse)
            {
                return NewParagraph();
            }

            // TODO: 结构定义可以考虑缓存。这样反复收缩/展开时就不用重新查询定义了
            // 查询结构定义

            // 查询之前对 name 进行修整
            name = NormalizeName(name);
            var struct_info = Metrics.GetStructure?.Invoke(this.Parent?.Parent, name, 2);
            // 无法获得结构定义，就用 Paragraph
            if (struct_info == null
                || struct_info.SubUnits.Count == 0
                || struct_info.IsUnknown())
            {
                _viewMode = ViewMode.Plane;
                return NewParagraph();
            }

            if (_viewMode == ViewMode.Expand)
            {
                if (struct_info.IsChars())
                {
                    return NewNameAndTemplate(struct_info);
                }
                else if (struct_info.IsSubfield())
                {
                    throw new ArgumentException("Subfield 之下不应出现 Subfield 子结构");
                }
                else if (struct_info.IsField())
                {
                    throw new ArgumentException("Subfield 之下不应出现 Field 子结构");
                }
            }

            if (_viewMode == ViewMode.None)
            {
                _viewMode = ViewMode.Collapse;
            }

            return NewParagraph();
        }

        void EnsureCaption()
        {
            if (_caption == null)
                _caption = new Line(this)
                {
                    Name = "!caption",
                    TextAlign = TextAlign.None
                };
        }

        internal string _initialCaptionText = null;

        public ReplaceTextResult ReplaceText(
            ViewModeTree view_mode_tree,
            IContext context,
            Gdi32.SafeHDC dc,
            int start,
            int end,
            string content,
            int pixel_width)
        {
            ReplaceTextResult ret1 = null;
            ReplaceTextResult ret2 = null;
            string new_text = "";
            string old_text = "";

            if (end != -1 && start > end)
            {
                throw new ArgumentException($"start ({start}) 必须小于 end ({end})");
            }

            old_text = this.MergeText();
            if (end == -1)
                end = old_text.Length;

            new_text = old_text.Substring(0, start) + content + old_text.Substring(end);
            string name = new_text.Substring(0, Math.Min(2, new_text.Length));

            if (view_mode_tree != null)
                this._viewMode = view_mode_tree.ViewMode;
            EnsureContent(name);
            Debug.Assert(_viewMode != ViewMode.None);

            var x0 = GetContentX();
            var y0 = GetContentY();

            var button_rect = GetButtonRect(0, 0);

            if (_caption == null)
            {
                EnsureCaption();

                var ret = _caption.ReplaceText(context,
                    dc,
                    0,
                    -1,
                    _initialCaptionText,
                    int.MaxValue);
                var caption_update_rect = ret.UpdateRect;
                var rect = TemplateItem.GetCaptionRect(_caption, 0, 0, Metrics);

                if (caption_update_rect != System.Drawing.Rectangle.Empty)
                    caption_update_rect.Offset(rect.X, rect.Y);
                button_rect = Utility.Union(button_rect, caption_update_rect);
            }


            // TODO: 是否要规定一个最小的宽度?
            pixel_width = Math.Max(0, pixel_width - x0);

            if (_viewMode == ViewMode.Plane
                || _viewMode == ViewMode.Collapse)
            {
                ret1 = _content.ReplaceText(context,
                    dc,
                    start,
                    end,
                    content,
                    pixel_width);
            }
            if (_viewMode == ViewMode.Expand)
            {
                if (start < 0 || end < -1)
                {
                    throw new ArgumentException($"start ({start}) 或 end ({end}) 不合法");
                }

                // 确保 start 是较小的一个
                if (end != -1 && start > end)
                {
                    int temp = start;
                    start = end;
                    end = temp;
                }

                if (end == -1)
                {
                    end = Int32.MaxValue;
                }


                var name_ret = _name.ReplaceText(context,
                    dc,
                    0,
                    -1,
                    name,
                    pixel_width);
                var template_text = new_text.Length > 2 ? new_text.Substring(2) : "";
                Debug.Assert(_template.StructureInfo != null);
                var template_ret = _template.ReplaceText(
                    view_mode_tree,
                    context,
                    dc,
                    0,
                    -1,
                    template_text,
                    pixel_width);
                var update_rect = new Rectangle(0,
                    0,
                    Math.Max(_name.GetPixelWidth(), _template.GetPixelWidth()),
                    _name.GetPixelHeight() + _template.GetPixelHeight());
                update_rect = Utility.Union(update_rect, button_rect);
                ret2 = new ReplaceTextResult
                {
                    MaxPixel = Math.Max(name_ret.MaxPixel, template_ret.MaxPixel),
                    NewText = new_text.Substring(start, content.Length),
                    ReplacedText = old_text.Substring(start, end - start),
                    UpdateRect = update_rect,
                    ScrollRect = Rectangle.Empty,
                    ScrolledDistance = 0,
                };
            }
            if (ret1 == null && ret2 == null)
            {
                return new ReplaceTextResult();
            }

            if (ret1 != null && ret2 != null)
            {
                // ret1 在上 ret2 在下
                var name_height = GetTemplateY();
                var rect2 = ret2.UpdateRect;
                rect2.Offset(0, name_height);
                var update_rect = Utility.Union(ret1.UpdateRect, rect2);
                update_rect = Utility.Union(update_rect, button_rect);
                var max_pixel_width = Math.Max(ret1.MaxPixel, ret2.MaxPixel);
                ret2 = new ReplaceTextResult
                {
                    MaxPixel = max_pixel_width,
                    NewText = new_text.Substring(start, content.Length),
                    ReplacedText = old_text.Substring(start, end - start),
                    UpdateRect = update_rect,
                    ScrollRect = Rectangle.Empty,
                    ScrolledDistance = 0,
                };
            }
            if (ret1 != null)
            {
                ret1.UpdateRect = Utility.Offset(ret1.UpdateRect, x0, y0);
                ret1.UpdateRect = Utility.Union(ret1.UpdateRect, button_rect);

                ret1.ScrollRect = Utility.Offset(ret1.ScrollRect, x0, y0);
                ret1.MaxPixel += x0;
                return ret1;
            }

            if (ret2 != null)
            {
                ret2.UpdateRect = Utility.Offset(ret2.UpdateRect, x0, y0);
                ret2.UpdateRect = Utility.Union(ret2.UpdateRect, button_rect);

                ret2.ScrollRect = Utility.Offset(ret2.ScrollRect, x0, y0);
                ret2.MaxPixel += x0;
                return ret2;
            }

            return new ReplaceTextResult();
        }

#if REMOVED
        void EnsureContent()
        {
            if (_content == null)
            {
                _content = new Paragraph(this);
            }
        }

        void EnsureNameAndTemplate()
        {
            if (_name == null)
                _name = new Line(this);
            if (_template == null)
                _template = new Template(this, Metrics);
        }
#endif

#if REMOVED
        // 改变显示模式
        public ReplaceTextResult ChangeViewMode(IContext context,
            Gdi32.SafeHDC dc,
            ViewMode mode,
            int active_mode,
            int pixel_width)
        {
            if (_viewMode == mode)
            {
                return new ReplaceTextResult();
            }
            _viewMode = mode;
            if (active_mode < 0 || active_mode > 1)
                throw new ArgumentException($"active_mode 参数值 {active_mode} 不合法。应为 0 或 1");
            _active_mode = active_mode;
            return this.ReplaceText(context,
                dc,
                0,
                this.TextLength,
                this.MergeText(0, this.TextLength),
                pixel_width);
        }
#endif

        public ViewModeTree GetViewModeTree()
        {
            // 只要当前对象不是展开的状态，则没有必要再递归获得下级的状态
            if (this._viewMode != ViewMode.Expand)
            {
                return null;
            }

            Debug.Assert(_template != null);
            if (_template != null)
            {
                var result = _template.GetViewModeTree();
                Debug.Assert(result.ViewMode == ViewMode.Expand);
                Debug.Assert(this._viewMode == ViewMode.Expand);
                result.ViewMode = this._viewMode;
                result.Name = "!subfield";
                return result;
            }
            return null;
            // return new ViewModeTree { ViewMode = this._viewMode != ViewMode.Expand ? ViewMode.None : this._viewMode };
        }

        public ReplaceTextResult ToggleExpand(
            HitInfo info,
            IContext context,
            Gdi32.SafeHDC dc,
            int pixel_width)
        {
            ReplaceTextResult ret1 = null;
            ReplaceTextResult ret2 = null;

            if (info.ChildIndex == (int)FieldRegion.Button)
            {
                if (this._viewMode == ViewMode.Plane)
                    return new ReplaceTextResult();

                var text = this.MergeText();
                var new_view_mode = this._viewMode == ViewMode.Collapse ? ViewMode.Expand : ViewMode.Collapse;
                // 此时 _content _name _template 和 this._viewMode 关系暂时扭曲了
                return ReplaceText(
                    new ViewModeTree { ViewMode = new_view_mode },
                    context,
                    dc,
                    0,
                    -1,
                    text,
                    pixel_width);
            }

            return new ReplaceTextResult();
        }

    }

    public enum ViewMode
    {
        None = 0,   // 尚未决定。需要查询结构定义，然后决定是 Plane 还是 Collapse Expand
        Plane = 1,   // 平面。表示不允许展开。
        Collapse = 2,   // 收缩。表示暂时收缩，未来可能展开
        Expand = 3,   // 展开
    }
}
