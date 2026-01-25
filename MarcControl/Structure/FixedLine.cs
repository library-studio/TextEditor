// ""

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 具有固定内容长度的 Line
    /// </summary>
    public class FixedLine : Line
    {
        public int FixedLength { get; set; }

        public FixedLine(IBox parent) : base(parent)
        {

        }

        public FixedLine(IBox parent, int fixed_length) : base(parent)
        {
            FixedLength = fixed_length;
        }
    }
}
