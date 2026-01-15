using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 定义多个连续的字段为文字块
    /// </summary>
    public partial class MarcControl
    {
        #region 选择多个字段

        // 开始选择完整字段的开始 index (字段下标)
        // TODO: start 要永久记忆，MouseUp 之后也要记忆。另外用一个 bool 变量表示是否正在拖动之中
        bool _selecting_field = false;   // 是否正在选择字段过程中? 
        int _select_field_start = -1;
        int _select_field_end = -1;

        bool InSelectingField()
        {
            return _selecting_field;
        }

        void BeginFieldSelect(int index)
        {
            // 如果是按住 Ctrl 键进入本函数，则要汇总当前已有的 offs range 对应的 field index start end range，以便开始在此基础上修改选择
            if (shiftPressed || controlPressed)
            {
                // Ctrl 键被按下的时候，观察 _select_field_start 是否有以前
                // 残留的值，如果有则说明刚点选过(完整)字段，可以直接利用

                if (_select_field_start == -1)
                    _select_field_start = index;
                _select_field_end = index;  // 尾部则用最新 index 充当
                // Debug.WriteLine($"start={_select_field_start} end={_select_field_end}");
            }
            else
            {
                _select_field_start = index;
                _select_field_end = index;
            }

            _selecting_field = true;
            UpdateFieldSelection();
        }

        void AdjustFieldSelect(int index)
        {
            if (_selecting_field == false)
                return;

            if (_select_field_end == index)
                return; // 没有变化

            if (index < 0 || index > this._record.FieldCount)
                return;

            _select_field_end = index;

            UpdateFieldSelection();
        }

        // 更新 field offs range 和显示
        void UpdateFieldSelection()
        {
            int start_index = Math.Min(_select_field_start, _select_field_end);
            int end_index = Math.Max(_select_field_start, _select_field_end);
            int count = end_index - start_index + 1;

            if (start_index + count > _record.FieldCount)
                count = _record.FieldCount - start_index;

            if (count == 0)
                return;

            var ret = _record.GetContiguousFieldOffsRange(start_index,
                count,
                out int start_offs,
                out int end_offs);
            if (ret == true)
            {
                ChangeSelection(start_offs, end_offs);
            }
        }

        void EndFieldSelect()
        {
            if (shiftPressed || controlPressed)
            {
                // 中途不改变 _selecting_field
            }
            else
            {
                _selecting_field = false;
            }
        }

        void ResetFieldSelect()
        {
            _select_field_start = -1;
        }

        #endregion

    }
}
