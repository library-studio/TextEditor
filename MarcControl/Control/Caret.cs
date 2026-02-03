using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Vanara.PInvoke;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;
using static LibraryStudio.Forms.TemplateItem;
using static Vanara.PInvoke.LANGID;

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
                return _caretInfo?.Clone() ?? new HitInfo { Box = this };
            }
        }

        // 根据 HitInfo 设置插入符的 offs 和更新显示
        void SetCaret(HitInfo result,
            bool reset_selection = true,
            bool ensure_caret_visible = true)
        {
            //SetCaretOffs(result.Offs);

#if DEBUG
            Debug.Assert(_caret_offs <= _record.TextLength);
#endif

            MoveCaret(result, ensure_caret_visible);

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

            if (_valueListFloating == false)
            {
                HideSuggestion();
            }
            else
            {
                OpenValueListWindow(this.CaretInfo, false);
            }
            /*
            Task.Run(async () => {
                await Task.Delay(500);
                this.Invoke(new Action(() => {
                    if (_valueListFloating == false)
                    {
                        HideSuggestion();
                    }
                    else
                    {
                        OpenValueListWindow(this.CaretInfo, auto_close_prev: true);
                    }
                }));
            });
            */
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
            if (delta > 0 && CaretAtHeaderOrTemplateItem())
            {
                // 为了避免向右移动后 caret 处在令人诧异的等同位置，向右移动也需要模仿向左的 -1 特征
                // 注: 诧异位置比如头标区的右侧末尾，001 字段的字段名末尾，等等
                info = HitByCaretOffs(_caret_offs + delta + 1, -1);
            }
            else
            {
                info = HitByCaretOffs(_caret_offs, delta);
            }
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

        bool CaretAtHeaderOrTemplateItem()
        {
            if (_caretInfo.ChildIndex == 0)
                return true;
            var template_item = FindTemplateItem(_caretInfo, out HitInfo temp);
            if (template_item != null && template_item.Overflow == false)
                return true;
            return false;
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

        #region 被 OpenValueListWindow() 用到的变量

        int suggestion_caret_offs = 0;
        string replaced_text = "";
        int before_length = 0;
        int after_length = 0;

        #endregion

        bool OpenValueListWindow(HitInfo info, bool has_focus)
        {
            bool auto_close_prev = _valueListFloating;

            var template_item = FindTemplateItem(_caretInfo,
                out HitInfo hit_info);
            if (template_item == null)
            {
                if (auto_close_prev)
                    HideSuggestion();
                return false;
            }

            if (template_item.Overflow)
            {
                if (auto_close_prev)
                    HideSuggestion();
                return true;    // 只要是属于 TemplateItem 的区域都返回 true，这样避免往后继续做定义块的处理，保持行为一致
            }

            var struct_info = template_item.GetStructureInfoByBox(template_item, 1);
            // 如果插入符在 TemplateItem 中定额的最后一个字符右边，则不能弹出 ValueList。因为这样弹出会让用户误以为时这里的 List 但实际上选择后修改了下一个 TemplateItem 的内容
            if (hit_info.Offs >= struct_info.Length)
            {
                if (auto_close_prev)
                    HideSuggestion();
                return true;
            }

            var ret = _openValueListWindow();
            /*
            if (ret == true && item_text_length > 0)
            {
                var offs = info.Offs - hit_info.Offs + ((hit_info.Offs / item_text_length) * item_text_length);
                _suggestion_caret_offs = offs;
            }
            else
            {
                HideSuggestion();
                // return false;
                return true;    // 只要是属于 TemplateItem 的区域都返回 true，这样避免往后继续做定义块的处理，保持行为一致
            }
            */
            if (ret == false && auto_close_prev)
            {
                HideSuggestion();
            }
            else
            {
                if (ValueListWindowOpened())
                    _suggestionPopup.HasFocus = has_focus;
            }
            // return ret;
            return true;    // 只要是属于 TemplateItem 的区域都返回 true，这样避免往后继续做定义块的处理，保持行为一致


            bool _openValueListWindow()
            {
                int list_item_text_length = 0;
                // 获得值列表。注意值的字符数可能比 TemplateItem 文本长度短(一般是整倍关系)
                var list = template_item.GetValueList(template_item);
                if (list == null)
                {
                    return false;
                }

                int template_item_text_length = template_item.TextLength;

                var value_font = FixedFontGroup.FirstOrDefault();
                var comment_font = CaptionFontGroup.FirstOrDefault();

                // 列表中第一个事项的字符数
                list_item_text_length = list.FirstOrDefault()?.Value.Length ?? 0;

                // 编辑部分处在 TemplateItem 文本中的偏移。全局偏移
                var offs = info.Offs;
                if (list_item_text_length > 0)
                    offs = info.Offs - hit_info.Offs + ((hit_info.Offs / list_item_text_length) * list_item_text_length);
                suggestion_caret_offs = offs;
                replaced_text = this._record.MergeText(offs, offs + Math.Min(list_item_text_length, template_item_text_length));
                // 将编辑器对应的文本选中
                if (has_focus)
                {
                    this.Select(offs,
                        offs + replaced_text.Length,
                        _caret_offs);
                }

                // 兄弟中最宽的宽度
                int item_width = (template_item.Parent as Template).Children
                    .Where(o => o.Overflow == false)
                    .Max(o => o.GetPixelWidth());
                Rectangle ref_rect = new Rectangle(_caretInfo.X,
                    _caretInfo.Y,
                    0,
                    template_item.GetPixelHeight());
                if (has_focus == false)
                {
                    //var caption_pixel_width = _marcMetrics.GetCaptionPixelWidth(template_item);
                    ref_rect = new Rectangle(_caretInfo.X - hit_info.X/* + caption_pixel_width*/,
                    _caretInfo.Y,
                    item_width + FontContext.DefaultReturnWidth/* - caption_pixel_width*/,
                    template_item.GetPixelHeight());
                }
#if REMOVED
                int delta_x = 0;
                int delta_y = 0;
                if (has_focus == false)
                {
                    // int item_width = template_item.GetPixelWidth();

                    ref_rect.Width = item_width + FontContext.DefaultReturnWidth;
                    /*
                    delta_x = -hit_info.X + item_width + FontContext.DefaultReturnWidth;
                    delta_y = -hit_info.Y - template_item.GetPixelHeight();
                    */
                }
#endif

                // 检查当前 TemplateItem 是否因法定字符数不足，需要进行空白字符填充
                int padding_length = template_item.GetPaddingText(PaddingStyle.TemplateWhole,
                    out before_length);
                after_length = padding_length == 0 ? 0 :
                    padding_length - before_length - list_item_text_length;
                Debug.Assert(before_length >= 0);
                Debug.Assert(after_length >= 0);

                ShowSuggestion(
                    value_font,
                    comment_font,
                    list,
                    replaced_text,
                    //delta_x,
                    //delta_y,
                    ref_rect,
                    (chosen) =>
                    {
                        if (string.IsNullOrEmpty(chosen))
                            return;

                        Debug.Assert(suggestion_caret_offs != -1);
                        var start = suggestion_caret_offs;
                        var end = suggestion_caret_offs + replaced_text.Length;

                        string new_text = chosen;
                        int new_caret_offs = end;
                        if (before_length > 0)
                        {
                            new_text = new string(this.PaddingChar, before_length) + new_text;
                            new_caret_offs += before_length;
                        }
                        if (after_length > 0)
                        {
                            new_text = new_text + new string(this.PaddingChar, after_length);
                        }

                        ReplaceText(start,
                            end,
                            new_text,
                            delay_update: false,
                            auto_adjust_caret_and_selection: true,
                            add_history: true);
                        Select(new_caret_offs, new_caret_offs, new_caret_offs + 1, -1);
                        HideSuggestion();

                        // 重新打开
                        if (_valueListFloating)
                        {
                            this.BeginInvoke(new Action(() =>
                            {
                                OpenValueListWindow(this.CaretInfo, true);
                            }));
                        }
                    },
                    () =>
                    {
                        // if (reset_caret)
                        Select(_caret_offs, _caret_offs, _caret_offs);
                    });
                return true;
            }
        }


        bool _valueListFloating = false;

        public bool ValueListFloating
        {
            get
            {
                return _valueListFloating;
            }
            set
            {
                _valueListFloating = value;
                if (value == true)
                {
                    // 尝试打开
                    OpenValueListWindow(_caretInfo, false);
                }
                else
                {
                    HideSuggestion(true);
                }
            }
        }

        // 查找 HitInfo 中 InnerHitInfo (.Box)链条中书否存在类型为 TemplateItem 的对象
        public static TemplateItem FindTemplateItem(HitInfo info,
            out HitInfo hit_info)
        {
            hit_info = null;
            var current = info;
            while (current != null)
            {
                if (current.Box is TemplateItem)
                {
                    hit_info = current;
                    return current.Box as TemplateItem;
                }
                current = current.InnerHitInfo;
            }
            return null;
        }
    }
}
