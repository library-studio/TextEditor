using System.Collections.Generic;

namespace LibraryStudio.Forms
{
    // 只描述“有子节点”的能力，尽量保持精简
    public interface IContainer
    {
        // 返回该容器的子节点序列（逻辑上的直接子节点）
        IEnumerable<IBox> Children { get; }
    }
}
