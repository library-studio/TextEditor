using System;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
#if REMOVED
    // 在 MarcControl 类中增加字段、构造器设置、EnsureBuffer 与修改的 OnPaint/OnHandleDestroyed
    public partial class MarcControl : UserControl
    {
        // 双缓冲相关
        private readonly BufferedGraphicsContext _bufferedContext = BufferedGraphicsManager.Current;
        private BufferedGraphics _bufferedGraphics;

        public MarcControl()
        {
            // 保持已有初始化...
            // 启用控件画面优化（避免直接依赖 WinForms 自动双缓冲）
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw, true);
            this.DoubleBuffered = true;

            _record = new MarcRecord(this, _fieldProperty);

            #region 延迟刷新
            CreateTimer();
            #endregion

            InitializeComponent();

            _line_height = Line.InitialFonts(this.Font);

            _fieldProperty.Refresh();

            _dpiXY = DpiUtil.GetDpiXY(this);

            _context = GetDefaultContext();
        }

        // 确保或重新分配 BufferedGraphics
        private void EnsureBuffer()
        {
            var sz = this.ClientSize;
            if (sz.Width <= 0 || sz.Height <= 0)
                return;

            if (_bufferedGraphics != null)
            {
                var g = _bufferedGraphics.Graphics;
                // 如果现有缓冲已足够，则重用
                if ((int)g.VisibleClipBounds.Width >= sz.Width && (int)g.VisibleClipBounds.Height >= sz.Height)
                    return;
                _bufferedGraphics.Dispose();
                _bufferedGraphics = null;
            }

            try
            {
                // 使用 CreateGraphics 分配与当前控件兼容的缓冲
                _bufferedGraphics = _bufferedContext.Allocate(this.CreateGraphics(), new Rectangle(0, 0, Math.Max(1, sz.Width), Math.Max(1, sz.Height)));
            }
            catch
            {
                // 分配失败则保持 _bufferedGraphics 为 null，回退到原先方式
                _bufferedGraphics = null;
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            // 计算绘制偏移（与原逻辑保持一致）
            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;
            var clipRect = e.ClipRectangle;

            // 尝试使用 BufferedGraphics 绘制以减少闪烁
            EnsureBuffer();
            if (_bufferedGraphics != null)
            {
                var g = _bufferedGraphics.Graphics;

                // 使用 GDI+ 绘制背景和 caption 区域（在获取 HDC 之前完成）
                var backColor = this._context?.GetBackColor?.Invoke(null, false) ?? SystemColors.Window;
                using (var brush = new SolidBrush(backColor))
                {
                    g.FillRectangle(brush, clipRect);
                }

                var left_rect = new Rectangle(
                    x,
                    y,
                    _fieldProperty.CaptionPixelWidth,
                    this.AutoScrollMinSize.Height);
                if (clipRect.IntersectsWith(left_rect))
                {
                    using (var brush = new SolidBrush(_fieldProperty?.CaptionBackColor ?? Metrics.DefaultCaptionBackColor))
                    {
                        g.FillRectangle(brush, left_rect);
                    }
                }

                MarcField.PaintLeftRightBorder(g,
                    x + _fieldProperty.SolidX,
                    y + 0,
                    _fieldProperty.SolidPixelWidth,
                    this.AutoScrollMinSize.Height,
                    _fieldProperty.BorderThickness);

                // 现在把 Graphics 的 HDC 给底层 GDI 绘制使用
                IntPtr hdc = IntPtr.Zero;
                try
                {
                    hdc = g.GetHdc();
                    using (var dc = new SafeHDC(hdc))
                    {
                        var old_mode = Gdi32.SetBkMode(dc, Gdi32.BackgroundMode.TRANSPARENT);
                        try
                        {
                            Debug.Assert(_initialized == true);
                            _record.Paint(
                                _context,
                                dc,
                                x,
                                y,
                                clipRect,
                                _blockOffs1,
                                _blockOffs2,
                                0);
                        }
                        finally
                        {
                            // 还原 GDI 状态（如果需要）
                            Gdi32.SetBkMode(dc, old_mode);
                        }
                    }
                }
                finally
                {
                    if (hdc != IntPtr.Zero)
                        g.ReleaseHdc(hdc);
                }

                // 把缓冲区渲染到屏幕
                _bufferedGraphics.Render(e.Graphics);
                return;
            }

            // 无法使用缓冲（分配失败），回退到原有代码路径
            base.OnPaint(e);
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            Imm32.ImmReleaseContext(this.Handle, hIMC);

            // 销毁双缓冲资源
            if (_bufferedGraphics != null)
            {
                try { _bufferedGraphics.Dispose(); } catch { }
                _bufferedGraphics = null;
            }

            #region 延迟刷新
            DestroyTimer();
            #endregion

            base.OnHandleDestroyed(e);
        }

        protected override void OnResize(EventArgs e)
        {
            // 重新分配或调整缓冲区大小
            if (_bufferedGraphics != null)
            {
                // 下一次 OnPaint 会根据 ClientSize 调整缓冲
                _bufferedGraphics.Dispose();
                _bufferedGraphics = null;
            }

            if (_clientBoundsWidth == 0)
            {
                Relayout();
            }

            base.OnResize(e);
        }
    }
#endif
}
