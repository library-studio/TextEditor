using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Vanara.PInvoke;

namespace LibraryStudio.Forms
{
    public class TemplateItem : IViewBox
    {
        Line _caption = null;

        Paragraph _content = null;

        public Metrics Metrics { get; set; }

        public TemplateItem()
        {

        }

        public TemplateItem(IBox parent, Metrics metrics)
        {
            Parent = parent;
            Metrics = metrics;
        }

        public string Name { get; set; }
        public IBox Parent { get; set; }

        public int TextLength => _content?.TextLength ?? 0;

        public float BaseLine => _content?.BaseLine ?? 0;

        public float Below => _content?.Below ?? 0;

        ViewMode _viewMode = ViewMode.Plane;    // 表示不可能展开
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

        /*
        string _initialCaptionText = null;
        public string CaptionText
        {
            get
            {
                return _caption?.MergeText();
            }
            set
            {
                _initialCaptionText = value;
            }
        }
        */

        Rectangle GetButtonRect(int x = 0, int y = 0)
        {
            var height = _content?.GetPixelHeight() ?? 0;
            if (height == 0)
            {
                height = FontContext.DefaultFontHeight;
            }
            return new Rectangle(x,
    y,
    Metrics?.ButtonWidth ?? FontContext.DefaultReturnWidth,
    height);
        }

        int GetContentX(int x0 = 0)
        {
            Debug.Assert(Metrics != null);
            int button_width = Metrics?.ButtonWidth ?? 0;
            int caption_width = Metrics?.CaptionPixelWidth ?? 0;
            return x0 + caption_width + button_width;
        }

        int GetContentY(int y0 = 0)
        {
            return y0;
        }

        public bool CaretMoveDown(int x, int y, out HitInfo info)
        {
            var x0 = GetContentX();
            var y0 = GetContentY();

            var ret = _content.CaretMoveDown(x - x0, y - y0, out HitInfo sub_info);
            info = sub_info.Clone();
            info.X += x0;
            info.Y += y0;
            info.ChildIndex = (int)FieldRegion.Content;
            // 保持 info.LineHeight
            info.InnerHitInfo = sub_info;
            return ret;

        }

        public bool CaretMoveUp(int x, int y, out HitInfo info)
        {
            var x0 = GetContentX();
            var y0 = GetContentY();

            var ret = _content.CaretMoveUp(x - x0, y - y0, out HitInfo sub_info);
            info = sub_info.Clone();
            info.X += x0;
            info.Y += y0;
            info.ChildIndex = (int)FieldRegion.Content;
            // 保持 info.LineHeight
            info.InnerHitInfo = sub_info;
            return ret;
        }


        public void Clear()
        {
            _caption?.Clear();

            _content?.Clear();
        }

        public void ClearCache()
        {
            _caption?.ClearCache();

            _content?.ClearCache();
        }

        public void Dispose()
        {
            _caption?.Dispose();

            _content?.Dispose();
        }

        public int GetPixelHeight()
        {
            return _content?.GetPixelHeight() ?? 0;
        }

        public int GetPixelWidth()
        {
            return GetContentX() + _content?.GetPixelWidth() ?? 0;
        }

        public Region GetRegion(int start_offs = 0, int end_offs = int.MaxValue, int virtual_tail_length = 0)
        {
            var x0 = GetContentX();
            var y0 = GetContentY();
            if (_content == null)
            {
                return null;
            }

            var region = _content.GetRegion(start_offs, end_offs, virtual_tail_length);
            region?.Offset(x0, y0);
            return region;
        }


        public ViewModeTree GetViewModeTree()
        {
            return null;
        }

        public HitInfo HitTest(int x, int y)
        {
            var x0 = GetContentX();
            var y0 = GetContentY();
            if (x < x0)
                return new HitInfo { ChildIndex = (int)FieldRegion.Button };
            if (_content == null)
                return new HitInfo();
            var sub_info = _content.HitTest(x - x0, y - y0);
            var info = sub_info.Clone();
            info.X += x0;
            info.Y += y0;
            info.ChildIndex = (int)FieldRegion.Content;
            info.InnerHitInfo = sub_info;
            return info;
        }

