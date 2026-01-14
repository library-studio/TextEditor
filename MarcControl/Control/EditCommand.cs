using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

using LibraryStudio.Forms.MarcControlDialog;
using static LibraryStudio.Forms.MarcField;
using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 编辑命令
    /// </summary>
    public partial class MarcControl
    {
        #region 编辑操作

        // 编辑操作时，自动填充不足的字符
        // 键盘输入回车时，是否自动填充分离位置左右两侧的文本，以防止出现不足 3 或 5 字符的短字段内容。
        public bool PadWhileEditing { get; set; } = false;
        public char PaddingChar { get; set; } = ' ';

        public virtual bool ProcessBackspaceKey(HitInfo info)
        {
            Debug.Assert(this._caret_offs == info.Offs);

            if (this._readonly)
            {
                return false;
            }

            if (DeleteSelectionText())
            {
                return true;
            }

            var delay = _keySpeedDetector.Detect();
            if (_caret_offs > 0)
            {
                return ProcessBackspaceChar(info, _deleteKeyStyle, delay);
            }
            return false;
        }

        // TODO: 改为可以删除泰文的任意一个 char
        public virtual bool ProcessBackspaceChar(HitInfo info_param,
            DeleteKeyStyle style,
            bool delay)
        {
            Debug.Assert(this._caret_offs == info_param.Offs);

            var save_info = info_param.Clone();
            // 记忆起点 offs
            var old_offs = info_param.Offs;
            // 移动插入符
            var ret = _record.MoveByOffs(info_param.Offs, -1, out HitInfo info);
            if (ret != 0)
            {
                return false;
            }

            //SetGlobalOffs(info.Offs);
            //MoveCaret(HitByGlobalOffs(info.Offs));

            var input_info = this._record.GetDeleteInfo(info,
                style,
                PadWhileEditing ? PaddingChar : (char)0);

            // 特殊文字，可能多个字符粘连，中间不允许插入符停留
            // TODO: 在保护区域删除时要自动填充
            if (old_offs - info.Offs > 1)
            {
                ReplaceText(info.Offs,
    old_offs,
    "",
    delay_update: delay,
    auto_adjust_caret_and_selection: false);
            }
            else
            {
                if (input_info.Start == input_info.End
                    && input_info.Text == "")
                {
                    return false;
                }
                else
                {
                    ReplaceText(input_info.Start,
                        input_info.End,
                        input_info.Text,
                        delay_update: delay,
                        auto_adjust_caret_and_selection: false);
                }
                // 插入符 offs 需要特殊调整
                if (input_info.Caret != -1)
                {
                    //SetGlobalOffs(input_info.Caret);
                    //MoveCaret(HitByGlobalOffs(input_info.Caret));
                    info.Offs = input_info.Caret;
                }

            }


            // 重新调整一次 caret 位置。因为有可能在最后一行删除最后一个字符时突然行数减少
            _record.MoveByOffs(info.Offs + 1, -1, out info);

            //MoveCaret(info);
            SetCaret(info);

            _lastX = _caretInfo.X; // 记录最后一次左右移动的 x 坐标

            /*
            // 2026/1/11
            ChangeSelection(() =>
            {
                this._selectOffs1 = info.Offs;
                this._selectOffs2 = info.Offs;
            });
            */
            return true;
        }

        #region 编辑行为风格

        DeleteKeyStyle _deleteKeyStyle = DeleteKeyStyle.DeleteFieldTerminator;

        public DeleteKeyStyle DeleteKeyStyle
        {
            get
            {
                return _deleteKeyStyle;
            }
            set
            {
                _deleteKeyStyle = value;
            }
        }

        /*
        bool _deleteKeyAsDeleteField = false;
        public bool DeleteKeyAsDeleteField
        {
            get
            {
                return _deleteKeyAsDeleteField;
            }
            set
            {
                _deleteKeyAsDeleteField = value;
            }
        }

        bool _allowDeleteFieldTerminator = true;
        public bool DeleteFieldTerminator
        {
            get
            {
                return _allowDeleteFieldTerminator;
            }
            set
            {
                _allowDeleteFieldTerminator = value;
            }
        }
        */

        #endregion

        public virtual bool ProcessDeleteKey(HitInfo info,
            DeleteKeyStyle style)
        {
            Debug.Assert(this._caret_offs == info.Offs);

            if (this._readonly)
                return false;

            if (DeleteSelectionText())
            {
                return true;
            }

            var delay = _keySpeedDetector.Detect();
            // TODO: 可以考虑增加一个功能，Ctrl+Delete 删除一个 char。而不是删除一个不可分割的 cluster(cluster 可能包含若干个 char)
            if (info.Offs < _record.TextLength/*_content_length*/)
            {
                if (style.HasFlag(DeleteKeyStyle.DeleteKeyAsDeleteField))
                {
                    // 判断 caret 是否处在字段名和指示符区域，如果是，则改为执行删除字段
                    var caret_region = this.CaretFieldRegion;
                    if (caret_region == FieldRegion.Name
                        || caret_region == FieldRegion.Indicator)
                    {
                        // this.GetDomRecord().DeleteField(_caretInfo.ChildIndex);
                        DeleteFields(new int[] { _caretInfo.ChildIndex });
                        return true;
                    }
                }

                return ProcessDeleteChar(_caretInfo, _deleteKeyStyle, delay);
#if REMOVED
                            // TODO: 改写函数，单独执行 delete char 功能
                            var replace = this._record.GetReplaceMode(_caretInfo,
                                "delete",
                                PaddingChar,
                                out string fill_content);
                            if (string.IsNullOrEmpty(fill_content) == false)
                                ReplaceText(_global_offs,
                                    _global_offs,
                                    fill_content,
                                    delay_update: delay,
                                    false);

                            // 记忆起点 offs
                            var old_offs = _global_offs;
                            // 验证向右移动插入符
                            var ret = _record.MoveByOffs(_global_offs, 1, out HitInfo info);
                            if (ret != 0)
                                break;

                            if (info.Offs > old_offs)
                            {
                                // 删除一个或者多个字符

                                if (replace)
                                    ReplaceText(old_offs,
                                        info.Offs,
                                        new string(PaddingChar, info.Offs - old_offs),
                                        delay_update: delay,
                                        false);
                                else
                                    ReplaceText(old_offs,
                                        info.Offs,
                                        "",
                                        delay_update: delay,
                                        false);
                            }

                            // TODO: 严格来说 Delete 时候插入符也需要重新 MoveCaret()
#endif

            }

            return false;
        }

        // 删除当前位置一个字符
        // 对于泰文来说，要能一次删除连续的有关联的两个字符
        public virtual bool ProcessDeleteChar(HitInfo info,
            DeleteKeyStyle style,
            bool delay)
        {
            Debug.Assert(this._caret_offs == info.Offs);

            int char_count = 1;
            {
                var save_info = info.Clone();
                // 记忆起点 offs
                var old_offs = info.Offs;
                // 移动插入符
                var ret = _record.MoveByOffs(info.Offs, 1, out HitInfo temp_info);
                if (ret != 0)
                {
                    return false;
                }
                char_count = temp_info.Offs - old_offs;
            }

            // 删除多于一个 char
            if (char_count > 1)
            {
                ReplaceText(info.Offs,
    info.Offs + char_count,
    "",
    delay_update: delay,
    auto_adjust_caret_and_selection: false);
                SetCaret(HitByCaretOffs(info.Offs));
                return true;
            }

            var input_info = this._record.GetDeleteInfo(info,
                style,
    PadWhileEditing ? PaddingChar : (char)0);
            if (input_info.Start == input_info.End
                && input_info.Text == "")
            {
                return false;
            }
            else
            {
                ReplaceText(input_info.Start,
                    input_info.End,
                    input_info.Text,
                    delay_update: delay,
                    auto_adjust_caret_and_selection: false);
            }
            // 插入符 offs 需要特殊调整
            if (input_info.Caret != -1)
            {
                MoveCaret(HitByCaretOffs(input_info.Caret));
            }

            // 2026/1/11
            // 把 selection start end 设置为 _caretInfo.CaretOffs
            ChangeSelection(() =>
            {
                this._selectOffs1 = _caretInfo.Offs;
                this._selectOffs2 = _caretInfo.Offs;
            });
            return true;
#if OLD
            // TODO: 改写函数，单独执行 delete char 功能
            var replace = this._record.GetReplaceMode(info,
                "delete",
                PaddingChar,
                out string fill_content);

            if (string.IsNullOrEmpty(fill_content) == false)
                ReplaceText(_global_offs,
                    _global_offs,
                    fill_content,
                    delay_update: delay,
                    false);

            // 记忆起点 offs
            var old_offs = info.Offs;
            // 验证向右移动插入符
            var ret = _record.MoveByOffs(info.Offs, 1, out HitInfo current_info);
            if (ret != 0)
                return false;

            if (current_info.Offs > old_offs)
            {
                // 删除一个或者多个字符

                if (replace)
                    ReplaceText(old_offs,
                        current_info.Offs,
                        new string(PaddingChar, current_info.Offs - old_offs),
                        delay_update: delay,
                        false);
                else
                    ReplaceText(old_offs,
                        current_info.Offs,
                        "",
                        delay_update: delay,
                        false);
            }
            return true;
#endif
        }

        bool DeleteSelectionText()
        {
            if (HasSelection() == false)
                return false;
            var block_start = Math.Min(this._selectOffs1, this._selectOffs2);
            SoftlyRemoveSelectionText();    // 注意这个函数会改变传入的 info 参数

            // 有时候在保护区域 SoftlyRemoveSelectionText() 之后还残留了块定义，需要先消除，避免影响后面的击键处理
            SetCaret(HitByCaretOffs(block_start));
            return true;
        }

        // 处理输入字符
        public virtual bool ProcessInputChar(char ch, HitInfo info)
        {
            Debug.Assert(this._caret_offs == info.Offs);

            if (ch == '\b')
                return false;   // ProcessBackspaceChar() 应该处理过了，这里避免重复处理
            if (!(ch >= 32 || ch == '\r' || ch == 31))
                return false;

            // 检测键盘输入速度
            var delay = _keySpeedDetector.Detect();

            if (DeleteSelectionText())
            {
                info = _caretInfo; // 更新 info 参数
            }

            var action = "input";
            if (ch == '\r')
            {
                return ProcessInputReturnChar(ch, info, delay);
#if REMOVED
                ch = (char)0x1e;
                action = "return";

                // 如果在头标区的末尾，则调整为下一字符开头，插入一个字段结束符
                if (info.ChildIndex == 0 && info.TextIndex >= 24)
                {
                    // 插入一个字符
                    ReplaceText(_global_offs,
                        _global_offs,
                        ch.ToString(),
                        delay_update: delay,
                        false);
                    // 修改后，插入符定位到头标区下一字段的开头
                    MoveCaret(HitByGlobalOffs(24 + 1, -1));
                }
                else if (info.ChildIndex == 0 && info.TextIndex < 24)
                {
                    // 在头标区内回车，补足空格
                    var fill_char_count = 24 - info.TextIndex;
                    var fragment = new string('_', fill_char_count);
                    // 插入一个字符
                    ReplaceText(_global_offs,
                        _global_offs,
                        fragment,
                        delay_update: delay,
                        false);
                    // 修改后，插入符定位到头标区下一字段的开头
                    MoveCaret(HitByGlobalOffs(24 + 1, -1));

                }
                else
                {
                    // (已经优化) 如果在一个字段末尾 caret 位置插入回车，
                    // 可以优化为先向后移动 1 char，然后插入回车，这样导致更新的数据更少

                    // 插入一个字符
                    ReplaceText(_global_offs,
                        _global_offs,
                        ch.ToString(),
                        delay_update: delay,
                        false);

                    // 向前移动一次 Caret
                    MoveGlobalOffsAndBlock(1);
                }

                return true;
#endif
            }
            else if (ch == '\\')
            {
                ch = KERNEL_SUBFLD;
            }

#if OLD_VERSION
            // TODO: 检查覆盖输入字符后，当前相关 Region 是否有足够的字符。如果不足则需要补足。
            var replace = this._record.GetReplaceMode(_caretInfo,
                action,
                PaddingChar,
                out string fill_content);
            if (string.IsNullOrEmpty(fill_content) == false)
                ReplaceText(_global_offs,
                    _global_offs,
                    fill_content,
                    delay_update: delay,
                    false);
            if (replace)
                ReplaceText(_global_offs,
                    _global_offs + 1,
                    ch.ToString(),
                    delay_update: delay,
                    false);
            else
                ReplaceText(_global_offs,
                    _global_offs,
                    ch.ToString(),
                    delay_update: delay,
                    false);
#endif

            return ProcessInputNormalChar(ch, info, delay);
#if REMOVED
            var input_info = this._record.GetInputInfo(_caretInfo,
                ch,
                PadWhileEditing ? PaddingChar : (char)0);
            ReplaceText(input_info.Start,
    input_info.End,
    input_info.Text,
    delay_update: delay,
    false);
            // 插入符 offs 需要特殊调整
            if (input_info.Caret != -1)
            {
                SetGlobalOffs(input_info.Caret);
                MoveCaret(HitByGlobalOffs(input_info.Caret));
            }

            // 向前移动一次 Caret
            MoveGlobalOffsAndBlock(1);
            // e.Handled = true;
            return true;
#endif
        }

        public virtual void SplitPadding(
            ref string left,
            ref string right)
        {
            if (left.Length <= 5)
            {
                if (left.Length == 3)
                {
                    var field_name = left.Substring(0, 3);
                    if (MarcField.IsControlFieldName(field_name))
                        goto SKIP;
                }
                left = left.PadRight(5, PaddingChar);
            }

        SKIP:
            if (right.Length <= 5)
            {
                if (right.Length == 3)
                {
                    var field_name = right.Substring(0, 3);
                    if (MarcField.IsControlFieldName(field_name))
                        return;
                }
                right = right.PadRight(5, PaddingChar);
            }
        }

        // 不包含字段结束符
        void GetLeftRight(out string left,
            out string right)
        {
            var infos = this._record.LocateFields(_caret_offs, _caret_offs);
            if (infos == null || infos.Length == 0)
            {
                left = "";
                right = "";
                return;
            }
            var info = infos[0];
            var index = info.Index;
            var field = this._record.GetField(index);
            //var start_offs = _global_offs - info.StartLength;
            var text = field.MergePureText();
            left = text.Substring(0, info.StartLength);
            right = text.Substring(info.StartLength);
        }

        // 处理输入回车字符
        public virtual bool ProcessInputReturnChar(char ch,
            HitInfo info,
            bool delay)
        {
            Debug.Assert(this._caret_offs == info.Offs);

            if (ch == '\r')
            {
                ch = (char)0x1e;

                // 如果在头标区的末尾，则调整为下一字符开头，插入一个字段结束符
                if (info.ChildIndex == 0 && info.TextIndex >= 24)
                {
                    if (PadWhileEditing)
                    {
                        // 插入一个回车字符后，新的字段就内容为空，需要插入 5 个空格
                        ReplaceText(_caret_offs,
    _caret_offs,
    new string(PaddingChar, 5) + ch.ToString(),
    delay_update: delay,
    auto_adjust_caret_and_selection: false);
                    }
                    else
                    {
                        // 插入一个字符
                        ReplaceText(_caret_offs,
                            _caret_offs,
                            ch.ToString(),
                            delay_update: delay,
                            auto_adjust_caret_and_selection: false);
                    }

                    // 修改后，插入符定位到头标区下一字段的开头
                    // MoveCaret(HitByCaretOffs(24 + 1, -1));
                    SetCaret(HitByCaretOffs(24 + 1, -1));

                    /*
                    // 2026/1/11
                    // 把 selection start end 设置为 _caretInfo.CaretOffs
                    ChangeSelection(() =>
                    {
                        this._selectOffs1 = _caretInfo.Offs;
                        this._selectOffs2 = _caretInfo.Offs;
                    });
                    */
                }
                else if (info.ChildIndex == 0 && info.TextIndex < 24)
                {
                    // 在头标区内回车，补足空格
                    var fill_char_count = 24 - info.TextIndex;
                    // TODO: 头标区是否使用特殊字符填充?
                    var fragment = new string(PaddingChar, fill_char_count);
                    // 插入一个字符
                    ReplaceText(_caret_offs,
                        _caret_offs,
                        fragment,
                        delay_update: delay,
                        auto_adjust_caret_and_selection: false);

                    if (PadWhileEditing)
                    {
                        // 观察插入点以后的新字段内容，如果不足 3 或 5 字符需要补齐
                        var pad_info = PaddingRight(24);
                        if (string.IsNullOrEmpty(pad_info.Text) == false)
                        {
                            ReplaceText(24 + pad_info.Offs, // 插入在原有字符后面
    24 + pad_info.Offs,
    pad_info.Text,
    delay_update: delay,
    auto_adjust_caret_and_selection: false);
                        }
                    }

                    // 修改后，插入符定位到头标区下一字段的开头
                    // TODO: 使用 MoveGlobalOffsAndBlock(left.Length - old_left.Length + 1);

                    //MoveCaret(HitByCaretOffs(24 + 1, -1));
                    MoveCaret(HitByCaretOffs(24 + 1, -1));

                    /*
                    // 2026/1/11
                    // 把 selection start end 设置为 _caretInfo.CaretOffs
                    ChangeSelection(() =>
                    {
                        this._selectOffs1 = _caretInfo.Offs;
                        this._selectOffs2 = _caretInfo.Offs;
                    });
                    */
                }
                else
                {
                    // *** 其它普通字段内插入

                    if (PadWhileEditing)
                    {
                        // 获得插入点以后直到字段结束符这一段的文字内容，
                        // 触发一个函数，让它决定是否要补充字符，如何补充。
                        GetLeftRight(out string left,
                            out string right);
                        // 注意 right 不包含字段结束符
                        var old_left = left;
                        var old_right = right;
                        SplitPadding(
                ref left,
                ref right);
                        // 原来的 text
                        var old_text = old_left + old_right;
                        var new_text = left + Metrics.FieldEndCharDefault + right;

                        // TODO: 优化，从两端向中间寻找，找到中间不一样的一段，只替换不一样的一段

                        ReplaceText(_caret_offs - old_left.Length,
        _caret_offs + old_right.Length,    // + 1,
        new_text,
        delay_update: delay,
        auto_adjust_caret_and_selection: false);

                        // 向前移动 Caret
                        DeltaCaretOffsAndSelectionOffs(left.Length - old_left.Length + 1);
                    }
                    else
                    {
                        // (已经优化) 如果在一个字段末尾 caret 位置插入回车，
                        // 可以优化为先向后移动 1 char，然后插入回车，这样导致更新的数据更少
                        // 插入一个字符
                        ReplaceText(_caret_offs,
                            _caret_offs,
                            ch.ToString(),
                            delay_update: delay,
                            auto_adjust_caret_and_selection: false);

                        // 向前移动一次 Caret
                        DeltaCaretOffsAndSelectionOffs(1);
                    }
                }

                return true;
            }
            return false;
        }

        // 处理输入普通字符。在头标区和其它字段的字段名、指示符位置，为覆盖字符；其它位置为插入字符
        public virtual bool ProcessInputNormalChar(char ch,
            HitInfo info,
            bool delay)
        {
            Debug.Assert(this._caret_offs == info.Offs);

            var input_info = this._record.GetInputInfo(info,
                ch,
                PadWhileEditing ? PaddingChar : (char)0);
            int save_offs = this._caret_offs;
            ReplaceText(input_info.Start,
                input_info.End,
                input_info.Text,
                delay_update: delay,
                auto_adjust_caret_and_selection: false);

            Debug.Assert(this._caret_offs == save_offs);

            // 插入符 offs 需要特殊调整
            if (input_info.Caret != -1)
            {
                // MoveCaret(HitByCaretOffs(input_info.Caret));
                SetCaret(HitByCaretOffs(input_info.Caret));
            }
            else
            {
                SetCaret(HitByCaretOffs(this._caret_offs));
            }

            // 向前移动一次 Caret
            DeltaCaretOffsAndSelectionOffs(1);
            //Debug.Assert(this._caretInfo.Offs == save_offs + 1);
            //Debug.Assert(this._caret_offs == save_offs + 1);
            return true;
        }

        class PaddingInfo
        {
            // Text 要插入的偏移位置。注意是从测试起点计算
            public int Offs { get; set; }
            // 要插入的文字内容
            public string Text { get; set; }
        }

        // 观察插入点以后的新字段内容，如果不足 3 或 5 字符需要补齐
        PaddingInfo PaddingRight(int offs)
        {
            var test_length = 5;    // 测试用字符串的最小长度
            // right_count 有可能越过最大长度
            var test_string = this._record.MergeText(offs, offs + test_length);
            // 寻找字段结束符
            var index = test_string.IndexOf(Metrics.FieldEndCharDefault);
            if (index == -1 && test_string.Length >= test_length)
                return new PaddingInfo();
            if (index == -1)
                index = 0;
            if (index < 3)
                return new PaddingInfo
                {
                    Offs = index,
                    Text = new string(PaddingChar, 5 - index),
                };
            // 检查字段名是否为控制字段
            if (MarcField.IsControlFieldName(test_string.Substring(0, 3)))
            {
                // 控制字段，3个字符已经足够
                return new PaddingInfo();
            }
            // 否则需要至少 5 字符
            if (index < 5)
                return new PaddingInfo
                {
                    Offs = index,
                    Text = new string(PaddingChar, 5 - index),
                };
            return new PaddingInfo();
        }

#if REMOVED
        // 有几种观察。
        // 第一种，为了插入回车。
        // 1) 如果 offs < 24，左边(一直到整个开头)不足 24 字符，则左边补足空格。注: 中间即便有字段结束符也被当作头标区内容，因为头标区是用位置和长度定义的
        // 2) 右边不足 3 或者 5 字符，则右边补足空格。到底是 3 还是 5 字符，取决于前三字符是否为控制字段名，控制字段只要求 3 字符足够，否则要 5 字符才够
        // 第二种，为了插入其它普通字符
        // 要求插入点左边至少有 5 字符，如果不足，则从 offs 点开始插入足够的字符。到底是 3 或 5 要看字段名
        // 插入之后，注意保留插入的字符数信息，便于调用者调整插入符位置
        public string Padding(int offs, 
            int left_count,
            int right_count)
        {
            int total_length = this.TextLength;
            if (left_count > offs)
                left_count = offs;
            if (offs + right_count > total_length)
                right_count = total_length - offs;
            var start = offs - left_count;
            var end = offs + right_count;
            var text = this.MergeText(start, end);


            // 观察左边是否不足 5 字符
        }
#endif


        #endregion

        #region Edit Commands

        // 插入一个子字段符号
        public bool InsertSubfieldChar(bool auto_off_ime = true)
        {
            var ret = ProcessInputChar(Metrics.SubfieldCharDefault, _caretInfo);
            if (auto_off_ime)
                OpenIME(false);
            return ret;
        }

        // 对所有字段排序
        public bool SortFields()
        {
            var old_text = this._record.MergeText();
            var ret = this._record.SortFields();
            if (ret == true)
            {
                this.Invalidate();
                this.Select(0, 0, 0);
                // 进入编辑历史
                _history.Memory(new EditAction
                {
                    Start = 0,
                    End = old_text.Length,
                    OldText = old_text,
                    NewText = this._record.MergeText(),
                });
            }
            return ret;
        }

        // 折行。即，默认的输入回车的效果
        public bool BreakText()
        {
            // 获得当前字段的字段名和指示符
            int index = this.CaretFieldIndex;
            if (index >= this.MarcRecord.FieldCount)
                return false;
            if (index == 0)
                return ProcessInputChar('\r', _caretInfo);

            var field = this.GetDomRecord().GetField(index);
            var prefix = field.Name + field.Indicator;
            /*
            var infos = this.MarcRecord.LocateFields(this.CaretOffset, this.CaretOffset);
            if (infos.Length == 0)
                return false;
            var info = infos[0];
            */
            this.ReplaceText(this.CaretOffset,
                this.CaretOffset,
                new string(Metrics.FieldEndCharDefault, 1) + prefix,
                false);
            return true;
        }

        // 将插入符移动到下一个字段的内容第一字符位置
        public bool ToNextField()
        {
            var index = this.CaretFieldIndex;
            if (index < this._record.FieldCount - 1)
                index++;
            else
            {
                var offs = this._record.TextLength;
                //this.SetCaretOffs(offs);
                MoveCaret(HitByCaretOffs(offs, 0));
                return true;
            }
            if (this._record.GetFieldOffsRange(index,
                out int start,
                out int end) == false)
                return false;
            var field = this._record.GetField(index);
            if (field.IsControlField)
                start += 3;
            else
                start += 5;
            start = Math.Min(end, start);
            //this.SetCaretOffs(start);
            MoveCaret(HitByCaretOffs(start, 0));
            return true;
        }

        // 根据指定的 index 删除若干字段。可以出现提示对话框
        public virtual bool DeleteFields(IEnumerable<int> field_indices,
            bool show_dialog = true)
        {
            var fields = new List<MarcField>();
            foreach (var index in field_indices)
            {
                if (CanDeleteField(index) == false)
                {
                    return false;
                }
                fields.Add(this._record.GetField(index));
            }

            if (show_dialog)
            {
                using (var dlg = new DeletingFieldDialog())
                {
                    dlg.MessageText = $"确实要删除下列 {field_indices.Count()} 个字段?";
                    dlg.StartPosition = FormStartPosition.CenterParent;
                    dlg.PaintPreview += (s, e) =>
                    {
                        var handle = e.Graphics.GetHdc();
                        using (var hdc = new SafeHDC(handle))
                        {
                            int x = -_fieldProperty.SolidX;
                            int y = 0;
                            foreach (var field in fields)
                            {
                                field.PaintBackAndBorder(hdc,
                                    x,
                                    y,
                                    e.ClipRectangle,
                                    true);
                                field.Paint(this._context,
                                    hdc,
                                    x,
                                    y,
                                    e.ClipRectangle,
                                    -1,
                                    -1,
                                    1);
                                y += field.GetPixelHeight();
                            }
                        }
                    };
                    if (dlg.ShowDialog(this) == DialogResult.Cancel)
                    {
                        return false;
                    }
                }
            }

            // 倒序，从后面开始删除
            var list = field_indices.ToList();
            list.Sort((a, b) => b - a);
            foreach (var index in list)
            {
                if (this.DeleteField(index, 1, false) == false)
                {
                    return false;
                }
            }
            return true;
        }

        public bool CanDeleteField(int index, int count = 1)
        {
            if (this._record.VerifyIndexAndLength(index, count, false) == false)
                return false;
            if (index == 0)
            {
                if (this._record.FieldCount != 1
                    && count < this._record.FieldCount)
                {
                    return false;
                    // throw new ArgumentException($"不允许删除头标区，除非头标区已经是最后一个字段");
                }
            }
            return true;
        }

        public bool DeleteField(int index, int count = 1,
            bool throw_exception = true)
        {
            if (this._record.VerifyIndexAndLength(index, count, throw_exception) == false)
                return false;

            if (index == 0)
            {
                if (this._record.FieldCount != 1
                    && count < this._record.FieldCount)
                {
                    if (throw_exception)
                    {
                        throw new ArgumentException($"不允许删除头标区，除非头标区已经是最后一个字段");
                    }

                    return false;
                }
            }
            // 先获得这些字段的 offs 范围。然后一次性删除
            this._record.GetContiguousFieldOffsRange(index,
                count,
                out int start,
                out int end);
            this.ReplaceText(start,
                end,
                null,   // 彻底删除
                delay_update: false);
            return true;
        }

        private bool _selectCurrentFull = true;
        // 选择当前插入符所在子字段为文字块
        // 除了子字段，字段名，指示符，或者子字段左侧的无主文字，都可以被选择
        public bool SelectCaretSubfield(bool toggle = true)
        {
            var field_info = this._record.LocateFields(_caret_offs, _caret_offs).FirstOrDefault();
            if (field_info == null)
                return false;
            var index = field_info.Index;
            if (index >= this._record.FieldCount)
                return false;
            // 在头标区最后一个字符右边，依然当作头标区
            if (index == 1 && this._caretInfo.ChildIndex == 0)
            {
                field_info.StartLength = 24;
                index = 0;
            }
            var field = this._record.GetField(index);
            var subfield_info = field.GetSubfieldBoundsEx(field_info.StartLength, true);
            if (this._record.GetFieldOffsRange(index,
                out int start,
                out _) == false)
                return false;
            int offs_start = start + (_selectCurrentFull || toggle == false ? subfield_info.StartOffs : subfield_info.ContentStartOffs);
            int offs_end = start + subfield_info.EndOffs;
            this.Select(offs_start, offs_end, _caret_offs, 0);
            if (toggle)
                _selectCurrentFull ^= true;
            return true;
        }

        public virtual bool RawCut()
        {
            if (this._readonly)
                return false;

            if (HasSelection() == false)
                return false;
            var start = Math.Min(this.SelectionStart, this.SelectionEnd);
            var length = Math.Abs(this.SelectionEnd - this.SelectionStart);
            var text = this.Content.Substring(start, length);
            ClipboardSetText(text);
            RawRemoveBolckText();
            return true;
        }

        public virtual bool SoftlyCut()
        {
            if (this._readonly)
                return false;

            if (HasSelection() == false)
                return false;
            /*
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
            var text = _record.MergeText(start, start + length);
            */

            ClipboardSetText(GetSelectedContent());
            SoftlyRemoveSelectionText();
            return true;
        }

        public virtual bool Copy()
        {
            if (HasSelection() == false)
                return false;
            /*
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
            var text = this.Content.Substring(start, length);
            Clipboard.SetText(text);
            // RawRemoveBolckText();
            return true;
            */
            ClipboardSetText(GetSelectedContent());
            return true;
        }

        // 设置文字到 Windows 剪贴板。具有失败后自动重试能力
        public static bool ClipboardSetText(string text)
        {
            for (int i = 0; i < 10; i++)
            {
                try
                {
                    Clipboard.SetText(text);
                    return true;
                }
                catch
                {
                    Thread.Sleep(20);
                }
            }
            return false;
        }

        // 获得文字块中的文字
        public string GetSelectedContent()
        {
            if (HasSelection() == false)
                return "";
            var start = Math.Min(this.SelectionStart, this.SelectionEnd);
            var length = Math.Abs(this.SelectionEnd - this.SelectionStart);
            return this._record.MergeText(start, start + length);
        }

        public bool CanCut()
        {
            return this.HasSelection() && this.ReadOnly == false;
        }

        public bool CanPaste()
        {
            if (this._readonly)
                return false;

            // 检查剪贴板中是否有文本
            return Clipboard.ContainsText();
        }

        // 硬粘贴
        public virtual bool RawPaste(string text = null)
        {
            if (this._readonly)
                return false;

            if (text == null)
                text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
                return false;
            var start = Math.Min(this.SelectionStart, this.SelectionEnd);
            var length = Math.Abs(this.SelectionEnd - this.SelectionStart);
            this.ReplaceText(start,
                start + length,
                text,
                delay_update: false);
            this.Select(start, start + text.Length, start + 1, -1);
            return true;
        }

        // 软粘贴。会保护目标位置固定长内容的字符数不变
        // parameters:
        //      text    要粘贴进入的文本。
        //              如果为 null，表示自动从 Windows 剪贴板中获取粘贴
        public virtual bool SoftlyPaste(string text = null)
        {
            if (this._readonly)
                return false;

            if (text == null)
                text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
                return false;
            var start = Math.Min(this.SelectionStart, this.SelectionEnd);
            var length = Math.Abs(this.SelectionEnd - this.SelectionStart);
            //var old_text = _record.MergeText(start, start + length);

            // 获得即将被替换部分内容的 mask 形态
            var old_mask_text = _record.MergeTextMask(start, start + length);
            //Debug.Assert(old_text.Length == old_mask_text.Length);

            // 压缩。相当于先删除一次
            var compressed = MarcRecord.CompressMaskText(old_mask_text, PaddingChar);

            string result = "";
            // compress 被 text 内容置换，最后长度不少于 compressed 原有长度
            if (text.Length >= compressed.Length)
                result = text;
            else
                result = text + compressed.Substring(text.Length);

            /*
             * 旧算法被弃用
            var result = SoftReplace(old_mask_text, text, (char)0x01);
            */
            this.ReplaceText(start,
                start + old_mask_text.Length,
                result,
                delay_update: false);
            this.Select(start, start + result.Length, start + 1, -1);
            return true;
        }

        // *** 旧算法被弃用。原来用于 SoftlyPaste
        // TODO!!! mask char 规则变了。需要考虑 mask text 中间用字段结束符分隔为多个片段，单独处理
        // 利用掩码，指导进行字符替换
        // mask char 规则: 0x01~0x03 表示字段名位置, 0x04~0x05 表示指示符位置, 0x06 表示头标区位置(最多 24 个字符都是这个值)
        public static string SoftReplace(
            string old_mask_text,
            string new_text,
            char mask_char = (char)0x01)
        {
            if (old_mask_text.Length == 0)
                return new_text;

            int i = 0;
            int j = 0;

            StringBuilder result = new StringBuilder();

            // 对于掩码字符串，如果第一字符不是 mask char，则找到一个连续的普通字符范围，替换为 new_text；
            // 连续范围后面的部分里面的 mask char 都被替换为空格，其余字符被丢弃
            while (i < old_mask_text.Length)
            {
                // 找到第一个 mask char
                for (; i < old_mask_text.Length; i++)
                {
                    if (old_mask_text[i] == mask_char)
                    {
                        break;
                    }
                }

                // 如果始终没有找到 mask char，则把 new_text 中余下的部分全部输出
                if (i >= old_mask_text.Length)
                {
                    break;
                }

                // 找到了 mask char
                // 针对这一连续范围的 mask char，从 new_text 中取出连续的字符输出
                for (; i < old_mask_text.Length; i++)
                {
                    if (old_mask_text[i] == mask_char)
                    {
                        result.Append(GetChar());
                    }
                    else
                    {
                        break;
                    }
                }
            }
            result.Append(GetRest());
            return result.ToString();

            string GetRest()
            {
                // 返回 new_text 中余下的部分
                if (j < new_text.Length)
                {
                    return new_text.Substring(j);
                }

                return "";
            }

            char GetChar()
            {
                // 返回 new_text 中的一个字符
                if (j < new_text.Length)
                {
                    return new_text[j++];
                }

                return ' '; // 如果没有字符了，则返回空格
            }
        }


        public void SelectAll()
        {
            _selectOffs1 = 0;
            _selectOffs2 = this._record.TextLength;
            this.Invalidate();
        }


        public bool CanUndo()
        {
            if (this._readonly)
            {
                return false;
            }

            return _history.CanUndo();
        }

        public bool CanRedo()
        {
            if (this._readonly)
            {
                return false;
            }

            return _history.CanRedo();
        }

        public bool Undo()
        {
            if (this._readonly)
            {
                return false;
            }

            var action = _history.Back();
            if (action == null)
            {
                return false;
            }

            var start = Math.Min(action.Start, action.End);
            // var end = Math.Max(action.End, action.Start);
            var end = start + action.NewText.Length;
            ReplaceText(start,
                end,
                action.OldText,
                delay_update: false,
                auto_adjust_caret_and_selection: true,   // 这里必须要自动调整选择范围，因为这一次 undo 插入的内容可能会把原有的 selection range 挤到后面。如果不自动调整，当后面 InvalidateSelectionRegion() 的时候就会造成刷新不干净，会残留 selection 图像
                add_history: false);
            Select(start, start + action.OldText.Length, start);
            EnsureCaretVisible();
            return true;
        }

        public bool Redo()
        {
            if (this._readonly)
            {
                return false;
            }

            var action = _history.Forward();
            if (action == null)
            {
                return false;
            }

            var start = Math.Min(action.Start, action.End);
            var end = start + action.OldText.Length;
            ReplaceText(start,
                end,
                action.NewText,
                delay_update: false,
                auto_adjust_caret_and_selection: true,
                add_history: false);
            Select(start, start + action.NewText.Length, start);
            return true;
        }

        // 删除当前插入符所在的子字段
        // 如果插入符不在任何子字段上，则不做删除，返回 null
        // return:
        //      null    没有找到这样的子字段
        //      被删除的子字段内容
        public string DeleteCaretSubfield()
        {
            var offs = _caret_offs;

            // 根据全局偏移找到字段
            var ret = this._record.LocateFields(offs, offs);
            if (ret.Length == 0)
            {
                return null;
            }
            // 字段下标
            var index = ret[0].Index;
            var field_offs = offs - ret[0].StartLength;
            var offs_in_field = ret[0].StartLength;

            var field = this._record.GetField(index);
            var info = field.GetSubfieldBounds(
                offs_in_field);
            if (info.Found == false)
            {
                return null;
            }

            this._record.GetFieldOffsRange(
                ret[0].Index,
                out int field_start,
                out int field_end);

            var replace_result = ReplaceText(field_offs + info.StartOffs,
                field_offs + info.EndOffs,
                "",
                delay_update: false);
            return replace_result?.ReplacedText;
        }

        #endregion

        #region 快捷键和上下文菜单

        private List<CommandItem> _commands = new List<CommandItem>();

        IEnumerable<CommandItem> Commands
        {
            get
            {
                if (_commands.Count == 0)
                {
                    // 初始化命令集合
                    _commands.AddRange(GetCommandItems());
                    // _commands.AddRange(GetTestingItems());
                }
                return _commands;
            }
        }

        public virtual IEnumerable<CommandItem> GetCommandItems()
        {
            return new List<CommandItem>()
            {
                new CommandItem()
                {
                    Caption="撤销(&U)",
                    KeyData=Keys.Control | Keys.Z,
                    Handler=(s,e) => this.Undo(),
                    CanExecute=()=> this.CanUndo(),
                },
                new CommandItem()
                {
                    Caption="重做(&R)",
                    KeyData=Keys.Control | Keys.Y,
                    Handler=(s,e) => this.Redo(),
                    CanExecute=()=> this.CanRedo(),
                },
                new CommandItem()
                {
                    Caption="-",
                },
                new CommandItem()
                {
                    Caption="剪切(&T)",
                    KeyData=Keys.Control | Keys.X,
                    Handler=(s,e) =>this.SoftlyCut(),
                    CanExecute=()=> this.CanCut(),
                },
                new CommandItem()
                {
                    Caption="复制(&C)",
                    KeyData=Keys.Control | Keys.C,
                    Handler=(s,e) => this.Copy(),
                    CanExecute=()=> this.HasSelection(),
                },
                new CommandItem()
                {
                    Caption="粘贴(&V)",
                    KeyData=Keys.Control | Keys.V,
                    Handler=(s,e) => this.SoftlyPaste(),
                    CanExecute=()=> this.CanPaste(),
                },
                new CommandItem() { Caption="-" },
                new CommandItem()
                {
                    Caption="原始剪切",
                    // 不设置快捷键（若需要可添加）
                    Handler=(s,e) => this.RawCut(),
                    CanExecute=()=> this.CanCut(),
                },
                new CommandItem()
                {
                    Caption="原始粘贴",
                    Handler=(s,e) => this.RawPaste(),
                    CanExecute=()=> this.CanPaste(),
                },

                new CommandItem() { Caption="-" },
                new CommandItem()
                {
                    Caption="归一化(&N)",
                    KeyData=Keys.Control | Keys.N,
                    Handler=(s,e) => this.NormalizeSelectedText(),
                    CanExecute=()=> this.HasSelection(),
                },
                new CommandItem()
                {
                    Caption="显示十六进制",
                    Handler=(s,e) => this.DisplayHex(),
                    CanExecute=()=> this.HasSelection(),
                },
                new CommandItem() { Caption="-" },

                new CommandItem()
                {
                    Caption="全选(&A)",
                    KeyData=Keys.Control | Keys.A,
                    Handler=(s,e) => this.SelectAll(),
                    CanExecute=()=> true,
                },

                new CommandItem() { Caption="-" },

                new CommandItem()
                {
                    Caption="选择子字段",
                    KeyData=Keys.Control | Keys.B,
                    Handler=(s,e) => {
                        this.SelectCaretSubfield();
                    },
                    CanExecute=()=> true,
                },

                new CommandItem()
                {
                    Caption="删除当前子字段",
                    KeyData=Keys.Shift | Keys.Delete,
                    Handler=(s,e) => {
                        var ret = this.DeleteCaretSubfield() != null;
                        TriggerEvenArgs.SetHandled(e, ret);
                        },
                    CanExecute=()=> true,
                },
                new CommandItem()
                {
                    Caption="整理字段顺序",
                    KeyData=Keys.Control | Keys.Q,
                    Handler=(s,e) => this.SortFields(),
                    CanExecute=()=> true,
                },
                new CommandItem()
                {
                    Caption="插入子字段符号",
                    KeyData=Keys.Control | Keys.I,
                    Handler=(s,e) => this.InsertSubfieldChar(),
                    CanExecute=()=> true,
                },
                new CommandItem() { Caption="-" },
                // 突出显示空格

                new CommandItem()
                {
                    Refresh = (o)=>{
                        o.Checked = this.HighlightBlankChar != ' ';
                        return null;
                    },
                    Caption="突出显示空格",
                    Handler= (s, e)=>{
                        if (this.HighlightBlankChar == ' ')
                        {
                            this.HighlightBlankChar = '·';
                        }
                        else
                        {
                            this.HighlightBlankChar =' ';
                        }
                    },
                },
                // 
                new CommandItem()
                {
                    Caption="视觉风格(&V) ...",
                    Handler=(s,e) => SettingVisualStyle(),
                    CanExecute=()=> true,
                },
                new CommandItem()
                {
                    Caption="字体(&F) ...",
                    Handler=(s,e) => SettingFont(),
                    CanExecute=()=> true,
                },

                new CommandItem()
                {
                    Caption="属性(&P) ...",
                    KeyData=Keys.Control | Keys.P,
                    Handler=(s,e) => {
                        using(var dlg = new MarcControlDialog.PropertyDialog())
                        {
                            dlg.Instance = this;
                            dlg.ShowDialog(this);
                        }
                    },
                    CanExecute=()=> true,
                },
            };
        }

        public virtual IEnumerable<CommandItem> GetTestingItems()
        {
            return new List<CommandItem>()
            {
                new CommandItem() { Caption="测试子菜单",
                    SubCommands = new List<CommandItem>()
                    {
                    new CommandItem()
                    {
                        Caption="子命令1",
                        KeyData=Keys.Control | Keys.Shift | Keys.D1,
                        Handler=(s,e) => MessageBox.Show("子命令1 被触发"),
                        CanExecute=()=> true,
                    },
                    new CommandItem()
                    {
                        Caption="子命令2",
                        KeyData=Keys.Control | Keys.Shift | Keys.D2,
                        Handler=(s,e) => MessageBox.Show("子命令2 被触发"),
                        CanExecute=()=> true,
                    },
                    },
                },

                new CommandItem()
                {
                    Caption="测试双键击发\tCtrl+K,D",
                    KeyData=Keys.Control | Keys.K,
                    KeyData2=Keys.Control | Keys.D,
                    Handler=(s,e) => MessageBox.Show(this, "双键击发"),
                    CanExecute=()=> true,
                },

                new CommandItem()
                {
                    Caption="测试 Alt",
                    KeyData=Keys.Alt | Keys.K,
                    Handler=(s,e) => MessageBox.Show(this, "Alt+K"),
                    CanExecute=()=> true,
                },

                new CommandItem()
                {
                    Caption="测试 Shift+K\tShift+K",
                    KeyData=Keys.Shift | Keys.K,
                    Handler=(s,e) => MessageBox.Show(this, "Shift+K"),
                    CanExecute=()=> true,
                },

                new CommandItem()
                {
                    Caption="测试 .Tag 携带参数",
                    KeyData= Keys.Control | Keys.B,
                    Tag="这是一个参数",
                    Handler=(s,e) => {
                        var tag = GetItemMenuTag(s);
                        MessageBox.Show(this, (tag as string));
                    },
                    CanExecute=()=> true,
                },
            };
        }

        /*
        public virtual bool AfterProcessCmdKey(ref Message msg, Keys keyData)
        {
            return true;
        }
        */

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Debug.WriteLine($"keyData={keyData.ToString()}");
            var ret = KeyTriggerCommand(keyData);
            if (ret == true)
            {
                // return AfterProcessCmdKey(ref msg, keyData);
                return ret;
            }

            // 返回结果:
            //     true if the character was processed by the control; otherwise, false.
            return base.ProcessCmdKey(ref msg, keyData);
        }

        public event KeyEventHandler KeyTrigger;

        // 前一次命中(双键) CommandItem 的击键
        // 如果为 None，表示没有处于等待第二个键的状态
        Keys _firstHitKey = Keys.None;

        // 键盘触发命令
        public virtual bool KeyTriggerCommand(Keys keyData)
        {
            if (KeyTrigger != null)
            {
                var e = new KeyEventArgs(keyData);
                KeyTrigger(this, e);
                if (e.Handled)
                    return true;
            }

            var commands = this.Commands;
            if (commands == null)
            {
                return false;
            }

            // 把当前修饰键合并入 keyData，防止 Ctrl 先到、字母键后到导致识别失败
            // var currentModifiers = ModifierKeys & (Keys.Control | Keys.Shift | Keys.Alt);
            // var combinedKey = keyData | currentModifiers;

            var combinedKey = keyData;

            // Debug.WriteLine($"combinedKey={combinedKey.ToString()}");

            // 展平后遍历（包括所有下级），匹配并触发第一个符合的命令
            foreach (var command in commands.Flatten(c => c.SubCommands))
            {
                // Debug.WriteLine($"cmd KeyData={command.KeyData.ToString()}, KeyData2={command.KeyData2.ToString()} caption={command.GetCaption?.Invoke()}");

                // 双键连续击发（第二键也要用合并后的值比较）
                if (command.KeyData2 == combinedKey
                    && _firstHitKey == command.KeyData)
                {
                    return Trigger(command);
                }

                if (command.KeyData == combinedKey)
                {
                    if (command.KeyData2 != Keys.None)
                    {
                        // 进入等待第二个键的状态；保存第一个键（保存合并后的值）
                        _firstHitKey = combinedKey;
                        return false;   // 需要第二个键配合才触发
                    }
                    // 普通键击发
                    return Trigger(command);
                }
            }
            return false;

            bool Trigger(CommandItem item)
            {
                // 为了照顾那些没有专门设置 Handled 的代码，初始值为 true
                var args = new TriggerEvenArgs { Handled = true };

                item.Handler?.Invoke(new ToolStripMenuItem { Tag = item.Tag }, args);
                _firstHitKey = Keys.None;
                return args.Handled;
            }
        }

        public class TriggerEvenArgs : EventArgs
        {
            public bool Handled { get; set; } = true;

            public static void SetHandled(EventArgs e, bool value)
            {
                if (e is TriggerEvenArgs e1)
                {
                    e1.Handled = value;
                }
            }
        }

