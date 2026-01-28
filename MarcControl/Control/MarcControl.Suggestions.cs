#if NO
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibraryStudio.Forms
{
    public partial class MarcControl
    {
        SuggestionPopup _suggestionPopup;

        void EnsureSuggestionPopup()
        {
            if (_suggestionPopup != null)
                return;
            _suggestionPopup = new SuggestionPopup();
            _suggestionPopup.ItemChosen += SuggestionPopup_ItemChosen;
            _suggestionPopup.Cancelled += (s, e) => { /* nothing by default */ };

            // 防止 hosted 控件默认用 Tab 切换到自身。ListBox 已在 SuggestionPopup 设置 TabStop = false，
            // 这里再订阅 LostFocus 仅作保护（可选）。
            // 如果需要在 popup 失去激活时自动关闭，也可以在此订阅 Closed 事件。
        }

        void SuggestionPopup_ItemChosen(object sender, string chosen)
        {
            if (string.IsNullOrEmpty(chosen))
                return;

            // 把选中的内容插入到当前 caret 位置
            ReplaceText(_caret_offs, _caret_offs, chosen, delay_update: true, auto_adjust_caret_and_selection: true, add_history: true);

            // 隐藏弹窗并恢复焦点到编辑控件（异步恢复以避开 popup 的内部焦点逻辑）
            HideSuggestion();
            BeginInvoke(new Action(() =>
            {
                try { this.Focus(); } catch { }
                try { SetCompositionWindowPos(); } catch { }
            }));
        }

        /// <summary>
        /// 显示候选弹窗。items 可以为空（会清空并隐藏）。
        /// </summary>
        public void ShowSuggestion(IEnumerable<string> items)
        {
            var arr = (items ?? Enumerable.Empty<string>()).ToArray();
            if (arr.Length == 0)
            {
                HideSuggestion();
                return;
            }

            EnsureSuggestionPopup();
            _suggestionPopup.SetItems(arr);

            // 计算弹窗显示位置（相对于控件的 client 坐标）
            var caretClient = new Point(_caretInfo.X - this.HorizontalScroll.Value,
                                        _caretInfo.Y - this.VerticalScroll.Value);
            // 将要显示在 caret 下方（控件 client 坐标）
            var localShowPoint = new Point(caretClient.X, caretClient.Y + (_caretInfo.LineHeight > 0 ? _caretInfo.LineHeight : this.Font.Height));

            // 使用 owner overload 在控件坐标系显示，使弹窗与控件关联
            // 这样 ToolStripDropDown 能正确处理相对位置，并减少全局激活副作用
            try
            {
                _suggestionPopup.Show(this, localShowPoint);
            }
            catch
            {
                // 退回到屏幕坐标显示（兼容性兜底）
                var screenCaret = this.PointToScreen(caretClient);
                _suggestionPopup.ShowAt(new Point(screenCaret.X, screenCaret.Y + (_caretInfo.LineHeight > 0 ? _caretInfo.LineHeight : this.Font.Height)));
            }

            Task.Run(() =>
            {
                Thread.Sleep(100);
                // 延迟恢复焦点到 MarcControl，保证在 dropdown 内部的焦点调度完成后我们再把焦点回填
                BeginInvoke(new Action(() =>
                {
                    try { this.Focus(); } catch { }
                    try { SetCompositionWindowPos(); } catch { }
                }));
            });

            // 调试断言仅作检查（注意：ToolStripDropDown.Focused 并不总反映 hosted control 的焦点）
            Debug.Assert(_suggestionPopup.Focused == false || true);
        }

        public void HideSuggestion()
        {
            if (_suggestionPopup != null && _suggestionPopup.Visible)
            {
                try { _suggestionPopup.Close(); } catch { }
            }

            // 延迟恢复焦点，避免与 popup 内部的失活流程冲突
            BeginInvoke(new Action(() =>
            {
                if (this.Focused == false)
                {
                    try { this.Focus(); } catch { }
                    try { SetCompositionWindowPos(); } catch { }
                }
            }));
        }

        /// <summary>
        /// 在 __OnKeyDown__ 顶部调用：如果弹窗可见并且按键是弹窗相关的（上下/翻页/回车/ESC/TAB），则处理并返回 true（表示已处理）
        /// </summary>
        public bool HandlePopupKeyDown(KeyEventArgs e)
        {
            if (_suggestionPopup == null || !_suggestionPopup.Visible)
                return false;

            switch (e.KeyCode)
            {
                case Keys.Up:
                    if (_suggestionPopup.SelectPrev()) { e.Handled = true; return true; }
                    break;
                case Keys.Down:
                    if (_suggestionPopup.SelectNext()) { e.Handled = true; return true; }
                    break;
                case Keys.PageUp:
                    if (_suggestionPopup.PageUp()) { e.Handled = true; return true; }
                    break;
                case Keys.PageDown:
                    if (_suggestionPopup.PageDown()) { e.Handled = true; return true; }
                    break;
                case Keys.Enter:
                case Keys.Tab:
                    // 确认当前选中项
                    _suggestionPopup.AcceptSelected();
                    e.Handled = true;
                    return true;
                case Keys.Escape:
                    HideSuggestion();
                    e.Handled = true;
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 在 __OnKeyPress__ 顶部调用：处理 Enter/Escape 等字符型事件（若需要）
        /// </summary>
        public bool HandlePopupKeyPress(KeyPressEventArgs e)
        {
            if (_suggestionPopup == null || !_suggestionPopup.Visible)
                return false;

            if (e.KeyChar == (char)Keys.Escape)
            {
                HideSuggestion();
                e.Handled = true;
                return true;
            }
            // 其余字符一般仍交给 MarcControl（插入文本时会关闭或保留弹窗，按需）
            return false;
        }
    }
}


csharp MarcControl\Control\MarcControl.Suggestions.cs
#endif
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibraryStudio.Forms
{
    public partial class MarcControl
    {
        SuggestionPopup _suggestionPopup;

        void EnsureSuggestionPopup()
        {
            if (_suggestionPopup != null)
                return;
            _suggestionPopup = new SuggestionPopup();
            _suggestionPopup.ItemChosen += SuggestionPopup_ItemChosen;
            _suggestionPopup.Cancelled += (s, e) => { /* nothing by default */ };
        }

        void SuggestionPopup_ItemChosen(object sender, string chosen)
        {
            if (string.IsNullOrEmpty(chosen))
                return;

            // 把选中的内容插入到当前 caret 位置
            // ReplaceText(_caret_offs, _caret_offs, chosen, delay_update: true, auto_adjust_caret_and_selection: true, add_history: true);

            Debug.Assert(_suggestion_caret_offs != -1);
            var start = _suggestion_caret_offs;
            var end = _suggestion_caret_offs + chosen.Length;
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

        int _suggestion_caret_offs = -1;

        /// <summary>
        /// 显示候选弹窗。items 可以为空（会清空并隐藏）。
        /// 弹窗不会激活窗口（WS_EX_NOACTIVATE），因此输入焦点、IME 保持在 MarcControl。
        /// </summary>
        public void ShowSuggestion(IEnumerable<string> items,
            int delta_x,
            int delta_y)
        {
            var arr = (items ?? Enumerable.Empty<string>()).ToArray();
            if (arr.Length == 0)
            {
                HideSuggestion();
                return;
            }

            EnsureSuggestionPopup();
            _suggestionPopup.SetItems(arr);

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

        public void HideSuggestion()
        {
            if (_suggestionPopup != null && _suggestionPopup.Visible)
            {
                try { _suggestionPopup.Hide(); } catch { }
            }

            // 关闭后也更新 IME 合成窗口位置
            try { SetCompositionWindowPos(); } catch { }
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
                    HideSuggestion();
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
                HideSuggestion();
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
