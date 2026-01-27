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
            var container_info = this.GetStructureInfoByBox(this.Parent, 2);
            if (container_info == null)
            {
                return base.SplitChildren(text);
            }

            return SplitChars(text, container_info);
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

        public override TemplateItem CreateChild(IContext context, int index, string text)
        {
            var result = base.CreateChild(context, index, text);
            var container_info = this.GetStructureInfoByBox(this.Parent, 2);
            int count = container_info?.SubUnits?.Count ?? -1;
            if (count > 0 && index > count - 1)
            {
                // result._initialCaptionText = "(溢出)";
                result.Overflow = true;
            }
            else
            {
                var info = container_info?.SubUnits?.ElementAtOrDefault(index);
                result.ItemName = info?.Name;
                result.SetStructureInfo(info, 1);
                // result._initialCaptionText = this.StructureInfo?.SubUnits?.ElementAtOrDefault(index)?.Caption;
                result.Overflow = false;
            }
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
