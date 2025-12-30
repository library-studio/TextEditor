using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.Kernel32.DEBUG_EVENT;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 文字选择，文字块
    /// </summary>
    public partial class MarcControl
    {
        int _blockOffs1 = 0;    // -1;   // 选中范围开始的偏移量
        int _blockOffs2 = 0;    // -1;   // 选中范围的结束的偏移量

        // 文字块的起始偏移量
        public int BlockStartOffset
        {
            get { return _blockOffs1; }
            //get { return Math.Min(_blockOffs1, _blockOffs2); }
        }

        // 文字块的结束偏移量
        public int BlockEndOffset
        {
            get { return _blockOffs2; }
            //get { return Math.Max(_blockOffs1, _blockOffs2); }
        }

        // 选择一段文字
        public void Select(int start,
            int end,
            int caret_offs,
            int caret_delta = 0)
        {
            ChangeSelection(() => {
                if (start >= 0)
                    this._blockOffs1 = start;
                if (end >= 0)
                    this._blockOffs2 = end;
            });
            /*
            DetectBlockChange1(_blockOffs1, _blockOffs2);

            if (start >= 0)
                this._blockOffs1 = start;
            if (end >= 0)
                this._blockOffs2 = end;

            InvalidateBlockRegion();
            */
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

        public bool HasBlock()
        {
            return _blockOffs1 != _blockOffs2; // 选中范围不相等，表示有选中范围
        }

        // 柔和地删除块中文字。所谓柔和的意思是，保留固定长内容的字符数(只把这些字符替换为空格)
        public bool SoftlyRemoveBolckText()
        {
            if (_blockOffs1 == _blockOffs2)
                return false; // 不存在选中范围

            var start = Math.Min(_blockOffs1, _blockOffs2);
            var length = Math.Abs(_blockOffs1 - _blockOffs2);

            /*
            text = text.Remove(start, length);

            Relayout(false);
            */
            var mask_text = _record.MergeTextMask(start, start + length);

            ReplaceText(start,
                start + length,
                MarcRecord.CompressMaskText(mask_text, PaddingChar),
                delay_update: false,
                false);

            {
                ChangeSelection(start);
                /*
                DetectBlockChange1(_blockOffs1, _blockOffs2);

                _blockOffs1 = start;
                _blockOffs2 = start;

                // 块定义发生刷新才有必要更新变化的区域
                InvalidateBlockRegion();
                */
            }

            if (_global_offs > start)
            {
                // DeltaGlobalOffs(-length); // 调整 _global_offs
                AdjustGlobalOffs(-(length - 1));
                // 这里面已经有更新块显示的动作
                MoveGlobalOffsAndBlock(-1);
            }
            return true;


#if REMOVED
            string process_mask_text(string text)
            {
                // 将 mask_text 中 0x01 字符替换为空格，其余内容丢弃
                var result = new StringBuilder();
                foreach (var ch in text)
                {
                    if (ch == (char)0x01)
                        result.Append(' ');
                }
                return result.ToString();
            }
#endif
        }


        // 删除块中的文字。硬性删除的版本，可能会导致固定长内容的字符数变化
        public bool RawRemoveBolckText()
        {
            if (_blockOffs1 == _blockOffs2)
                return false; // 不存在选中范围

            var start = Math.Min(_blockOffs1, _blockOffs2);
            var length = Math.Abs(_blockOffs1 - _blockOffs2);

            /*
            text = text.Remove(start, length);

            Relayout(false);
            */
            ReplaceText(start,
                start + length,
                "",
                delay_update: false,
                false);

            {
                ChangeSelection(start);
                /*
                DetectBlockChange1(_blockOffs1, _blockOffs2);

                _blockOffs1 = start;
                _blockOffs2 = start;

                // 块定义发生刷新才有必要更新变化的区域
                InvalidateBlockRegion();
                */
            }

            if (_global_offs > start)
            {
                // DeltaGlobalOffs(-length); // 调整 _global_offs
                AdjustGlobalOffs(-(length - 1));
                MoveGlobalOffsAndBlock(-1);
            }
            return true;
        }


        int _oldOffs1 = 0;
        int _oldOffs2 = 0;

        // 修改 _blockOffs1, _blockOffs2，并刷新显示
        public void ChangeSelection(Action change_action)
        {
            DetectBlockChange1(_blockOffs1, _blockOffs2);

            change_action?.Invoke();

            InvalidateBlockRegion();
        }

        // 修改 _blockOffs1, _blockOffs2，并刷新显示
        public void ChangeSelection(int start, int end)
        {
            DetectBlockChange1(_blockOffs1, _blockOffs2);

            _blockOffs1 = start;
            _blockOffs2 = end;

            InvalidateBlockRegion();
        }

        // 修改 _blockOffs1, _blockOffs2，并刷新显示
        public void ChangeSelection(int value)
        {
            DetectBlockChange1(_blockOffs1, _blockOffs2);

            _blockOffs1 = value;
            _blockOffs2 = value;

            InvalidateBlockRegion();
        }

        // 为了比较块定义是否发生变化，从而决定是否刷新显示，第一步，记忆信息
        void DetectBlockChange1(int offs1, int offs2)
        {
            _oldOffs1 = offs1;
            _oldOffs2 = offs2;
        }

#if REMOVED
        // 为了比较块定义是否发生变化，从而决定是否刷新显示，第二步，进行比较
        bool DetectBlockChange2(int offs1, int offs2)
        {
            // 新旧两种，都是无块定义
            if (_oldOffs1 == _oldOffs2
                && offs1 == offs2)
                return false;
            // 接上，此时说明新旧对比，至少有一个有快定义。
            // 那么头尾有任何变化，都算作块定义发生了变化
            if (_oldOffs1 != offs1 || _oldOffs2 != offs2)
                return true;
            return false;
        }
#endif

#if OLD
        // 多语种时不完备，即将废弃
        void InvalidateBlock(bool trigger_event = true)
        {
            // 移动插入符的情况，不涉及到块定义和变化
            if (_oldOffs1 == _oldOffs2
                && _blockOffs1 == _blockOffs2)
                return;
            bool changed = false;
            if (InvalidateBlock(_oldOffs1, _blockOffs1))
                changed = true;
            if (InvalidateBlock(_oldOffs2, _blockOffs2))
                changed = true;

            if (trigger_event == true && changed)
            {
                this.BlockChanged?.Invoke(this, new EventArgs());
            }
        }
#endif

        void InvalidateBlockRegion(bool trigger_event = true)
        {
            // 移动插入符的情况，不涉及到块定义和变化
            if (_oldOffs1 == _oldOffs2
                && _blockOffs1 == _blockOffs2)
                return;
            bool changed = false;
            if (InvalidateBlockRegion(_oldOffs1, _blockOffs1))
                changed = true;
            if (InvalidateBlockRegion(_oldOffs2, _blockOffs2))
                changed = true;

            if (changed == true)
                DisposeBlockRegion();

            if (trigger_event == true && changed)
            {
                this.BlockChanged?.Invoke(this, new EventArgs());
            }
        }

#if OLD
        // 多语种时不完备，即将废弃
        bool InvalidateBlock(int offs1, int offs2)
        {
            if (offs1 == offs2)
                return false;
            var rect = GetBlockRectangle(offs1, offs2);
            if (rect.IsEmpty == false)
            {
                this.Invalidate(rect);
                return true;
            }

            return false;
        }
#endif

        bool InvalidateBlockRegion(int offs1, int offs2)
        {
            if (offs1 == offs2)
                return false;
            var region = GetBlockRegion(offs1, offs2);
            if (region != null)
            {
                this.Invalidate(region);
                region.Dispose();
                return true;
            }

            return false;
        }

        // 获得表示块精确边界范围的 Region 对象
        Region GetBlockRegion(
            int offs1,
            int offs2)
        {
            if (offs1 < 0 || offs2 < 0)
                return null;

            if (offs1 == offs2)
                return null;

            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;

            int start = Math.Min(offs1, offs2);
            int end = Math.Max(offs1, offs2);

            var region = _record.GetRegion(start, end, 1);
            if (region != null)
                region.Offset(x, y);
            return region;
        }


        // 获得包围块边界范围的 Rectangle。若需要精确边界请使用 GetBlockRegion()
        Rectangle GetBlockRectangle(int offs1, int offs2)
        {
            if (offs1 < 0 || offs2 < 0)
                return System.Drawing.Rectangle.Empty;

            if (offs1 == offs2)
                return System.Drawing.Rectangle.Empty;

            int x = -this.HorizontalScroll.Value;
            int y = -this.VerticalScroll.Value;

            int start = Math.Min(offs1, offs2);
            int end = Math.Max(offs1, offs2);
            _record.MoveByOffs(start, 0, out HitInfo info1);
            _record.MoveByOffs(end, 0, out HitInfo info2);

            // start 或者 end 越出当前合法范围，返回一个巨大的矩形。迫使窗口全部失效
            if (info1.Area != Area.Text
                || info2.Area != Area.Text)
                return new Rectangle(x + 0,
    y + 0,
    this.AutoScrollMinSize.Width,   // document width
    this.AutoScrollMinSize.Height);

            // return new Rectangle(0, 24, 5000, 24);
            if (info1.Y == info2.Y)
            {
                var left = Math.Min(info1.X, info2.X);
                var right = Math.Max(info1.X, info2.X);
                return new Rectangle(x + left,
                y + info1.Y,
                right - left,
                info2.Y + info2.LineHeight - info1.Y);
            }
            Debug.Assert(info1.Y <= info2.Y);
            return new Rectangle(x + 0,
                y + info1.Y,
                this.AutoScrollMinSize.Width,   // document width
                info2.Y + info2.LineHeight - info1.Y);
        }

    }
}
