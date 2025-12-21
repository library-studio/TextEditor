using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vanara.Extensions.Reflection;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 用于外部操作 MarcField 的接口类。只提供修改 MARC 记录数据的必要接口
    /// DomField 对象用完后及时 Dispose() 掉可以减轻 Register 体系的压力
    /// </summary>
    public class DomField : IDisposable
    {
        int _fieldIndex = -1;
        MarcRecord _record = null;

        public int Index
        {
            get
            {
                return _fieldIndex;
            }
        }

        #region 追踪变化

        /*
SampleChange() 函数中 field2 在 field1 被删除后，其 Content 不正确了，请问需要对 DomRecord 追踪 DomField 变化这一能力对代码进行什么改造？

你收到这些错误是因为 DomField 实例在底层记录结构变化（插入或删除其它字段）时没有被更新，它们通常缓存了字段的“索引”或位置信息而不知底层已变更。
发生这种情况的原因可能是 DomField 保留了初始索引（或偏移），但当 DomRecord 调用 ReplaceText 导致字段插入/删除后，已有 DomField 的索引不再正确，导致后续访问（如 field2.Content）返回错误或过期数据。
下面是我修复代码的说明
为 DomRecord 增加对已创建 DomField 的追踪（WeakReference 列表），在 InsertField/DeleteField 之后发送通知；为 DomField 添加内部处理接口（InternalOnFieldsInserted/InternalOnFieldsDeleted、SetOwner）以在通知时更新索引或标记已删除；同时让 DomField 的 Content 读取使用最新索引或返回空。当你把这些改动合入现有 DomField 的实际字段名/内容读取逻辑后，field2 在 field1 被删除后将能返回正确的 Content。
        * */

        private bool _isDeleted;
        private DomRecord _owner;
        private bool _disposed;

        // DomRecord 在注册时调用，建立反向关联（可选）
        internal void SetOwner(DomRecord owner)
        {
            _owner = owner;
        }

        // 当调用 Dispose 时，通知 DomRecord 取消注册，避免 _domFieldListeners 中的 WeakReference 长期堆积
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;

            var o = _owner;
            _owner = null;

            try
            {
                o?.UnregisterDomField(this);
            }
            catch
            {
                // 忽略取消注册时的异常
            }

            GC.SuppressFinalize(this);
        }

        // 若需要，可以定义 finalizer 作为兜底（通常不推荐，除非确实需要）
        //~DomField()
        //{
        //    Dispose();
        //}

        // DomRecord 在字段插入时调用：若本 field 在插入点之后需偏移
        internal void InternalOnFieldsInserted(int insertIndex, int count)
        {
            if (_isDeleted) return;
            if (_fieldIndex >= insertIndex)
                _fieldIndex += count;
        }

        // DomRecord 在字段删除时调用：若本 field 在删除范围内则标记为已删除；在删除范围之后的索引需左移
        internal void InternalOnFieldsDeleted(int deleteIndex, int count)
        {
            if (_isDeleted) return;
            if (_fieldIndex >= deleteIndex && _fieldIndex < deleteIndex + count)
            {
                // 本 field 被删除
                _isDeleted = true;
            }
            else if (_fieldIndex >= deleteIndex + count)
            {
                _fieldIndex -= count;
            }
        }

        void DenyModifyDeleted()
        {
            if (_isDeleted)
                throw new InvalidOperationException("当前 DomField 对象已经被删除，不允许进行修改操作");
        }

        void DenyUseDeleted()
        {
            if (_isDeleted)
                throw new InvalidOperationException("当前 DomField 对象已经被删除，不允许进行操作");
        }

        public bool IsDeleted
        {
            get
            {
                return _isDeleted;
            }
        }

        #endregion

        public DomField(
            MarcRecord record,
            int field_index)
        {
            _record = record;
            _fieldIndex = field_index;
        }

        public bool IsHeader
        {
            get
            {
                return GetMarcField()?.IsHeader ?? false;
            }
        }

        public bool IsControlField
        {
            get
            {
                return GetMarcField()?.IsControlField ?? false;
            }
        }

        public string Name
        {
            get
            {
                return GetMarcField()?.GetName();
            }
            set
            {
                DenyModifyDeleted();

                GetMarcField(true).ChangeName(value);
            }
        }

        public string Indicator
        {
            get
            {
                return GetMarcField()?.GetIndicator();
            }
            set
            {
                DenyModifyDeleted();

                GetMarcField(true).ChangeIndicator(value);
            }
        }

        public string Content
        {
            get
            {
                return GetMarcField()?.GetContent();
            }
            set
            {
                DenyModifyDeleted();

                GetMarcField(true).ChangeContent(value);
            }
        }

        public string Text
        {
            get
            {
                return GetMarcField()?.MergePureText();
            }
            set
            {
                DenyModifyDeleted();

                GetMarcField(true).ChangeText(value);
            }
        }

        // 删除自己
        public void Delete()
        {
            DenyModifyDeleted();

            _owner?.DeleteField(_fieldIndex);
        }

        public MarcField GetMarcField(bool throw_exception = false)
        {
            DenyUseDeleted();

            var result = _record?.GetField(_fieldIndex);
            if (result == null && throw_exception)
                throw new ArgumentException($"index 为 {_fieldIndex} 的 MarcField 对象没有找到");
            return result;
        }
    }
}