        public string MergeText(int start = 0, int end = int.MaxValue)
        {
            if (_content == null)
                return "";
            return _content.MergeText(start, end);
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
            var x0 = GetContentX();
            var y0 = GetContentY();

            if (_content != null)
            {
                var ret = _content.MoveByOffs(offs, direction, out HitInfo sub_info);
                info = sub_info.Clone();
                info.X += x0;
                info.Y += y0;
                info.ChildIndex = (int)FieldRegion.Content;
                // 保持 info.LineHeight
                info.InnerHitInfo = sub_info;
                return ret;
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
            var x0 = GetContentX();
            var y0 = GetContentY();

            // 绘制提示文字
            if (_caption != null)
            {
                var rect = new Rectangle(x,
    y + y0,
    Math.Max(0, (Metrics?.CaptionPixelWidth ?? 0) - (Metrics?.GapThickness ?? 0)),
    _caption?.GetPixelHeight() ?? 0);

                if (rect.Width > 0
    && clipRect.IntersectsWith(rect))
                {
                    var getforecolor = context.GetForeColor;
                    context.GetForeColor = (box, highlight) =>
                    {
                        return Metrics?.CaptionForeColor ?? Metrics.DefaultCaptionForeColor;
                    };
                    try
                    {
                        _caption.Paint(context,
    dc,
    x,
    y + y0,
    clipRect,
    0,
    0,
    0);
                    }
                    finally
                    {
                        context.GetForeColor = getforecolor;
                    }
                }
            }

            // 绘制展开/收缩按钮
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

            // 绘制内容
            if (_content != null)
            {
                _content.Paint(context,
                    dc,
                    x + x0,
                    y + y0,
                    clipRect,
                    blockOffs1,
                    blockOffs2,
                    virtual_tail_length);
            }
        }


        public ReplaceTextResult ReplaceText(IContext context, Gdi32.SafeHDC dc, int start, int end, string content, int pixel_width)
        {
            throw new NotImplementedException();
        }

        void EnsureCaption()
        {
            if (_caption == null)
                _caption = new Line(this)
                {
                    Name = "caption",
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
            if (end != -1 && start > end)
            {
                throw new ArgumentException($"start ({start}) 必须小于 end ({end})");
            }

            var x0 = GetContentX();
            var y0 = GetContentY();

            var update_rect = Rectangle.Empty;
            var scroll_rect = Rectangle.Empty;
            int scroll_instance = 0;
            int max_pixel_width = 0;
            string replaced = null;

            if (_caption == null)
            {
                EnsureCaption();

                var ret = _caption.ReplaceText(context,
                    dc,
                    0,
                    -1,
                    _initialCaptionText,
                    int.MaxValue);
                update_rect = ret.UpdateRect;
                if (update_rect != System.Drawing.Rectangle.Empty)
                    update_rect.Offset(0, y0);
            }


            pixel_width = Math.Max(0, pixel_width - x0);
            {
                EnsureContent();
                ReplaceTextResult ret1 = null;
                ret1 = _content.ReplaceText(context,
                    dc,
                    start,
                    end,
                    content,
                    pixel_width);
                var update_content_rect = ret1.UpdateRect;
                update_content_rect.Offset(x0, y0);
                update_rect = Utility.Union(update_rect, update_content_rect);

                scroll_rect = Utility.Offset(ret1.ScrollRect, x0, y0);
                scroll_instance = ret1.ScrolledDistance;

                max_pixel_width = ret1.MaxPixel + x0;
                replaced = ret1.ReplacedText;
            }

            var button_rect = GetButtonRect(0, 0);
            update_rect = Utility.Union(update_rect, button_rect);

            return new ReplaceTextResult
            {
                UpdateRect = update_rect,
                ScrolledDistance = scroll_instance,
                ScrollRect = scroll_rect,
                MaxPixel = max_pixel_width,
                ReplacedText = replaced
            };
        }

        void EnsureContent()
        {
            if (_content == null)
            {
                _content = new Paragraph(this);
            }
        }

        public ReplaceTextResult ToggleExpand(HitInfo info, IContext context, Gdi32.SafeHDC dc, int pixel_width)
        {
            return new ReplaceTextResult();
        }
    }
}
