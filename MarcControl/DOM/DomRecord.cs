using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 用于外部操作 MarcRecord 的接口类。只提供修改 MARC 记录数据的必要接口
    /// </summary>
    public class DomRecord : IEnumerable<DomField>
    {
        MarcRecord _marcRecord = null;

        // 用于追踪所有由此 DomRecord 创建的 DomField，便于在字段插入/删除时通知它们更新索引或标记已删除
        private readonly List<WeakReference<DomField>> _domFieldListeners = new List<WeakReference<DomField>>();

        public DomRecord(MarcRecord record)
        {
            _marcRecord = record;
        }

        public IEnumerator<DomField> GetEnumerator()
        {
            if (_marcRecord == null)
                yield break;
            var count = _marcRecord.FieldCount;
            for (int i = 0; i < count; i++)
            {
                var f = new DomField(this._marcRecord, i);
                RegisterDomField(f);
                yield return f;
            }
        }


        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator<DomField>)this.GetEnumerator();
        }

        // 注册/反注册 DomField 实例
        internal void RegisterDomField(DomField field)
        {
            lock (_domFieldListeners)
            {
                // 设置 DomField 知道它所属的 DomRecord（可选）
                try { field.SetOwner(this); } catch { }
                _domFieldListeners.Add(new WeakReference<DomField>(field));

                // 定期清理已回收的 WeakReference（阈值可根据实际情况调整）
                const int cleanupThreshold = 512;
                if (_domFieldListeners.Count > cleanupThreshold)
                {
                    _domFieldListeners.RemoveAll(wr => !wr.TryGetTarget(out var _));
                }
            }
        }

        /*
这个 UnregisterDomField() 函数需要在什么时候调用呢？如果非常多次创建 DomField 对象但从不 unregister，会不会造成内存泄露? 

关于防止 reference 出现内存泄露的要点说明（简短回答）：
什么时候调用 UnregisterDomField：
•	在 DomField 生命周期结束时立即调用（例如在 DomField.Dispose() 中）。也可在 DomField 被显式从 UI/缓存中移除时调用。
•	DomRecord 自身被销毁/重建时也应清理其监听器列表。
是否会导致内存泄露：
•	直接的强引用泄露不会发生，因为你在 DomRecord 中只保存了 WeakReference<DomField>，这不会阻止 DomField 被 GC 回收。
•	但如果频繁创建而从不调用 Unregister 且很少触发任何通知清理，_domFieldListeners 列表会累积大量失效的 WeakReference 对象（虽然每个对象很小），可能导致列表内存和遍历成本增长，表现为“泄露感”。因此需要主动清理或在 DomField Dispose 时取消注册。
推荐做法（总结）：
1.	让 DomField 实现 IDisposable，在 Dispose 中调用 _owner?.UnregisterDomField(this) 并断开 _owner 引用。调用方负责在不再使用 DomField 时 Dispose()（例如 UI 控件销毁、缓存淘汰处）。
2.	在 DomRecord.RegisterDomField 中定期清理已失效的 WeakReference（示例中用阈值触发）。
3.	如有长期持有 DomField 的场景（缓存、列表等），务必在移除时调用 Dispose/Unregister，避免列表膨胀。
4.	不建议依赖 finalizer 作为主要清理手段（性能与不可预测性问题）；只有在确有必要才实现 finalizer 作为兜底。
        * 
         * */
        internal void UnregisterDomField(DomField field)
        {
            lock (_domFieldListeners)
            {
                _domFieldListeners.RemoveAll(wr =>
                {
                    if (wr.TryGetTarget(out var t))
                        return ReferenceEquals(t, field);
                    return true; // 清理已失效的引用
                });
            }
        }

        // 通知监听者：插入字段（index 及 count）
        private void NotifyFieldsInserted(int index, int count)
        {
            lock (_domFieldListeners)
            {
                var remove = new List<WeakReference<DomField>>();
                foreach (var wr in _domFieldListeners)
                {
                    if (wr.TryGetTarget(out var f))
                    {
                        try { f.InternalOnFieldsInserted(index, count); } catch { }
                    }
                    else
                        remove.Add(wr);
                }
                foreach (var r in remove) _domFieldListeners.Remove(r);
            }
        }

        // 通知监听者：删除字段（index 及 count）
        private void NotifyFieldsDeleted(int index, int count)
        {
            lock (_domFieldListeners)
            {
                var remove = new List<WeakReference<DomField>>();
                foreach (var wr in _domFieldListeners)
                {
                    if (wr.TryGetTarget(out var f))
                    {
                        try { f.InternalOnFieldsDeleted(index, count); } catch { }
                    }
                    else
                        remove.Add(wr);
                }
                foreach (var r in remove) _domFieldListeners.Remove(r);
            }
        }

        public DomField InsertField(
    int insert_index,
    string text)
        {
            if (insert_index == 0 && _marcRecord.FieldCount > 0)
            {
                throw new ArgumentException("非空记录不允许在头标区之前插入新字段");
            }
            // 得到插入点的 offs
            _marcRecord.GetFieldOffsRange(insert_index,
    out int offs,
    out _);
            if (text.Length > 0
                && text.Last() != Metrics.FieldEndCharDefault)
                text += Metrics.FieldEndCharDefault;
            _marcRecord._marcControl?.ReplaceText(offs,
    offs,
    text,
    false);

            // 在底层记录已经修改之后，通知已有的 DomField 调整索引
            NotifyFieldsInserted(insert_index, 1);

            var newField = new DomField(_marcRecord, insert_index);
            RegisterDomField(newField);
            return newField;

            // TODO: 插入后要让 Enumberator 失效
        }


        // 在给定的下标之前插入一个新的字段
        public DomField InsertField(
            int insert_index,
            string name,
            string indicator,
            string content)
        {
            return InsertField(
    insert_index,
    name + indicator + content);
        }

        // 修改字段的 Text。Text 是指字段全部内容(TODO: 包括字段结束符?)
        public void ChangeFieldText(int index, string text)
        {
            _marcRecord.GetFieldOffsRange(index,
                out int start,
                out int end);
            _marcRecord._marcControl?.ReplaceText(start,
    end,
    text,
    false);
        }

        // 根据给定的下标(范围)删除字段
        public void DeleteField(int index, int count = 1)
        {
            VerifyIndexLength(index, count);

            if (index == 0)
            {
                if (_marcRecord.FieldCount != 1
                    && count < _marcRecord.FieldCount)
                    throw new ArgumentException($"不允许删除头标区，除非头标区已经是最后一个字段");
            }
            // 先获得这些字段的 offs 范围。然后一次性删除
            _marcRecord.GetContiguousFieldOffsRange(index,
                count,
                out int start,
                out int end);
            _marcRecord._marcControl?.ReplaceText(start,
                end,
                null,   // 彻底删除
                false);

            // 通知已有 DomField 调整索引或标记为已删除
            NotifyFieldsDeleted(index, count);
        }

        // 校验即将删除的下标范围是否越界
        void VerifyIndexLength(int index, int length)
        {
            if (index < 0)
                throw new ArgumentException($"index ({index}) 不应小于 0");
            if (index + length > _marcRecord.FieldCount)
                throw new ArgumentException($"index ({index}) length ({length}) 越过字段总数 ({_marcRecord.FieldCount})");
        }

        // 清除所有字段
        public void Clear()
        {
            var count = _marcRecord.FieldCount;
            _marcRecord._marcControl?.ReplaceText(0,
    -1,
    null,   // 彻底删除
    false);

            // 通知已有 DomField 调整索引或标记为已删除
            NotifyFieldsDeleted(0, count);
        }

        // 根据元素下标，选择一个范围的若干字段
        public void SelectField(int index, int count = 1)
        {
            VerifyIndexLength(index, count);

            // 先获得这些字段的 offs 范围
            _marcRecord.GetContiguousFieldOffsRange(index,
                count,
                out int start,
                out int end);
            // 选择
            Select(start, end, start);
        }

        // 选择任意 start~end 范围内的文本
        public void Select(int start,
            int end,
            int caret_offs,
            int caret_delta = 0)
        {
            GetControl()?.Select(start,
                end,
                caret_offs,
                caret_delta);
        }

        // 获得一个字段的 offs 范围
        public bool GetFieldOffsRange(int field_index,
    int count,
    out int start,
    out int end)
        {
            return _marcRecord.GetContiguousFieldOffsRange(field_index,
                count,
                out start,
                out end);
        }

        // 根据元素下标得到一个 DomField 对象
        public DomField GetField(int index)
        {
            if (index < 0 || index >= _marcRecord.FieldCount)
                throw new ArgumentException($"index 值 {index} 越界");

            var f = new DomField(_marcRecord, index);
            RegisterDomField(f);
            return f;
        }

        // 根据给定的 offs 定位到一个字段
        public DomField LocateField(int offs,
            int direction = 0)
        {
            // return:
            //      -1  越过左边
            //      0   成功
            //      1   越过右边
            var ret = _marcRecord.MoveByOffs(offs, direction, out HitInfo info);
            if (ret == 0)
            {
                if (info.ChildIndex >= _marcRecord.FieldCount)
                    return null;
                return GetField(info.ChildIndex);
            }
            return null;
        }

