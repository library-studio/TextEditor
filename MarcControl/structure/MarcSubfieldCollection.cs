using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LibraryStudio.Forms;
using Vanara.PInvoke;

namespace MarcControl.structure
{
    /// <summary>
    /// MARC 子字段的集合容器
    /// </summary>
    public class MarcSubfieldCollection : Collection<MarcSubfield>
    {
        // TODO: 在 ReplaceText() 重载函数中为每个 Child 设置 Parent 和 Metrics
        public Metrics Metrics { get; set; }

        public MarcSubfieldCollection()
        {

        }

        public MarcSubfieldCollection(IBox parent, Metrics metrics)
        {
            this.Parent = parent;
            this.Metrics = metrics;
        }

        public override ReplaceTextResult ReplaceText(IContext context, Gdi32.SafeHDC dc, int start, int end, string text, int pixel_width)
        {
            var ret = base.ReplaceText(context, dc, start, end, text, pixel_width);
            foreach(MarcSubfield child in Children)
            {
                child.Parent = this;
                child.Metrics = this.Metrics;
            }
            return ret;
        }

        // 把文字内容按需切割为子结构所需的部分
        public override IEnumerable<string> SplitChildren(string text)
        {
            return SplitSubfields(text, Metrics.SubfieldCharDefault);
        }

        // 切割为子字段。\ 符号和以后的属于一个完整子字段。切割在 \ 符号之前发生
        // 子字段符号独立切割出来
        public static List<string> SplitSubfields(string text,
    char delimeter = '\\')
        {
            if (text == null)
            {
                return new List<string>();
            }

            List<string> lines = new List<string>();
            StringBuilder line = null;  // new StringBuilder();
            foreach (var ch in text)
            {
                if (ch == delimeter)
                {
                    if (line != null && line.Length > 0)
                    {
                        lines.Add(line.ToString());
                    }

                    line = new StringBuilder();
                    line.Append(ch);
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
