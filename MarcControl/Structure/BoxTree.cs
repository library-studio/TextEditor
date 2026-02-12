using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace LibraryStudio.Forms
{
    public static class BoxTree
    {
        // 遍历祖先（从 parent 开始向上）
        public static IEnumerable<IBox> Ancestors(this IBox node)
        {
            if (node == null)
                yield break;
            var current = node.Parent;
            while (current != null)
            {
                yield return current;
                current = current.Parent;
            }
        }

        /*
迁移建议（小步快跑）
•	第一步（立即）：把上面三个文件加入代码库，改用 BoxTree.GetGlobalOffset() 在新代码中替代老的反射逻辑。
•	第二步（逐步）：为所有容器类实现 IContainer（最小改动，只是显式提供 Children）。示例：在 Collection<T> 添加 IContainer 实现（返回 Children.Cast<IBox>()）。
•	第三步（优化）：对性能敏感的容器实现 IIndexedContainer，复用现有内部高效方法（例如 Collection<T> 的 GetChildOffs(IViewBox)）。
•	第四步：逐步移除所有反射回退代码与 BoxExtensions 的反射实现（BoxExtensions 可暂留作兼容层，后期删除）。
        * */
        // 获得一个对象在整个结构树中的全局线性偏移
        // 计算全局线性偏移（不使用反射作为主路径）
        // 逻辑：对每一级 parent，优先使用 IIndexedContainer.GetChildOffs，
        // 否则使用 IContainer.Children 枚举累加到 child 之前的 TextLength，
        // 若都不可用则回退到 BoxExtensions.GetChildOffs（兼容旧逻辑）。
        public static int GetGlobalOffset(this IBox node)
        {
            if (node == null)
                return 0;

            int offs = 0;
            var child = node;
            while (child != null)
            {
                var parent = child.Parent;
                if (parent == null)
                    break;

                // 优先：父容器实现 IIndexedContainer（快速路径，无反射）
                if (parent is IIndexedContainer indexed)
                {
                    int v = indexed.GetChildOffs(child);
                    if (v >= 0)
                        offs += v;
                    else
                        offs += 0;
                }
                else if (parent is IContainer container)
                {
                    // 普通容器：遍历子项累加到 child 之前
                    int local = 0;
                    foreach (var c in container.Children)
                    {
                        if (ReferenceEquals(c, child))
                            break;
                        local += c?.TextLength ?? 0;
                    }
                    offs += local;
                }
                else
                {
                    throw new ArgumentException("遇到尚未实现 IContainer 接口的类");
                    // 回退：如果项目中已有 BoxExtensions（或其它兼容实现），使用它作为最后手段
                    // 这样既能避免在主路径使用反射，也能兼容现有代码（逐步迁移）

                    // offs += BoxExtensions.GetChildOffs(parent, child);
                }

                // 向上继续
                child = parent as IBox;
            }

            return offs;
        }

        // 获得一个对象的后继兄弟，如果没有后继兄弟了则获得父亲(或祖先)的后一个兄弟
        public static IBox GetNextSibling(this IBox node)
        {
            if (node == null)
                return null;
            var child = node;
            while (child != null)
            {
                var parent = child.Parent;
                if (parent == null)
                    break;

                if (parent is IContainer container)
                {
                    bool found = false;
                    foreach (var c in container.Children)
                    {
                        if (found)
                            return c;
                        if (ReferenceEquals(c, child))
                            found = true;
                    }
                }
 

                // 向上继续
                child = parent as IBox;
            }
            return null;
        }
    }

#if REMOVED
    public static class BoxExtensions
    {
        /// <summary>
        /// 尝试以兼容方式计算 child 在 parent 中的起始 offs。
        /// 优先反射调用 parent 上的 GetChildOffs(...)，否则回退到遍历 parent.Children 累加 TextLength。
        /// 如果无法确定则返回 0。
        /// </summary>
        public static int GetChildOffs(this IBox parent, IBox child)
        {
            if (parent == null || child == null)
                return 0;

            // 快速判断：相等情况下偏移为0
            if (ReferenceEquals(parent, child))
                return 0;

            var ptype = parent.GetType();

            // 1) 反射寻找 GetChildOffs 方法（尝试几种参数签名）
            try
            {
                MethodInfo method = null;
                // 首选签名：IViewBox
                method = ptype.GetMethod("GetChildOffs", new Type[] { typeof(IViewBox) })
                         // 其次：IBox
                         ?? ptype.GetMethod("GetChildOffs", new Type[] { typeof(IBox) })
                         // 最后：object
                         ?? ptype.GetMethod("GetChildOffs", new Type[] { typeof(object) });

                if (method != null)
                {
                    object arg = child;
                    var p = method.GetParameters().FirstOrDefault();
                    if (p != null)
                    {
                        var paramType = p.ParameterType;
                        // 如果方法期望 IViewBox，但 child 不是 IViewBox，则尝试用 child 作为 object
                        if (paramType == typeof(IViewBox) && !(child is IViewBox))
                        {
                            // pass as object (method may still accept it)
                            arg = (object)child;
                        }
                        else if (!paramType.IsInstanceOfType(child))
                        {
                            // 尝试转换（例如 child 是具体类型），否则仍以 object 传入
                            // leave arg as object
                            arg = (object)child;
                        }
                    }

                    var ret = method.Invoke(parent, new object[] { arg });
                    if (ret is int vi)
                        return vi;
                    if (ret is short vs)
                        return vs;
                    if (ret is long vl)
                        return (int)vl;
                }
            }
            catch
            {
                // 忽略反射异常，走回退策略
            }

            // 2) 回退：查找可枚举的 Children 属性并累加到 child 之前的 TextLength
            try
            {
                var prop = ptype.GetProperty("Children");
                if (prop != null)
                {
                    var childrenObj = prop.GetValue(parent) as IEnumerable;
                    if (childrenObj != null)
                    {
                        int offs = 0;
                        foreach (var c in childrenObj)
                        {
                            if (ReferenceEquals(c, child))
                                return offs;

                            if (c is IBox childBox)
                            {
                                offs += childBox.TextLength;
                            }
                            else
                            {
                                // 尝试通过反射读 TextLength 属性
                                var txtProp = c?.GetType().GetProperty("TextLength", BindingFlags.Public | BindingFlags.Instance);
                                if (txtProp != null && txtProp.PropertyType == typeof(int))
                                {
                                    var v = txtProp.GetValue(c);
                                    if (v is int vi)
                                        offs += vi;
                                }
                            }
                        }

                        // 没找到 child，则返回累计值（或 0）
                        return 0;
                    }
                }
            }
            catch
            {
                // 忽略遍历时异常
            }

            // 3) 无法计算，返回 0 作为安全默认
            return 0;
        }
    }

#endif
}
