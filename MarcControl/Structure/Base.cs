using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 组件基础类。实现一些通用功能
    /// </summary>
    public class Base
    {
        public IBox Parent { get; set; }

        // 引用字段共同属性
        internal Metrics _metrics;
        public Metrics Metrics
        {
            get
            {
                return _metrics;
            }
            set
            {
                _metrics = value;
            }
        }

        UnitInfo _struct_info = null;   // 里面包含了 .Name
        int _struct_level = 0;
        // string _struct_content = null;

        public void ClearStructureInfo()
        {
            _struct_info = null;
            _struct_level = 0;
        }

        // 查询 this.Parent 对象的结构信息。如果 name 不为 null，则表示查询 name 名字的对象的结构信息(假定这个对象是 this.Parent 的下级对象)。
        // TODO: 可以考虑集中做一个缓存所有结构信息的 Hashtable 共享
        // 头标区的 name 在查询结构时要使用 "###"
        public UnitInfo GetStructureInfo(string name,
            UnitType type,
            int level,
            GetContentFunc func_getcontent)
        {
            var path = UnitNode.BuildPath(this.Parent, name, type, func_getcontent);

            if (_struct_info != null
                && _struct_info.Sensitive == false
                && _struct_info.Name == path.LastOrDefault()?.Name
                && _struct_level >= level
                /*&& _struct_content == content*/)
            {
                return _struct_info;
            }

            try
            {
                // 如果 name 为 null，表示这是头标区
                var struct_info = _metrics.GetStructure?.Invoke(path, level);
                _struct_info = struct_info;
                _struct_level = level;
                //_struct_content = content;
                return _struct_info;
            }
            catch (Exception ex)
            {
                // TODO: 飘出 tips 显示出错原因
                return null;
            }
        }

        // 查询 box 对象的结构信息
        public UnitInfo GetStructureInfoByBox(IBox box,
            int level,
            GetContentFunc func_getcontent)
        {
            // 这个函数不完美，无法在第二参数为 null 的情况下为最后一级路径设置 .Content
            var path = UnitNode.BuildPath(box,
                null,
                UnitType.Unknown,
                null);
            // 专门给路径最后一级设置 .Content
            var last = path.LastOrDefault();
            if (last != null)
            {
                last.GetContent = func_getcontent;
            }

            if (_struct_info != null
                && _struct_info.Sensitive == false
                && _struct_info.Name == path.LastOrDefault()?.Name
                && _struct_level >= level
                /*&& _struct_content == content*/)
            {
                return _struct_info;
            }

            try
            {
                // 如果 name 为 null，表示这是头标区
                var struct_info = _metrics.GetStructure?.Invoke(path, level);
                _struct_info = struct_info;
                _struct_level = level;
                //_struct_content = content;
                return _struct_info;
            }
            catch(Exception ex)
            {
                // TODO: 飘出 tips 显示出错原因
                return null;
            }
        }

        public void SetStructureInfo(UnitInfo struct_info,
            int level,
            string content)
        {
            _struct_info = struct_info;
            if (struct_info == null)
            {
                _struct_level = 0;
                //_struct_content = null;
            }
            else
            {
                _struct_level = level;
                //_struct_content = content;
            }
        }

        public string GetCaptionText(string name,
            UnitType type,
            GetContentFunc func_getcontent)
        {
            return GetStructureInfo(name, type, 1, func_getcontent)?.Caption ?? "";
        }

        public IEnumerable<ValueItem> GetValueList(IBox box)
        {
            try
            {
                return _metrics.GetValueList?.Invoke(UnitNode.BuildPath(box, null));
            }
            catch(Exception ex)
            {
                // TODO: 飘出 tips 显示出错原因
                return null;
            }
        }
    }
}