#if REMOVED
        // 根据给定的 offs 定位到一个字段
        // return:
        //      -1  越过左边
        //      0   成功
        //      1   越过右边
        public int LocateField(
            int offs,
            out int field_index,
            out int offs_in_field)
        {
            field_index = -1;
            offs_in_field = -1;
            // return:
            //      -1  越过左边
            //      0   成功
            //      1   越过右边
            var ret = _marcRecord.MoveByOffs(offs, 0, out HitInfo info);
            if (ret != 0)
                return ret;
            GetFieldOffsRange(field_index,
                1,
                out int start,
                out int end);
            offs_in_field = offs - start;
            return 0;
        }
#endif
        // 根据给定的 offs 定位到一个字段
        // return:
        public bool LocateField(
            int offs,
            out int field_index,
            out int offs_in_field)
        {
            var results = _marcRecord.LocateFields(offs, offs);
            if (results.Length == 0)
            {
                field_index = -1;
                offs_in_field = -1;
                return false;
            }

            field_index = results[0].Index;
            offs_in_field = results[0].StartLength;
            return true;
        }

        // 根据指定的 offs 范围，定位经过的字段
        public bool LocateFields(int start,
int end,
out int field_index,
out int count)
        {
            return _marcRecord.LocateFields(start,
                end,
                out field_index,
                out count);
        }

        // 探测 offs 落在了字段的哪个区域
        // TODO: 使用 enum
        // 0:提示区 1:字段名 2:指示符 3:内容
        public int DetectPart(int field_index,
            int offs_in_field)
        {
            var field = GetField(field_index);
            if (field.IsHeader)
                return 3;
            if (offs_in_field < 3)
                return 1;
            if (field.IsControlField)
                return 3;
            offs_in_field -= 3;
            if (offs_in_field < 2)
                return 2;
            return 3;
        }

        // 文字块开始偏移
        public int SelectionStart
        {
            get
            {
                return GetControl().BlockStartOffset;
            }
        }

        // 文字块结束偏移
        public int SelectionEnd
        {
            get
            {
                return GetControl().BlockEndOffset;
            }
        }

        // 插入符偏移
        public int CaretOffset
        {
            get
            {
                return GetControl().CaretOffset;
            }
        }


        public int FieldCount
        {
            get
            {
                return _marcRecord?.FieldCount ?? 0;
            }
        }

        public MarcControl GetControl()
        {
            return _marcRecord?.GetControl();
        }
    }
}
