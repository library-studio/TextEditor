using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.PInvoke;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 插入符
    /// </summary>
    public partial class MarcControl
    {
        bool _caretCreated = false;

        HitInfo _caretInfo = new HitInfo();

        public HitInfo CaretInfo
        {
            get
            {
                return _caretInfo?.Clone() ?? new HitInfo();
            }
        }

        // 根据 HitInfo 设置插入符的 offs 和更新显示
        void SetCaret(HitInfo result,
            bool reset_selection = true)
        {
            //SetCaretOffs(result.Offs);

#if DEBUG
            Debug.Assert(_caret_offs <= _record.TextLength);
#endif

            MoveCaret(result);

            if (reset_selection)
            {
                ChangeSelection(_caret_offs);
            }
        }

        // 当前插入符所在的字段 index。如果为 -1 表示不在任何字段上(可能是当前 MARC 内容为空)
        public int CaretFieldIndex
        {
            get
            {
                return _caretInfo?.ChildIndex ?? -1;
            }
        }


        // 当前插入符在字段的哪个区域。
        // 0:字段名 1:指示符 2:内容 -1:未知
        public FieldRegion CaretFieldRegion
        {
            get
            {
                return (FieldRegion)(_caretInfo?.InnerHitInfo?.ChildIndex ?? (int)FieldRegion.None);
            }
        }


        void RecreateCaret()
        {
            // 重新创建一次 Caret，改变 caret 高度
            if (_caretCreated)
            {
                User32.DestroyCaret();
                CreateCaret();
                if (this.Focused)
                    User32.ShowCaret();
            }
        }

        void CreateCaret()
        {
            var height = _caretInfo.LineHeight == 0 ? this.Font.Height : _caretInfo.LineHeight;
            var width = Math.Max(2, height / 10);
            User32.CreateCaret(this.Handle, new HBITMAP(IntPtr.Zero), width, height);
        }


        public void EnsureCaretVisible()
        {
            int x_delta = 0;
            int y_delta = 0;
            // 可见区域 左右边界
            var left = this.HorizontalScroll.Value;
            var right = this.HorizontalScroll.Value + this.ClientSize.Width;
            right -= 10;
            if (_caretInfo.X < left)
                x_delta = _caretInfo.X - left;
            else if (_caretInfo.X >= right)
                x_delta = _caretInfo.X - right;
            // 可见区域 上下边界
            var top = this.VerticalScroll.Value;
            var bottom = this.VerticalScroll.Value + this.ClientSize.Height;
            if (_caretInfo.Y < top)
                y_delta = _caretInfo.Y - top;
            else if (_caretInfo.Y + FontContext.DefaultFontHeight >= bottom)
                y_delta = _caretInfo.Y + FontContext.DefaultFontHeight - bottom;

            if (x_delta != 0 || y_delta != 0)
            {
                this.AutoScrollPosition = new Point(
                    this.HorizontalScroll.Value + x_delta,
                    this.VerticalScroll.Value + y_delta);

                RefreshCaret();

                // this.Invalidate();
            }
        }

        void RefreshCaret()
        {
            if (_caretCreated)
            {
                User32.HideCaret(this.Handle);
                User32.SetCaretPos(-this.HorizontalScroll.Value + _caretInfo.X,
                    -this.VerticalScroll.Value + _caretInfo.Y);
                User32.ShowCaret(this.Handle);
            }
        }

        // parameters:
        //      conditional_trigger   是否有条件地触发事件
        //                          所谓条件就是，和前一次的 global_offs 要有不同才会触发事件。常用于 OnMouseUp() 时，因为 OnMouseDown() 已经触发一次事件了
        void MoveCaret(HitInfo result,
            bool ensure_caret_visible = true,
            bool conditional_trigger = false)
        {
            var old_offs = _caretInfo?.Offs ?? 0;
            var old_field_index = _caretInfo.ChildIndex;

            this._caret_offs = result.Offs; // 2026/1/4
            Debug.Assert(result.Offs == this._caret_offs, "caretInfo.Offs 和 _caret_offs 未能同步");

            /*
            if (result.LineHeight == 0)
                return;
            Debug.Assert(result.LineHeight != 0);
            */
            var old_caret_height = _caretInfo?.LineHeight ?? 0;
            _caretInfo = result;

            if (ensure_caret_visible)
                EnsureCaretVisible();

            if (old_caret_height != _caretInfo?.LineHeight)
            {
                RecreateCaret();
            }

            /*
            if (_caretCreated)
            {
                User32.HideCaret(this.Handle);
                User32.SetCaretPos(-this.HorizontalScroll.Value + _caretInfo.X,
                    -this.VerticalScroll.Value + _caretInfo.Y);
                User32.ShowCaret(this.Handle);
            }
            */
            OnFocusedIndexChanged();
            RefreshCaret();

            SetCompositionWindowPos();

            // 插入符移动以后，重置子字段选择的 toggle 状态
            if (old_offs != this._caret_offs)
                _selectCurrentFull = true;

            if (conditional_trigger && old_offs == (_caretInfo?.Offs ?? 0))
            {

            }
            else
            {
                OnCaretMoved(EventArgs.Empty);
            }
        }

        public virtual void OnCaretMoved(EventArgs e)
        {
            CaretMoved?.Invoke(this, e);
        }

        private int _caret_offs = 0; // Caret 全局偏移量。

        // 插入符全局偏移量
        public int CaretOffset
        {
            get { return _caret_offs; }
        }

#if REMOVED
        void SetCaretOffs(int offs)
        {
            if (_caret_offs != offs)
            {
                _caret_offs = offs;
                //if (trigger_event)
                //    this.CaretMoved?.Invoke(this, new EventArgs());
            }
        }

        void AdjustGlobalOffs(int delta)
        {
            if (delta != 0)
            {
                _caret_offs += delta;
                // this.CaretMoved?.Invoke(this, new EventArgs());
            }
        }
#endif

        // TODO: 名字叫 offset... 比较好
        // 平移全局偏移量，和平移块范围
        bool DeltaCaretOffsAndSelectionOffs(int delta)
        {
            if (_caret_offs + delta < 0)
                return false;

            DetectSelectionChange1(_selectOffs1, _selectOffs2);

            var start_offs = _caret_offs; // 记录开始偏移量

            HitInfo info = null;
            if (/*delta >= 0*/false)
            {
                // 为了避免向右移动后 caret 处在令人诧异的等同位置，向右移动也需要模仿向左的 -1 特征
                // 注: 诧异位置比如头标区的右侧末尾，001 字段的字段名末尾，等等
                info = HitByCaretOffs(_caret_offs + delta + 1, -1);
            }
            else
                info = HitByCaretOffs(_caret_offs, delta);
            //SetCaretOffs(info.Offs); // 更新 _global_offs
            MoveCaret(info);

            _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标

            // 平移块范围
            if (_selectOffs1 >= start_offs)
                _selectOffs1 += delta;
            if (_selectOffs2 >= start_offs)
                _selectOffs2 += delta;

            // 块定义发生刷新才有必要更新变化的区域
            InvalidateSelectionRegion();
            return true;
        }

        HitInfo HitByCaretOffs(int offs, int delta = 0)
        {
            _record.MoveByOffs(offs, delta, out HitInfo info);
            return info;
        }

        int _lastX = 0;  // 最后一次左右移动，点击设置插入符的位置信息。用于确定上下移动的初始 x 值


        public void SetLastX()
        {
            _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标
        }

    }
}
