using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

using Vanara.PInvoke;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// Chars 模板
    /// </summary>
    public class Template : Collection<TemplateItem>
    {

        public Template(IBox parent, Metrics metrics) : base(parent, metrics)
        {
        }

        public override IEnumerable<string> SplitChildren(string text)
        {
            if (this.StructureInfo == null)
            {
                return base.SplitChildren(text);
            }

            return SplitChars(text, this.StructureInfo);
        }

        static List<string> SplitChars(string text, UnitInfo info)
        {
            var results = new List<string>();
            int offs = 0;
            foreach (var unit in info.SubUnits)
            {
                string segment = text.Substring(offs, Math.Min(unit.Length, text.Length - offs));
                results.Add(segment);
                offs += segment.Length;
            }

            // 多余出来的内容
            if (offs < text.Length)
            {
                results.Add(text.Substring(offs));
            }

            if (results.Count == 0)
            {
                results.Add("");    // 至少要有一个元素
            }
            return results;
        }

        public override TemplateItem CreateChild(IContext context, int index)
        {
            var result = base.CreateChild(context, index);
            int count = this.StructureInfo?.SubUnits?.Count ?? -1;
            if (count > 0 && index > count - 1)
                result._initialCaptionText = "(溢出)";
            else
                result._initialCaptionText = this.StructureInfo?.SubUnits?.ElementAtOrDefault(index)?.Caption;
            return result;
        }


        public override void Paint(IContext context, Gdi32.SafeHDC dc, int x, int y, Rectangle clipRect, int blockOffs1, int blockOffs2, int virtual_tail_length)
        {
            base.PaintBack(context,
                dc,
                x,
                y,
                clipRect);
            base.Paint(context,
                dc,
                x,
                y,
                clipRect,
                blockOffs1,
                blockOffs2,
                virtual_tail_length);
        }
    }
}