#if REMOVED
        public virtual bool KeyTriggerCommand(Keys keyData)
        {
            var commands = this.Commands;
            if (commands == null)
                return false;
            // 展平后遍历（包括所有下级），匹配并触发第一个符合的命令
            foreach (var command in commands.Flatten(c => c.SubCommands))
            {
                // 双键连续击发
                if (command.KeyData2 == keyData
                    && _firstHitKey == command.KeyData)
                {
                    Trigger(command);
                    return true;
                }

                if (command.KeyData == keyData)
                {
                    if (command.KeyData2 != Keys.None)
                    {
                        // 进入等待第二个键的状态
                        _firstHitKey = keyData;
                        return false;   // 需要第二个键配合才触发
                    }
                    // 普通键击发
                    Trigger(command);
                    return true;
                }
            }
            return false;

            void Trigger(CommandItem item)
            {
                item.Handler?.Invoke(new ToolStripMenuItem { Tag = item.Tag }, new EventArgs());
                _firstHitKey = Keys.None;
            }
        }
#endif


#if REMOVED
        public bool TriggerAll(IEnumerable<CommandItem> commands,
            Keys keyData)
        {
            foreach (var command in commands)
            {
                if (command.KeyData == keyData)
                {
                    command.Handler?.Invoke(this, null);
                    return true;
                }

                var children = (command.SubCommands);
                if (children != null)
                {
                    if (TriggerAll(children, keyData) == true)
                        return true;
                }
            }
            return false;
        }

