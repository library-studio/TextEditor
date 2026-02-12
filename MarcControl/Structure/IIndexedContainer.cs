using System.Collections.Generic;

namespace LibraryStudio.Forms
{
    // 可选接口：为容器提供索引化访问/快速偏移计算，供性能关键路径实现
    public interface IIndexedContainer : IContainer
    {
        // 返回 child 在当前容器内从头开始的偏移（若找不到可返回 -1 或抛异常，按实现决定）
        int GetChildOffs(IBox child);

        // 可选：返回 child 的 index，找不到返回 -1
        int IndexOf(IBox child);
    }
}
