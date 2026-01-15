using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 鼠标拖拽移动文字块
    /// </summary>
    public partial class MarcControl
    {
        Region _selectionRegion = null;

        // 是否正在拖动文字块，在拖动的哪个阶段
        //  0:  不在拖动中
        //  1:  已经启动拖动，等待第一次 MouseMove
        //  2:  经过了 MouseMove
        int _draggingSelectionText = 0;

        // 获得当前文字块的 Region
        // 此 Region 对象为系统持有，用后不用 Dispose()
        Region GetCurrentSelectionRegion()
        {
            if (_selectOffs1 == _selectOffs2)
                return null;
            if (_selectionRegion == null)
            {
                var start = Math.Min(_selectOffs1, _selectOffs2);
                var end = Math.Max(_selectOffs1, _selectOffs2);
                _selectionRegion = this._record.GetRegion(start, end);
            }
            return _selectionRegion;
        }

        void DisposeSelectionRegion()
        {
            if (_selectionRegion != null)
            {
                _selectionRegion.Dispose();
                _selectionRegion = null;
            }
        }

        void BeginDragSelectionText(int stage)
        {
            if (stage != 1 && stage != 2)
                throw new ArgumentException($"stage 值 {stage} 不合法");
            _draggingSelectionText = stage;
        }

        int InDraggingSelectionText()
        {
            return _draggingSelectionText;
        }

        bool CompleteDragSelectionText()
        {
            _draggingSelectionText = 0;

            if (this._readonly)
                return false;

            if (HasSelection() == false)
                return false;

            var start = Math.Min(this.SelectionStart, this.SelectionEnd);
            var length = Math.Abs(this.SelectionEnd - this.SelectionStart);

            // 要去的位置在块边沿和边沿之内，也就没有必要真正拖动
            if (_caret_offs >= start && _caret_offs <= start + length)
                return false;

            var text = _record.MergeText(start, start + length);

            bool copy = true;
            if (controlPressed == false)
            {
                // 先剪切，再粘贴
                SoftlyRemoveSelectionText();
                copy = false;
            }

#if REMOVED
            this.ReplaceText(_global_offs,
    _global_offs,
    text,
    delay_update: false,
    auto_adjust_global_offs: false);

            this.Select(_global_offs,
_global_offs + text.Length,
_global_offs + 1,
-1);
            // 复制后，
            // 更新原有 start end 文字块范围。
            if (copy)
            {
                // 只是要注意，如果原有 start-end
                // 在复制的位置之后，那么块会被挤向后，所以数值需要调整
                if (_global_offs < start)
                {
                    start += text.Length;
                }
                InvalidateBlockRegion(start, start + length);
            }

            return true;
#endif
            this.Select(_caret_offs, _caret_offs, _caret_offs);
            return SoftlyPaste(text);
        }

        // 判断光标是否在 selection region 范围内
        bool IsCursorInsideSelectionRegion(MouseEventArgs e)
        {
            var p = new Point(e.X + this.HorizontalScroll.Value,
                e.Y + this.VerticalScroll.Value);
            var region = GetCurrentSelectionRegion();
            return (region != null && region.IsVisible(p));
        }

        /*
        static Cursor _moveCursor = null;
        Cursor GetMoveCursor()
        {
            if (_moveCursor == null)
            {
                using (var stream = new MemoryStream(LibraryStudio.Forms.Properties.Resource1.move_block_cursor_48))
                {
                    _moveCursor = new Cursor(stream);
                }
            }
            return _moveCursor;
        }
        */
    }
}
