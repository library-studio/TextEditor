using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static LibraryStudio.Forms.MarcField;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 编辑命令
    /// </summary>
    public partial class MarcControl
    {
        #region Edit Commands

        public bool RawCut()
        {
            if (this._readonly)
                return false;

            if (HasBlock() == false)
                return false;
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
            var text = this.Content.Substring(start, length);
            Clipboard.SetText(text);
            RawRemoveBolckText();
            return true;
        }

        public bool SoftlyCut()
        {
            if (this._readonly)
                return false;

            if (HasBlock() == false)
                return false;
            /*
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
            var text = _record.MergeText(start, start + length);
            */

            Clipboard.SetText(GetSelectedContent());
            SoftlyRemoveBolckText();
            return true;
        }

        public bool Copy()
        {
            if (HasBlock() == false)
                return false;
            /*
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
            var text = this.Content.Substring(start, length);
            Clipboard.SetText(text);
            // RawRemoveBolckText();
            return true;
            */
            Clipboard.SetText(GetSelectedContent());
            return true;
        }

        public string GetSelectedContent()
        {
            if (HasBlock() == false)
                return "";
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
            return this._record.MergeText(start, start + length);
        }

        public bool CanCut()
        {
            return this.HasBlock() && this.ReadOnly == false;
        }

        public bool CanPaste()
        {
            if (this._readonly)
                return false;

            // 检查剪贴板中是否有文本
            return Clipboard.ContainsText();
        }

        // 硬粘贴
        public bool RawPaste()
        {
            if (this._readonly)
                return false;

            var text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
                return false;
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
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
        public bool SoftlyPaste(string text = null)
        {
            if (this._readonly)
                return false;

            if (text == null)
                text = Clipboard.GetText();
            if (string.IsNullOrEmpty(text))
                return false;
            var start = Math.Min(this.BlockStartOffset, this.BlockEndOffset);
            var length = Math.Abs(this.BlockEndOffset - this.BlockStartOffset);
            //var old_text = _record.MergeText(start, start + length);

            // 获得即将被替换部分内容的 mask 形态
            var old_mask_text = _record.MergeTextMask(start, start + length);
            //Debug.Assert(old_text.Length == old_mask_text.Length);

            // 压缩。相当于先删除一次
            var compressed = MarcRecord.CompressMaskText(old_mask_text);

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
                        break;
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
                        result.Append(GetChar());
                    else
                        break;
                }
            }
            result.Append(GetRest());
            return result.ToString();

            string GetRest()
            {
                // 返回 new_text 中余下的部分
                if (j < new_text.Length)
                    return new_text.Substring(j);
                return "";
            }

            char GetChar()
            {
                // 返回 new_text 中的一个字符
                if (j < new_text.Length)
                    return new_text[j++];
                return ' '; // 如果没有字符了，则返回空格
            }
        }


        public void SelectAll()
        {
            _blockOffs1 = 0;
            _blockOffs2 = this._record.TextLength;
            this.Invalidate();
        }

        // 选择一段文字
        public void Select(int start,
            int end,
            int caret_offs,
            int caret_delta = 0)
        {
            DetectBlockChange1(_blockOffs1, _blockOffs2);

            if (start >= 0)
                this._blockOffs1 = start;
            if (end >= 0)
                this._blockOffs2 = end;

            InvalidateBlockRegion();
            if (caret_offs + caret_delta >= 0)
            {
                if (caret_offs + caret_delta != _global_offs)
                {
                    // _global_offs = caret_offs + caret_delta;
                    SetGlobalOffs(caret_offs + caret_delta);
                    MoveCaret(HitByGlobalOffs(caret_offs, caret_delta), false);
                }
            }
        }

        public void SetLastX()
        {
            _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标
        }

        public bool CanUndo()
        {
            if (this._readonly)
                return false;

            return _history.CanUndo();
        }

        public bool CanRedo()
        {
            if (this._readonly)
                return false;

            return _history.CanRedo();
        }

        public bool Undo()
        {
            if (this._readonly)
                return false;

            var action = _history.Back();
            if (action == null)
                return false;
            var start = Math.Min(action.Start, action.End);
            // var end = Math.Max(action.End, action.Start);
            var end = start + action.NewText.Length;
            ReplaceText(start,
                end,
                action.OldText,
                delay_update: false,
                false,
                false);
            Select(start, start + action.OldText.Length, start);
            return true;
        }

        public bool Redo()
        {
            if (this._readonly)
                return false;

            var action = _history.Forward();
            if (action == null)
                return false;
            var start = Math.Min(action.Start, action.End);
            var end = start + action.OldText.Length;
            ReplaceText(start,
                end,
                action.NewText,
                delay_update: false,
                false,
                false);
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
            var offs = _global_offs;

            // 根据全局偏移找到字段
            var ret = this._record.LocateFields(offs, offs);
            if (ret.Length == 0)
                return null;
            // 字段下标
            var index = ret[0].Index;
            var field_offs = offs - ret[0].StartLength;
            var offs_in_field = ret[0].StartLength;

            var field = this._record.GetField(index);
            var info = field.GetSubfieldBounds(
                offs_in_field);
            if (info.Found == false)
                return null;
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
                    CanExecute=()=> this.HasBlock(),
                },
                new CommandItem()
                {
                    Caption="复制(&C)",
                    KeyData=Keys.Control | Keys.C,
                    Handler=(s,e) => this.Copy(),
                    CanExecute=()=> this.HasBlock(),
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
                    Caption="全选(&A)",
                    KeyData=Keys.Control | Keys.A,
                    Handler=(s,e) => this.SelectAll(),
                    CanExecute=()=> true,
                },

                new CommandItem() { Caption="-" },

                new CommandItem()
                {
                    Caption="属性(&P)",
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


        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Debug.WriteLine($"keyData={keyData.ToString()}");

            var ret = KeyTriggerCommand(keyData);
            if (ret == true)
                return ret;
            // 返回结果:
            //     true if the character was processed by the control; otherwise, false.
            return base.ProcessCmdKey(ref msg, keyData);
        }

        // 前一次命中(双键) CommandItem 的击键
        // 如果为 None，表示没有处于等待第二个键的状态
        Keys _firstHitKey = Keys.None;

        // 键盘触发命令
        public virtual bool KeyTriggerCommand(Keys keyData)
        {
            var commands = this.Commands;
            if (commands == null)
                return false;

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
                    Trigger(command);
                    return true;
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
                        command = item;
                    else
                        command = ret;
                }
                else
                {
                    if (item.GetCaption == null)
                        continue;
                    command = item;
                }

                Debug.Assert(command != null);

                var caption = command.GetCaption?.Invoke();
                if (caption == null)
                    continue;
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
                var menuItem = new ToolStripMenuItem(caption);
                if (caption.Contains("\t"))
                {
                    var parts = caption.Split(new char[] { '\t' }, 2);
                    menuItem.Text = parts[0];
                    menuItem.ShortcutKeyDisplayString = parts[1];
                }
                else
                {
                    try
                    {
                        menuItem.ShortcutKeys = command.KeyData;
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
                throw new ArgumentException($"无法识别的 sender 类型 '{sender.GetType().ToString()}'");
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
                        true);
                }
                else
                {
                    ReplaceText(offs + start + length,
                        offs + start,
                        "",
                        true);
                }
            };
            var errors = new List<string>();
            for (int i = 0; i < this._record.FieldCount; i++)
            {
                var field = this._record.GetField(i);
                if (auto_fix)
                    errors.AddRange(field.Verify(func));
                else
                    errors.AddRange(field.Verify(null));
            }
            return errors;
        }
    }

    public class CommandItem
    {
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
            if (source == null) yield break;
            foreach (var item in source)
            {
                yield return item;
                var childs = children?.Invoke(item);
                if (childs != null)
                {
                    foreach (var sub in childs.Flatten(children))
                        yield return sub;
                }
            }
        }
    }
}
