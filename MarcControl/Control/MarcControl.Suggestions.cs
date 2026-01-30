using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace LibraryStudio.Forms
{
    public partial class MarcControl
    {
        SuggestionPopup _suggestionPopup;

        void EnsureSuggestionPopup(Font value_font,
            Font comment_font,
            delegate_itemChosen func_itemChosen,
            delegate_cancel func_cancel)
        {
            if (_suggestionPopup != null)
                return;
            _suggestionPopup = new SuggestionPopup(_marcMetrics,
                value_font,
                comment_font,
                this.HighlightBlankChar);
            // _suggestionPopup.ItemChosen += SuggestionPopup_ItemChosen;
            _suggestionPopup.ItemChosen += (s, e) => { func_itemChosen?.Invoke(e); };
            _suggestionPopup.Cancelled += (s, e) => { func_cancel?.Invoke(); };
        }

#if REMOVED
        void SuggestionPopup_ItemChosen(object sender, string chosen)
        {
            if (string.IsNullOrEmpty(chosen))
                return;

            // 把选中的内容插入到当前 caret 位置
            // ReplaceText(_caret_offs, _caret_offs, chosen, delay_update: true, auto_adjust_caret_and_selection: true, add_history: true);

            Debug.Assert(_suggestion_caret_offs != -1);
            var start = _suggestion_caret_offs;
            var end = _suggestion_caret_offs + _suggestion_start_text.Length;
            ReplaceText(start,
                end,
                chosen,
                delay_update: false,
                auto_adjust_caret_and_selection: true,
                add_history: true);
            Select(end, end, end + 1, -1);

            /*
            Task.Run(async () => {
                await Task.Delay(1000);
                BeginInvoke(new Action(() =>
                {
                    try { _suggestionPopup.Focus(); } catch { }
                    try { SetCompositionWindowPos(); } catch { }
                }));

            });
            */

            // 关闭弹窗后需要重新定位 IME 合成窗口
            try { _suggestionPopup.Hide(); } catch { }
            try { SetCompositionWindowPos(); } catch { }
        }
#endif

        /*
        // ValueList 窗口弹出前对应的插入符偏移
        int _suggestion_caret_offs = -1;
        // ValueList 窗口弹出前对应的拟替换的原有文字片段。注意可能比 Value 内容短
        string _suggestion_start_text = "";
        */

        public delegate void delegate_itemChosen(string text);
        public delegate void delegate_cancel();
        /// <summary>
        /// 显示候选弹窗。items 可以为空（会清空并隐藏）。
        /// 弹窗不会激活窗口（WS_EX_NOACTIVATE），因此输入焦点、IME 保持在 MarcControl。
        /// </summary>
        public void ShowSuggestion(
            Font value_font,
            Font comment_font,
            IEnumerable<ValueItem> arr,
            string selected_item_text,
            int delta_x,
            int delta_y,
            delegate_itemChosen func_itemChosen,
            delegate_cancel func_cancel)
        {
            if (arr.Any() == false)
            {
                HideSuggestion();
                return;
            }

            EnsureSuggestionPopup(value_font,
                comment_font,
                func_itemChosen,
                func_cancel);
            _suggestionPopup.SetItems(arr, selected_item_text);

            _suggestionPopup._focus_owner = this;

            // 计算弹窗显示位置（屏幕坐标）：在 caret 下方优先显示，否则上方
            var caretClient = new Point(_caretInfo.X - this.HorizontalScroll.Value + delta_x,
                                        _caretInfo.Y - this.VerticalScroll.Value + delta_y);
            var screenCaret = this.PointToScreen(caretClient);
            int belowY = screenCaret.Y + (_caretInfo.LineHeight > 0 ? _caretInfo.LineHeight : this.Font.Height);

            // 使用屏幕坐标显示无激活窗体
            _suggestionPopup.ShowAt(new Point(screenCaret.X, belowY));

            /*
            // 显示后更新 IME 合成窗口位置，确保输入法仍在正确位置
            try { SetCompositionWindowPos(); } catch { }

            Debug.Assert(_suggestionPopup.Visible == true);
            */
            BeginInvoke(new Action(() =>
            {
                try { this.Focus(); } catch { }
                try { SetCompositionWindowPos(); } catch { }
            }));
        }

        public void HideSuggestion(bool reset_caret = false)
        {
            if (_suggestionPopup != null && _suggestionPopup.Visible)
            {
                try { _suggestionPopup.Hide(); } catch { }
                if (reset_caret)
                    Select(_caret_offs, _caret_offs, _caret_offs);
            }

            // 关闭后也更新 IME 合成窗口位置
            try { SetCompositionWindowPos(); } catch { }
        }

        public bool ValueListWindowOpened()
        {
            return (_suggestionPopup != null && _suggestionPopup.Visible);
        }

        public bool HandlePopupKeyDown(KeyEventArgs e)
        {
            if (_suggestionPopup == null || !_suggestionPopup.Visible)
                return false;

            switch (e.KeyCode)
            {
                case Keys.Up:
                    _suggestionPopup.SelectPrev();
                    e.Handled = true;
                    return true;
                case Keys.Down:
                    _suggestionPopup.SelectNext();
                    e.Handled = true;
                    return true;
                case Keys.PageUp:
                    _suggestionPopup.PageUp();
                    e.Handled = true;
                    return true;
                case Keys.PageDown:
                    _suggestionPopup.PageDown();
                    e.Handled = true;
                    return true;
                case Keys.Enter:
                case Keys.Tab:
                    // 确认当前选中项
                    _suggestionPopup.AcceptSelected();
                    e.Handled = true;
                    return true;
                case Keys.Escape:
                    HideSuggestion(true);
                    e.Handled = true;
                    return true;
            }
            return false;
        }

        public bool HandlePopupKeyPress(KeyPressEventArgs e)
        {
            if (_suggestionPopup == null || !_suggestionPopup.Visible)
            {
                return false;
            }

            if (e.KeyChar == (char)Keys.Escape)
            {
                HideSuggestion(true);
                e.Handled = true;
                return true;
            }
            if (e.KeyChar == '\r')
            {
                e.Handled = true;
                return true;
            }
            return false;
        }
    }
}
