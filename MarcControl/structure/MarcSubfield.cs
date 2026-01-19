using System;
using System.Drawing;

using Vanara.PInvoke;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// MARC 子字段
    /// 有两种模式: 平面、表格
    /// 表格模式下，如果 _name 内容为空，表示这是一个字段的指示符后面到第一个子字段之间的文本。这部分文本没有子字段名 
    /// </summary>
    public class MarcSubfield : IBox, IDisposable
    {
        Paragraph _content = null;

        Line _name = null;
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

        ViewMode _viewMode = ViewMode.Plane;
        public ViewMode ViewMode
        {
            get
            {
                return _viewMode;
            }
        }

        // 当前激活的视图模式
        int _active_mode = 0;

        public string Name { get; set; }

        // 子字段节点的父节点可能是一个 MarcRecord 节点，也可能是一个 MarcField 节点(比如 UNIMARC 4XX 情形)
        public IBox Parent { get; set; }


        public int TextLength
        {
            get
            {
                if (_viewMode.HasFlag(ViewMode.Plane))
                {
                    return _content?.TextLength ?? 0;
                }
                if (_viewMode.HasFlag(ViewMode.Table))
                {
                    EnsureNameAndTemplate();
                    return (_name?.TextLength ?? 0) + (_template?.TextLength ?? 0);
                }
                return 0;
            }
        }

        public float BaseLine => _content?.BaseLine ?? 0;

        public float Below => _content?.Below ?? 0;

        public bool CaretMoveDown(int x, int y, out HitInfo info)
        {
            if (_active_mode == 0)
            return _content.CaretMoveDown(x, y, out info);
            if (_active_mode == 1)
            {
                int name_height = _name.GetPixelHeight();
                if (y < name_height)
                {
                    return _name.CaretMoveDown(x, y, out info);
                }
                else
                {
                    var ret = _template.CaretMoveDown(x, y - name_height, out info);
                    if (info != null)
                    {
                        info.Y += name_height;
                        info.Offs += 2;
                    }
                    return ret;
                }
            }

            info = new HitInfo();
            return false;
        }

        public bool CaretMoveUp(int x, int y, out HitInfo info)
        {
            if (_active_mode == 0)
                return _content.CaretMoveUp(x, y, out info);
            if (_active_mode == 1)
            {
                int name_height = _name.GetPixelHeight();
                if (y < name_height)
                {
                    return _name.CaretMoveUp(x, y, out info);
                }
                else
                {
                    var ret = _template.CaretMoveUp(x, y - name_height, out info);
                    if (info != null)
                    {
                        info.Y += name_height;
                        info.Offs += 2;
                    }
                    return ret;
                }
            }

            info = new HitInfo();
            return false;
        }

        public void Clear()
        {
            _content?.Clear();

            _name?.Clear();
            _template?.Clear();
        }

        public void ClearCache()
        {
            _content?.ClearCache();

            _name?.ClearCache();
            _template?.ClearCache();
        }

        public void Dispose()
        {
            _content?.Dispose();

            _name?.Dispose();
            _template?.Dispose();
        }

        public int GetPixelHeight()
        {
            int height1 = 0;
            int height2 = 0;
            if (_viewMode.HasFlag(ViewMode.Plane))
            {
                if (_content == null)
                    return 0;
                height1 = _content.GetPixelHeight();
            }
            if (_viewMode.HasFlag(ViewMode.Table))
            {
                EnsureNameAndTemplate();
                height2 = _name.GetPixelHeight() + _template.GetPixelHeight();
            }
            return height1 + height2;
        }

        public int GetPixelWidth()
        {
            int width1 = 0;
            int width2 = 0;
            if (_viewMode.HasFlag(ViewMode.Plane))
            {
                if (_content == null)
                    return 0;
                width1 = _content.GetPixelWidth();
            }
            if (_viewMode.HasFlag(ViewMode.Table))
            {
                EnsureNameAndTemplate();
                width2 = Math.Max(_name.GetPixelWidth(), _template.GetPixelWidth());
            }

            return Math.Max(width1, width2);
        }

        public Region GetRegion(int start_offs = 0, int end_offs = int.MaxValue, int virtual_tail_length = 0)
        {
            if (_active_mode == 0)
            {
                if (_content == null)
                    return null;
                return _content.GetRegion(start_offs, end_offs, virtual_tail_length);
            }
            else if (_active_mode == 1)
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
                    }
                    if (end_offs > name_length)
                    {
                        region2 = _template.GetRegion(Math.Max(0, start_offs - name_length),
                            end_offs - name_length,
                            virtual_tail_length);
                        if (region2 != null)
                        {
                            var matrix = new System.Drawing.Drawing2D.Matrix();
                            matrix.Translate(0, _name.GetPixelHeight());
                            region2.Transform(matrix);
                        }
                    }
                    if (region1 == null)
                        return region2;
                    if (region2 == null)
                        return region1;
                    region1.Union(region2);
                    region2?.Dispose();
                    return region1;
                }
            }

            return null;
        }

        public HitInfo HitTest(int x, int y)
        {
            if (_active_mode == 0)
            {
                return _content?.HitTest(x, y) ?? null;
            }
            else if (_active_mode == 1)
            {
                if (_name != null && _template != null)
                {
                    int name_height = _name.GetPixelHeight();
                    if (y < name_height)
                    {
                        return _name.HitTest(x, y);
                    }
                    else
                    {
                        var info = _template.HitTest(x, y - name_height);
                        if (info != null)
                        {
                            info.Offs += 2;
                            info.Y += name_height;
                        }
                        return info;
                    }
                }
            }

            return null;
        }

        public string MergeText(int start = 0, int end = int.MaxValue)
        {
            if (_viewMode.HasFlag(ViewMode.Plane))
            {
                if (_content == null)
                    return "";
                return _content.MergeText(start, end);
            }
            else if (_viewMode.HasFlag(ViewMode.Table))
            {
                EnsureNameAndTemplate();
                string name_text = _name.MergeText(0, 2);
                string template_text = _template.MergeText(2, end - 2);
                return name_text + template_text;
            }
            return "";
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
            // 疑问: 如果两个视图模式都启用了怎么办？插入符只能在其中一个上面
            if (_active_mode == 0)
            {
                if (_content != null)
                    return _content.MoveByOffs(offs, direction, out info);
            }
            else if (_active_mode == 1)
            {
                if (_name != null && _template != null)
                {
                    int name_length = _name.TextLength;
                    if (offs + direction < name_length)
                    {
                        return _name.MoveByOffs(offs, direction, out info);
                    }
                    else
                    {
                        var ret = _template.MoveByOffs(offs - name_length, direction, out info);
                        info.Offs += name_length;
                        return ret;
                    }
                }
            }

            info = new HitInfo();
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
            int y_offs = 0;
            if (_viewMode.HasFlag(ViewMode.Plane)
                && _content != null)
            {
                _content.Paint(context,
                    dc,
                    x,
                    y,
                    clipRect,
                    blockOffs1,
                    blockOffs2,
                    virtual_tail_length);
                y_offs += _content.GetPixelHeight();
            }
            if (_viewMode.HasFlag(ViewMode.Table)
                && _name != null && _template != null)
            {
                _name.Paint(context,
dc,
x,
y + y_offs,
clipRect,
blockOffs1,
blockOffs2,
virtual_tail_length);
                _template.Paint(context,
    dc,
    x,
    y + y_offs + _name.GetPixelHeight(),
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
            ReplaceTextResult ret1 = null;
            ReplaceTextResult ret2 = null;
            string new_text = "";
            string old_text = "";
            if (_viewMode.HasFlag(ViewMode.Plane))
            {
                EnsureContent();
                ret1 = _content.ReplaceText(context,
                    dc,
                    start,
                    end,
                    content,
                    pixel_width);
            }
            if (_viewMode.HasFlag(ViewMode.Table))
            {
                if (start < 0 || end < -1)
                {
                    throw new ArgumentException($"start ({start}) 或 end ({end}) 不合法");
                }

                EnsureNameAndTemplate();

                // 确保 start 是较小的一个
                if (end != -1 && start > end)
                {
                    int temp = start;
                    start = end;
                    end = temp;
                }

                if (end == -1)
                    end = Int32.MaxValue;

                old_text = this.MergeText();
                new_text = old_text.Substring(0, start) + content + old_text.Substring(end);

                var name = new_text.Substring(0, Math.Min(2, new_text.Length));
                var name_ret = _name.ReplaceText(context,
                    dc,
                    0,
                    -1,
                    name,
                    pixel_width);
                var template_text = new_text.Length > 2 ? new_text.Substring(2) : "";
                var template_ret = _template.ReplaceText(context,
                    dc,
                    0,
                    -1,
                    template_text,
                    pixel_width);
                var update_rect = new Rectangle(0,
                    0,
                    Math.Max(_name.GetPixelWidth(), _template.GetPixelWidth()),
                    _name.GetPixelHeight() + _template.GetPixelHeight());
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
                var name_height = _name.GetPixelHeight();
                var rect2 = ret2.UpdateRect;
                rect2.Offset(0, name_height);
                var update_rect = Utility.Union(ret1.UpdateRect, rect2);
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
                return ret1;
            }

            if (ret2 != null)
            {
                return ret2;
            }

            return new ReplaceTextResult();
        }

        void EnsureContent()
        {
            if (_content == null)
                _content = new Paragraph(this);
        }

        void EnsureNameAndTemplate()
        {
            if (_name == null)
                _name = new Line(this);
            if (_template == null)
                _template = new Template(this, Metrics);
        }

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
    }

    [Flags]
    public enum ViewMode
    {
        Plane = 0x01,   // 平面
        Table = 0x02,   // 表格
        Dual = 0x01 | 0x02,
    }
}
