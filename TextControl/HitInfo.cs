using System;
using System.Collections.Generic;
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
        public int LineHeight; // 行高
        public Area Area;

        public HitInfo Clone()
        {
            return new HitInfo
            {
                X = this.X,
                Y = this.Y,
                ChildIndex = this.ChildIndex,
                TextIndex = this.TextIndex,
                Area = this.Area
            };
        }
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
    }

}
