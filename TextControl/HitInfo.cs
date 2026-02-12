using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 点击信息
    /// </summary>
    public class HitInfo
    {
        public int X;   // 注意这是文档坐标
        public int Y;
        public int ChildIndex;  // 文档下级子对象的 index
        // public int RangeIndex;
        /*
        public Line Line;  // 点击的行
        public Range Range;    // 点击的 Range
        */
        public int TextIndex;    // 点击的文字 index。注意这是 ChildIndex 所指的子对象内部的文本 index，而不是指文档全局的
        public int Offs;    // 插入符所在的字符串线性位置

        int _lineHeight = 0;
        public int LineHeight   // 行高
        { 
            get
            {
                return _lineHeight;
            }
            set
            {
                // Debug.Assert(value == 42);
                _lineHeight = value;
            }
        }

        public Area Area;

        public object Box;    // 命中的 Box 对象

        public HitInfo InnerHitInfo;

        public int Direction;   // 前一次命中时采用过的 direction 参数。注意 HitInfo 这里返回的 Offs 值并没有和 Direction 对冲。

        public HitInfo Clone()
        {
            return new HitInfo
            {
                X = this.X,
                Y = this.Y,
                ChildIndex = this.ChildIndex,
                TextIndex = this.TextIndex,
                Offs = this.Offs,
                LineHeight = this.LineHeight,
                Area = this.Area,
                Box = this.Box,
                InnerHitInfo = this.InnerHitInfo?.Clone(),
                Direction = this.Direction,
            };
        }

        // direction <= 0 表示倾向于选择后方的位置
        public static HitInfo Select(List<HitInfo> infos, int direction)
        {
            if (direction <= 0)
            {
                return infos[infos.Count - 1];
            }
            return infos[0];
        }

        // 查找 HitInfo 链条中第一个匹配指定类型的 Box，并返回该类型实例（泛型版本）
        public static T HitInner<T>(HitInfo info, out HitInfo hit_info) where T : class
        {
            var current = info;
            while (current != null)
            {
                if (current.Box is T t)
                {
                    hit_info = current;
                    return t;
                }

                current = current.InnerHitInfo;
            }
            hit_info = null;
            return null;
        }

#if REF
        // 新增：按类型集合匹配，返回第一个命中的对象（object），并通过 out 返回对应的 HitInfo
        public static object HitInner(HitInfo info, IEnumerable<Type> types, out HitInfo hit_info)
        {
            if (info == null)
            {
                hit_info = null;
                return null;
            }
            if (types == null)
            {
                hit_info = null;
                return null;
            }

            // 为避免多次枚举，先 materialize
            var typeList = types as IList<Type> ?? types.ToList();
            if (typeList.Count == 0)
            {
                hit_info = null;
                return null;
            }

            var current = info;
            while (current != null)
            {
                var box = current.Box;
                if (box != null)
                {
                    foreach (var t in typeList)
                    {
                        if (t != null && t.IsInstanceOfType(box))
                        {
                            hit_info = current;
                            return box;
                        }
                    }
                }
                current = current.InnerHitInfo;
            }

            hit_info = null;
            return null;
        }

        // 便捷重载：使用 params 语法并带 out HitInfo
        public static object HitInner(HitInfo info, out HitInfo hit_info, params Type[] types)
        {
            return HitInner(info, (IEnumerable<Type>)types ?? Array.Empty<Type>(), out hit_info);
        }

#endif
    }


    // 点击的区域
    [Flags]
    public enum Area
    {
        None = 0x00,
        TopBlank = 0x01,
        BottomBlank = 0x02,
        LeftBlank = 0x04,
        RightBlank = 0x08,
        Text = 0x10,
        // Button = 0x20,  // 左侧按钮
    }

}
