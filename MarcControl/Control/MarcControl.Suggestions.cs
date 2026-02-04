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

        // 记录用于追踪的宿主 Form（FindForm()）
        Form _popupOwnerForm = null;

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
        // parameters:
        //      ref_rect    参考用的矩形。这个矩形是显示小窗口时需要避开的一个区域
        public void ShowSuggestion(
            Font value_font,
            Font comment_font,
            IEnumerable<ValueItem> arr,
            string selected_item_text,
            Rectangle ref_rect,
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
            // 设置事项，并且初始化小窗口尺寸
            _suggestionPopup.SetItems(arr, selected_item_text);

            _suggestionPopup._focus_owner = this;

            // 计算弹窗显示位置（屏幕坐标）：在 caret 下方优先显示，否则上方

            //var caretClient = new Point(_caretInfo.X - this.HorizontalScroll.Value + delta_x,
            //                            _caretInfo.Y - this.VerticalScroll.Value + delta_y);
            var caretClient = new Point(ref_rect.Right - this.HorizontalScroll.Value,
                ref_rect.Top - this.VerticalScroll.Value);
            var screenCaret = this.PointToScreen(caretClient);

            int belowY = screenCaret.Y;
            if (ref_rect.Width == 0)
                belowY += ref_rect.Height;

            // + (_caretInfo.LineHeight > 0 ? _caretInfo.LineHeight : this.Font.Height);

            Rectangle screenBounds = Screen.FromControl(this).WorkingArea;
            if (belowY + _suggestionPopup.Height > screenBounds.Height)
            {
                belowY = screenCaret.Y - _suggestionPopup.Height;
            }
            if (screenCaret.X + _suggestionPopup.Width > screenBounds.Width)
            {
                screenCaret.X -= ref_rect.Width + _suggestionPopup.Width;
            }

            // 使用屏幕坐标显示无激活窗体
            _suggestionPopup.ShowAt(new Point(screenCaret.X, belowY));

            // 开始追踪宿主窗体与控件自身的移动/大小变化
            StartPopupTracking();

            BeginInvoke(new Action(() =>
            {
                try { this.Focus(); } catch { }
                // 显示后更新 IME 合成窗口位置，确保输入法仍在正确位置
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

                // 关闭后也更新 IME 合成窗口位置
                try { SetCompositionWindowPos(); } catch { }

                // 关闭后停止追踪并更新 IME 合成窗口位置
                StopPopupTracking();
            }
        }

        #region 追踪大小改变、移动

        // 开始订阅移动/大小等事件以便跟随
        void StartPopupTracking()
        {
            StopPopupTracking(); // 防止重复订阅

            // 订阅宿主 Form 的移动/位置/大小变化事件
            var f = this.FindForm();
            if (f != null)
            {
                _popupOwnerForm = f;
                _popupOwnerForm.Move += PopupOwnerForm_MovedOrResized;
                _popupOwnerForm.LocationChanged += PopupOwnerForm_MovedOrResized;
                _popupOwnerForm.SizeChanged += PopupOwnerForm_MovedOrResized;
            }

            // 订阅本控件的变化事件（控件被父容器布局/移动时）
            this.LocationChanged += MarcControl_LocationChanged;
            this.VisibleChanged += MarcControl_VisibleChanged;

            // 如果控件滚动（OnScroll 被覆盖），你也可以调用 RepositionSuggestionPopup()，
            // 这里直接订阅一个可观察到的事件：当 MarcControl 的滚动发生时（如果有），在 OnScroll 中显式调用 RepositionSuggestionPopup。
        }

        void StopPopupTracking()
        {
            if (_popupOwnerForm != null)
            {
                try
                {
                    _popupOwnerForm.Move -= PopupOwnerForm_MovedOrResized;
                    _popupOwnerForm.LocationChanged -= PopupOwnerForm_MovedOrResized;
                    _popupOwnerForm.SizeChanged -= PopupOwnerForm_MovedOrResized;
                }
                catch { }
                _popupOwnerForm = null;
            }

            try
            {
                this.LocationChanged -= MarcControl_LocationChanged;
                this.VisibleChanged -= MarcControl_VisibleChanged;
            }
            catch { }
        }

        // 事件处理：宿主窗体移动或大小变化
        void PopupOwnerForm_MovedOrResized(object sender, EventArgs e)
        {
            if (_suggestionPopup == null || !_suggestionPopup.Visible)
                return;
            // 异步调用以避开可能的重入或时序问题
            this.BeginInvoke(new Action(() =>
            {
                try { RepositionSuggestionPopup(); } catch { }
            }));
        }

        // 事件处理：MarcControl 自身位置/可见性变化
        void MarcControl_LocationChanged(object sender, EventArgs e)
        {
            if (_suggestionPopup == null || !_suggestionPopup.Visible)
                return;
            this.BeginInvoke(new Action(() =>
            {
                try { RepositionSuggestionPopup(); } catch { }
            }));
        }

        void MarcControl_VisibleChanged(object sender, EventArgs e)
        {
            // 若控件隐藏，隐藏 popup
            if (!this.Visible)
            {
                HideSuggestion();
                return;
            }
            // 若可见且 popup 显示中，重新定位
            if (_suggestionPopup != null && _suggestionPopup.Visible)
            {
                this.BeginInvoke(new Action(() =>
                {
                    try { RepositionSuggestionPopup(); } catch { }
                }));
            }
        }

        void RepositionSuggestionPopup()
        {
            HideSuggestion();
        }

#endregion

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