#endif

        void AppendMenu(ToolStripItemCollection items,
            IEnumerable<CommandItem> commands)
        {
            foreach (var item in commands)
            {
                CommandItem command = item;
                if (item.Refresh != null)
                {
                    // .Refresh 既可以起到触发刷新的作用，也可以直接返回一个新的 CommandItem，实际创建菜单利用的是这个新的 CommandItem
                    var ret = item.Refresh?.Invoke(item);
                    if (ret == null)
                    {
                        command = item;
                    }
                    else
                    {
                        command = ret;
                    }
                }
                else
                {
                    if (item.GetCaption == null)
                    {
                        continue;
                    }

                    command = item;
                }

                Debug.Assert(command != null);

                var caption = command.GetCaption?.Invoke();
                if (caption == null)
                {
                    continue;
                }

                if (caption.StartsWith("-"))
                {
                    if (caption?.Length > 1)
                    {
                        var text = caption.Substring(1).Trim();
                        var label = new ToolStripLabel(text);
                        items.Add(label);
                        continue;
                    }
                    var sep = new ToolStripSeparator();
                    items.Add(sep);
                    continue;
                }
                var menuItem = new ToolStripMenuItem(caption) { Checked = command.Checked };
                if (caption.Contains("\t"))
                {
                    var parts = caption.Split(new char[] { '\t' }, 2);
                    menuItem.Text = parts[0];
                    menuItem.ShortcutKeyDisplayString = parts[1];
                }
                else if (command.KeyData != Keys.None)
                {
                    try
                    {
                        /*
            if (value != 0 && !ToolStripManager.IsValidShortcut(value))
            {
                throw new InvalidEnumArgumentException("value", (int)value, typeof(Keys));
            }                         * 
                         * */
                        // 
                        //     public static bool IsValidShortcut(Keys shortcut)
                        if (ToolStripManager.IsValidShortcut(command.KeyData))
                        {
                            menuItem.ShortcutKeys = command.KeyData;
                        }
                        else
                        {
                            menuItem.ShortcutKeyDisplayString = command.KeyData.ToString();
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        menuItem.ShortcutKeyDisplayString = $"command.KeyData {command.KeyData.ToString()} error: {ex.Message}";
                    }
                }

                menuItem.Enabled = command.CanExecute?.Invoke() ?? true;
                menuItem.Click += (o1, e1) =>
                {
                    command.Handler?.Invoke(o1, e1);
                };
                menuItem.Tag = command.Tag;
                items.Add(menuItem);

                var children = (command.SubCommands);
                if (children != null)
                {
                    AppendMenu(menuItem.DropDown.Items, children);
                }
            }
        }

        public static object GetItemMenuTag(object sender)
        {
            if (sender is MenuItem)
            {
                return (sender as MenuItem).Tag;
            }
            else if (sender is ToolStripItem)
            {
                return (sender as ToolStripItem).Tag;
            }
            else
            {
                throw new ArgumentException($"无法识别的 sender 类型 '{sender.GetType().ToString()}'");
            }
        }

#if REMOVED
        void PopupMenuOld(Point point)
        {
            ContextMenuStrip contextMenu = new ContextMenuStrip();

            /*
            ToolStripMenuItem subMenuItem = null;
            ToolStripSeparator menuSepItem = null;
            */

            /*
            ToolStripLabel label = new ToolStripLabel("日期范围");
            label.Font = new Font(label.Font, FontStyle.Bold);
            contextMenu.Items.Add(label);
            */

            // Undo
            {
                var menuItem = new ToolStripMenuItem("&Undo");
                menuItem.Enabled = this.CanUndo();
                menuItem.Click += (o1, e1) =>
                {
                    this.Undo();
                };
                contextMenu.Items.Add(menuItem);
            }

            // Redo
            {
                var menuItem = new ToolStripMenuItem("&Redo");
                menuItem.Enabled = this.CanRedo();
                menuItem.Click += (o1, e1) =>
                {
                    this.Redo();
                };
                contextMenu.Items.Add(menuItem);
            }

            // ---
            {
                var sep = new ToolStripSeparator();
                contextMenu.Items.Add(sep);
            }

            // Cut
            {
                var menuItem = new ToolStripMenuItem("Cu&t");
                menuItem.Enabled = this.HasBlock();
                menuItem.Click += (o1, e1) =>
                {
                    this.SoftlyCut();
                };
                contextMenu.Items.Add(menuItem);
            }

            // Copy
            {
                var menuItem = new ToolStripMenuItem("&Copy");
                menuItem.Enabled = this.HasBlock();
                menuItem.Click += (o1, e1) =>
                {
                    this.Copy();
                };
                contextMenu.Items.Add(menuItem);
            }

            // Paste
            {
                var menuItem = new ToolStripMenuItem("&Paste");
                menuItem.Enabled = this.CanPaste();
                menuItem.Click += (o1, e1) =>
                {
                    this.SoftlyPaste();
                };
                contextMenu.Items.Add(menuItem);
            }

            // ---
            {
                var sep = new ToolStripSeparator();
                contextMenu.Items.Add(sep);
            }

            // Cut
            {
                var menuItem = new ToolStripMenuItem("RawCu&t");
                menuItem.Enabled = this.HasBlock();
                menuItem.Click += (o1, e1) =>
                {
                    this.RawCut();
                };
                contextMenu.Items.Add(menuItem);
            }

            // RawPaste
            {
                var menuItem = new ToolStripMenuItem("&RawPaste");
                menuItem.Enabled = this.CanPaste();
                menuItem.Click += (o1, e1) =>
                {
                    this.RawPaste();
                };
                contextMenu.Items.Add(menuItem);
            }

            // ---
            {
                var sep = new ToolStripSeparator();
                contextMenu.Items.Add(sep);
            }

            // Select All
            {
                var menuItem = new ToolStripMenuItem("Select &All");
                menuItem.Click += (o1, e1) =>
                {
                    this.SelectAll();
                };
                contextMenu.Items.Add(menuItem);
            }

            this.Update();
            contextMenu.Show(this, point);
        }

#endif
        #endregion

        // 检查每个字段的字符数是否足够
        // parameters:
        //      auto_fix    是否同时自动修复问题。
        public IEnumerable<string> Verify(bool auto_fix)
        {
            delegate_fix func = (field, start, length) =>
            {
                this._record.GetFieldOffsRange(field,
                    out int offs,
                    out _);
                if (length > 0)
                {
                    ReplaceText(offs + start,
                        offs + start,
                        new string(PaddingChar, length),
                        delay_update: true);
                }
                else
                {
                    ReplaceText(offs + start + length,
                        offs + start,
                        "",
                        delay_update: true);
                }
            };
            var errors = new List<string>();
            for (int i = 0; i < this._record.FieldCount; i++)
            {
                var field = this._record.GetField(i);
                if (auto_fix)
                {
                    errors.AddRange(field.Verify(func));
                }
                else
                {
                    errors.AddRange(field.Verify(null));
                }
            }
            return errors;
        }

        // 获得 MARC content。
        // 注意得到修正后的 content，过程并未改变编辑器中的原有内容，而是只把返回的内容修正了。函数执行后编辑器中的原有内容不变(并没有添加 PaddingChar)。
        public string GetContent(bool return_padded_content)
        {
            if (return_padded_content == false)
                return this._record.MergeText();

            StringBuilder result = new StringBuilder();
            delegate_fix func = (field, start, length) =>
            {
                var value = field.MergeText();

                this._record.GetFieldOffsRange(field,
                    out int offs,
                    out _);
                if (length > 0)
                {
                    value = value.Substring(0, offs + start)
                    + new string(PaddingChar, length)
                    + value.Substring(offs + start);
                }
                else
                {
                    value = value.Substring(0, offs + start)
                    + ""
                    + value.Substring(offs + start);
                }
                result.Append(value);
            };

            // errors 可用作调试观察
            var errors = new List<string>();
            for (int i = 0; i < this._record.FieldCount; i++)
            {
                var field = this._record.GetField(i);
                errors.AddRange(field.Verify(func));
            }
            return result.ToString();
        }

        public static string ConvertGbkBytesToNormalizedRussian(byte[] gbkBytes)
        {
            if (gbkBytes == null) throw new ArgumentNullException(nameof(gbkBytes));

            // 推荐使用 GB18030（兼容 GBK）；在 Windows 下也可使用 codepage 936 ("GBK")
            Encoding gbk;
            try
            {
                gbk = Encoding.GetEncoding("GB18030");
            }
            catch
            {
                // 兜底尝试 codepage 936
                gbk = Encoding.GetEncoding(936);
            }

            // 将 GBK 字节解码为 Unicode 字符串
            string decoded = gbk.GetString(gbkBytes);

            // 先做兼容性归一化（把很多“全角/兼容字符”映射为常规字符）
            string normalized = decoded.Normalize(NormalizationForm.FormKC);
            return normalized;
        }


        public virtual bool NormalizeSelectedText()
        {
            var text = GetSelectedContent();
            if (string.IsNullOrEmpty(text))
                return false;
            string normalized = text.Normalize(NormalizationForm.FormKC);
            if (normalized == text)
                return false;
            return this.SoftlyPaste(normalized);
        }

        public bool DisplayHex()
        {
            var text = GetSelectedContent();
            if (string.IsNullOrEmpty(text))
                return false;
            MessageBox.Show(this, ToHex(text, " "));
            return true;
        }

        public static string ToHex(string text, string sep = "")
        {
            var result = new StringBuilder();
            foreach (var ch in text)
            {
                if (text.Length > 0 && string.IsNullOrEmpty(sep) == false)
                    result.Append(sep);
                result.Append(Convert.ToString(ch, 16).PadLeft(4, '0'));
            }

            return result.ToString();
        }

        public static string ToDBC(string input)
        {
            char[] c = input.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                if (c[i] == 12288)
                {
                    c[i] = (char)32;
                    continue;
                }
                if (c[i] > 65280 && c[i] < 65375)
                {
                    c[i] = (char)(c[i] - 65248);
                }
            }
            return new string(c);
        }
    }

    public class CommandItem
    {
        public bool Checked { get; set; }

        public RefreshDelegate Refresh { get; set; } = null;

        // 命令名称。用在菜单中显示。& 表示快捷键。
        // 为 "-" 表示创建一个 Separator。
        // 为 “- Text” 表示创建一个 Label，Text 是标签内容。
        // public string Caption { get; set; }
        public GetCaptionDelegate GetCaption { get; set; } = null;

        public string Caption
        {
            set
            {
                GetCaption = () => value;
            }
        }

        // 关联的快捷键
        public Keys KeyData { get; set; } = Keys.None;

        // 第二个键
        public Keys KeyData2 { get; set; } = Keys.None;


        // 菜单项被点击时的处理函数
        public EventHandler Handler { get; set; } = null;

        // 菜单项是否可用(Enabled)的判断函数
        public CanExecuteDelegate CanExecute { get; set; } = null;

        public IEnumerable<CommandItem> SubCommands { get; set; } = null;

        public object Tag { get; set; } = null;
    }

    public delegate bool CanExecuteDelegate();

    public delegate string GetCaptionDelegate();

    public delegate CommandItem RefreshDelegate(CommandItem cmd);


    public static class CommandItemExtensions
    {
        // 泛型展平扩展：把每个元素和其子元素（递归）全部展平为一个序列
        public static IEnumerable<T> Flatten<T>(this IEnumerable<T> source, Func<T, IEnumerable<T>> children)
        {
            if (source == null)
            {
                yield break;
            }

            foreach (var item in source)
            {
                yield return item;
                var childs = children?.Invoke(item);
                if (childs != null)
                {
                    foreach (var sub in childs.Flatten(children))
                    {
                        yield return sub;
                    }
                }
            }
        }
    }

    // Delete 和 Backspace 键的功能风格
    [Flags]
    public enum DeleteKeyStyle
    {
        None = 0x00,
        // 在字段名和指示符区域 Delete 键被当作删除字段。
        // 如果为 false，表示效果为用填充字符替换插入符位置字符
        DeleteKeyAsDeleteField = 0x01,
        // 在字段内容最后一个字符以右删除，效果是否为删除字段结束符。删除后下一个字段的内容会接续上来
        // 如果为 false，表示效果为不允许删除字段结束符
        DeleteFieldTerminator = 0x02,
    }
}
