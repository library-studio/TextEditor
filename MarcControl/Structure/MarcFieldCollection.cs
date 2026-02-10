using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using Vanara.PInvoke;
using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// MARC 字段的集合容器
    /// </summary>
    public class MarcFieldCollection : Collection<MarcInnerField>
    {
        public MarcFieldCollection() : base()
        {

        }

        public MarcFieldCollection(IBox parent, Metrics metrics) : base(parent, metrics)
        {
            this.Parent = parent;
            this.Metrics = metrics;
        }

        public override void Paint(IContext context, Gdi32.SafeHDC dc, int x, int y, Rectangle clipRect, int blockOffs1, int blockOffs2, int virtual_tail_length)
        {
            base.PaintBack(context,
                dc,
                x,
                y,
                clipRect);
            this.PaintSolidArea(dc,
    x,
    y,
    clipRect,
    -1/*caret_field_index*/);
            base.Paint(context,
                dc,
                x,
                y,
                clipRect,
                blockOffs1,
                blockOffs2,
                virtual_tail_length);
        }

        public void PaintSolidArea(SafeHDC hdc,
    int x,
    int y,
    Rectangle clipRect,
    int caret_field_index = -1)
        {
            int i = 0;
            foreach (var field in this.Children)
            {
                if (y >= clipRect.Bottom)
                    break;
                if (field.PlainText == false)
                {
                    field.PaintBackAndBorder(hdc,
    x,
    y,
    clipRect,
    i == caret_field_index);
                }
                y += field.GetPixelHeight();
                i++;
            }
        }


        public override ReplaceTextResult ReplaceText(IContext context, Gdi32.SafeHDC dc, int start, int end, string text, int pixel_width)
        {
            throw new NotImplementedException();
        }

        public override ReplaceTextResult ReplaceText(
            ViewModeTree view_mode_tree,
            IContext context, Gdi32.SafeHDC dc, int start, int end, string text, int pixel_width)
        {
            var ret = base.ReplaceText(
                view_mode_tree,
                context, dc, start, end, text, pixel_width);
            foreach (MarcInnerField child in Children)
            {
                child.Parent = this;
                child.Metrics = this.Metrics;
            }
            return ret;
        }

        /*
        public override MarcSubfield CreateChild(IContext context, int index)
        {
            var result = base.CreateChild(context, index);
            return result;
        }
        */

        public override MarcInnerField CreateChild(IContext context, int index, string text)
        {
            var result = base.CreateChild(context, index, text);
            var container_info = _struct;   // this.GetStructureInfoByBox(this.Parent, 2);
            // int count = container_info?.SubUnits?.Count ?? -1;
            var name = MarcSubfield.NormalizeName(text.Substring(0, Math.Min(2, text.Length)));
            var info = container_info?.SubUnits?.Where(o => o.Name == name).FirstOrDefault();
            result.SetStructureInfo(info, 1, null);
            return result;
        }

        UnitInfo _struct = null;

        // 把文字内容按需切割为子结构所需的部分
        public override IEnumerable<string> SplitChildren(string text)
        {
            _struct = this.GetStructureInfoByBox(this.Parent,
                2,
                (o) => text
                );

            return SplitFields(text);
        }

        // 切割为子字段。\ 符号和以后的属于一个完整子字段。切割在 \ 符号之前发生
        // 子字段符号独立切割出来
        public static List<string> SplitFields(string text)
        {
            if (text == null)
            {
                return new List<string>();
            }

            char delimeter = (char)0x01;

            List<string> lines = new List<string>();
            StringBuilder line = null;  // new StringBuilder();
            foreach (var ch in text.Replace("\u001f1", new string(delimeter, 1)))
            {
                if (ch == delimeter)
                {
                    if (line != null && line.Length > 0)
                    {
                        lines.Add(line.ToString());
                    }

                    line = new StringBuilder();
                    line.Append("\u001f1");
                }
                else
                {
                    if (line == null)
                    {
                        line = new StringBuilder();
                    }

                    line.Append(ch);
                }

            }
            if (line != null && line.Length > 0)
            {
                lines.Add(line.ToString());
            }

            if (lines.Count == 0)
            {
                lines.Add("");
            }

            return lines;
        }

    }
}
