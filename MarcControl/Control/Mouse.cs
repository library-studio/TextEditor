using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanara.PInvoke;
using static System.Net.Mime.MediaTypeNames;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrackBar;
using static Vanara.PInvoke.Gdi32;
using static Vanara.PInvoke.User32;
using static Vanara.PInvoke.User32.RAWINPUT;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 鼠标有关的功能
    /// </summary>
    public partial class MarcControl
    {
        private System.Windows.Forms.Timer _mouseTimer = null;

        // 开始监视插入符可见性，尽可能卷动内容让插入符可见
        void BeginMonitorCaretVisible()
        {
            if (_mouseTimer == null)
            {
                CreateMouseTimer();
            }

            if (_mouseTimer.Enabled == false)
            {
                _mouseTimer.Start();
            }
        }

        // 结束监视
        void EndMonitorCaretVisible()
        {
            DestroyMouseTimer();
            /*
            if (_mouseTimer != null
                && _mouseTimer?.Enabled == true)
            {
                _mouseTimer.Stop();
            }
            */
        }

        void CreateMouseTimer()
        {
            _mouseTimer = new System.Windows.Forms.Timer { Interval = 300 };
            // 尝试卷动内容，让 Caret 可见
            _mouseTimer.Tick += (s, e) => MonitorMouse();
        }

        void MonitorMouse()
        {
            var p = this.PointToClient(Control.MousePosition);
            if (PtInRect(p, this.ClientRectangle) == false)
            {
                OnMouseMove(new MouseEventArgs(MouseButtons.None, 0, p.X, p.Y, 0));
            }
        }

        public static bool PtInRect(Point p,
Rectangle rect)
        {
            return p.X >= rect.X
                && p.X < rect.Right
                && p.Y >= rect.Y
                && p.Y < rect.Bottom;
        }

        void DestroyMouseTimer()
        {
            if (_mouseTimer != null)
            {
                _mouseTimer.Stop();
                _mouseTimer.Tick -= (s, e) => MonitorMouse();
                _mouseTimer.Dispose();
                _mouseTimer = null;
            }
        }


        bool _isMouseDown = false;
        bool _isButtonDown = false;

        protected override void OnMouseDown(MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                {
                    ContextMenuStrip contextMenu = new ContextMenuStrip();
                    var commands = this.Commands;
                    AppendMenu(contextMenu.Items, commands);
                    contextMenu.Show(this, e.Location);
                }
                base.OnMouseDown(e);
                return;
            }
            this.Capture = true;
            this._isMouseDown = true;
            // ChangeCaretPos(e.X, e.Y);
            {
                var result = _record.HitTest(
                    e.X + this.HorizontalScroll.Value,
                    e.Y + this.VerticalScroll.Value);
                /*
                if ((result.Area & Area.LeftBlank) != 0)
                {

                }
                */


                // 按下了“展开/收缩”按钮
                if (HitButton(result))
                {
                    ToggleExpand(result);
                    base.OnMouseDown(e);
                    _isButtonDown = true;
                    return;
                }

                // 点击 Caption 区域
                var splitter_test = TestSplitterArea(e.X);
                if (splitter_test == -2)
                {
                    BeginFieldSelect(result.ChildIndex);
                    base.OnMouseDown(e);
                    return;
                }
                // 点击 Splitter
                if (splitter_test == -1)
                {
                    StartSplitting(e.X);
                    base.OnMouseDown(e);
                    return;
                }

                if (splitter_test >= 0
                    && IsCursorInsideSelectionRegion(e))
                {
                    BeginDragSelectionText(1);
                    base.OnMouseDown(e);
                    return;
                }

                // 2026/1/19
                ResetFieldSelect();

                SetCaret(result, reset_selection: true);
#if REMOVED
                SetGlobalOffs(result.Offs);

                Debug.Assert(_global_offs <= _content_length);

                DetectBlockChange1(_blockOffs1, _blockOffs2);

                _blockOffs1 = _global_offs;
                _blockOffs2 = _global_offs;

                InvalidateBlockRegion();

                MoveCaret(result);
#endif
            }
            base.OnMouseDown(e);
        }

        static bool HitButton(HitInfo info)
        {
            var current = info;
            while (current != null)
            {
                if (current.ChildIndex == (int)FieldRegion.Button)
                {
                    return true;
                }

                current = current.InnerHitInfo;
            }
            return false;
        }
        protected override void OnMouseUp(MouseEventArgs e)
        {
            // 有可能没有经过 OnMouseDown()，一上来就是 OnMouseUp()，这多半是别的窗口操作残留的消息
            if (this._isMouseDown == false)
            {
                base.OnMouseUp(e);
                return;
            }

            this.Capture = false;
            this._isMouseDown = false;

            EndMonitorCaretVisible();

            // 抬起了“展开/收缩”按钮
            if (this._isButtonDown == true)
            {
                base.OnMouseUp(e);
                this._isButtonDown = false;
                return;
            }

            if (_splitting)
            {
                FinishSplitting(e.X);
                base.OnMouseUp(e);
                return;
            }

            // 结束拖动文字块过程
            if (InDraggingSelectionText() == 2)
            {
                CompleteDragSelectionText();
                base.OnMouseUp(e);
                return;
            }

            if (InSelectingField())
            {
                EndFieldSelect();
                base.OnMouseUp(e);
                // 立即返回，避免所选的块被破坏
                return;
            }

            {
                var result = _record.HitTest(
                    e.X + this.HorizontalScroll.Value,
                    e.Y + this.VerticalScroll.Value);

                // 曾进入拖拽阶段 1，但直到 MouseUp 也没有经历过 MouseMove
                // 延迟兑现鼠标点击的功能
                if (InDraggingSelectionText() == 1)
                {
                    CompleteDragSelectionText();
                    SetCaret(result, reset_selection: true);
                    base.OnMouseUp(e);
                    return;
                }

                {
                    // 选中范围结束
                    DetectSelectionChange1(_selectOffs1, _selectOffs2);
                    var old_offs = _caret_offs;

                    //if (old_offs != result.Offs)
                    //    SetCaretOffs(result.Offs); // _record.GetGlobalOffs(result);

                    {
                        MoveCaret(result, true, true/*有条件地触发事件*/);
                        _lastX = _caretInfo.X; // 记录最后一次点击鼠标 x 坐标

                        ChangeSelection(() =>
                        {
                            _selectOffs2 = _caret_offs;
                        });
                    }
#if OLD
                    {
                        _blockOffs2 = _global_offs;

                        var changed = DetectBlockChange2(_blockOffs1, _blockOffs2);

                        MoveCaret(result, true, true/*有条件地触发事件*/);

                        _lastX = _caretInfo.X; // 记录最后一次点击鼠标 x 坐标

                        if (changed)
                        {
                            //this.BlockChanged?.Invoke(this, new EventArgs());

                            // TODO: 可以改进为只失效影响到的 Line
                            // this.Invalidate(); // 重绘
                            InvalidateBlockRegion();
                        }
                    }
#endif

                }
            }
            base.OnMouseUp(e);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            // 按下了“展开/收缩”按钮之后，在抬起之前的偶然移动
            if (this._isButtonDown == true)
            {
                Cursor = Cursors.Arrow;
                base.OnMouseMove(e);
                return;
            }

            if (_isMouseDown && _splitting)
            {
                MoveSplitting(e.X);
                base.OnMouseMove(e);
                return;
            }

            if (_isMouseDown)
            {
                BeginMonitorCaretVisible();
            }

            var result = _record.HitTest(
e.X + this.HorizontalScroll.Value,
e.Y + this.VerticalScroll.Value);

            if (HitButton(result))
            {
                Cursor = Cursors.Arrow;
                base.OnMouseMove(e);
                return;
            }

            if (_isMouseDown)
            {


                // 鼠标选择字段时拖动进入这里。而如果是按住 Ctrl 或 Shift 点选，则不应进入这里
                if (InSelectingField())
                {
                    if ((ModifierKeys & (Keys.Control | Keys.Shift)) == 0)
                    {
                        AdjustFieldSelect(result.ChildIndex);
                    }

                    //SetCaretOffs(result.Offs);
                    MoveCaret(result);
                    base.OnMouseMove(e);
                    return;
                }

                var splitter_test = TestSplitterArea(e.X);

                // 如果光标是首次进入块范围
                if (splitter_test >= 0
                    && InDraggingSelectionText() == 1
                    && IsCursorInsideSelectionRegion(e))
                {
                    BeginDragSelectionText(2);
                    //SetCaretOffs(result.Offs);
                    MoveCaret(result);
                    base.OnMouseMove(e);
                    return;
                }

                // 正在拖动文字块中途
                if (InDraggingSelectionText() == 2)
                {
                    // 拖入块范围内，显示表示禁止的光标形状
                    if (IsCursorInsideSelectionRegion(e))
                    {
                        Cursor = Cursors.No;
                    }
                    else
                    {
                        Cursor = Cursors.Arrow;
                        // Cursor = GetMoveCursor();
                    }

                    //SetCaretOffs(result.Offs);
                    MoveCaret(result);
                    base.OnMouseMove(e);
                    return;
                }

                // 按下鼠标左键，拖动定义文字块
                //SetCaretOffs(result.Offs);
                MoveCaret(result);

                if (_selectOffs2 != _caret_offs)
                {
                    //SetCaretOffs(result.Offs);
                    // MoveCaret(result);

                    ChangeSelection(() =>
                    {
                        _selectOffs2 = _caret_offs;
                    });
                }
            }
            else
            {
                var splitter_test = TestSplitterArea(e.X);

                if (splitter_test == -1)
                    Cursor = Cursors.SizeWE;
                else if (splitter_test >= 0)
                {
                    // 判断光标是否在 selection region 范围内
                    if (IsCursorInsideSelectionRegion(e))
                    {
                        // 箭头光标形状，暗示文字块可以被拖动。
                        // TODO: 或者改用更贴合这个意思的带有可移动按时的某种箭头光标形状
                        Cursor = Cursors.Arrow;
                    }
                    else
                    {
                        Cursor = Cursors.IBeam;
                    }
                }
                else
                {
                    Cursor = Cursors.Arrow;
                }
            }

            base.OnMouseMove(e);
        }


        protected override void OnMouseDoubleClick(MouseEventArgs e)
        {
            // 因为会受到 OnMouseUp() 的影响(会在鼠标抬起时被修改块定义)，所以放到稍后来执行
            Task.Run(() =>
            {
                this.Invoke((Action)new Action(() =>
                {
                    SelectCaretSubfield();
                }));
            });
            base.OnMouseDoubleClick(e);
        }

        void ToggleExpand(HitInfo info)
        {
            ReplaceTextResult ret = null;
            using (var g = this.CreateGraphics())
            {
                var handle = g.GetHdc();
                using (var dc = new SafeHDC(handle))
                {
                    ret = _record.ToggleExpand(
                        info,
                        _context,
                        dc,
                        GetLimitWidth()
                        );
                    _context.ClearFontCache();
                }
            }

            if (ret.UpdateRect == System.Drawing.Rectangle.Empty
                && ret.ScrollRect == System.Drawing.Rectangle.Empty)
                return;

            var max_pixel_width = ret.MaxPixel;
            var replaced_text = ret.ReplacedText;
            var update_rect = ret.UpdateRect;
            var scroll_rect = ret.ScrollRect;
            var scroll_distance = ret.ScrolledDistance;

            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;

            if (scroll_distance != 0)
            {
                Utility.Offset(ref scroll_rect, x, y);
                if (scroll_rect.IsEmpty == false)
                {
                    User32.ScrollWindowEx(this.Handle,
                        0,
                        scroll_distance,
                        scroll_rect,
                        null,
                        HRGN.NULL,
                        out _,
                        ScrollWindowFlags.SW_INVALIDATE);
                    _invalidateCount++;
                }
            }

            if (update_rect != System.Drawing.Rectangle.Empty)
            {
                Debug.Assert( update_rect.Width < int.MaxValue, "不允许用极大值来 Invalidate");
                update_rect.Offset(x, y);
                this.Invalidate(update_rect);
                // this.Invalidate();
            }

            if (max_pixel_width == 0)
                max_pixel_width = this._record.GetPixelWidth();
            this.AutoScrollMinSize = new Size(max_pixel_width, _record.GetPixelHeight());
            SetCaret(HitByCaretOffs(_caret_offs), reset_selection: false, ensure_caret_visible: false);
        }
    }
}
