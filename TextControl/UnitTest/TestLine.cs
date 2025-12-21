using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Xunit;
using static Vanara.PInvoke.Gdi32;

namespace LibraryStudio.Forms
{
    public class TestLine
    {
        [Theory]
        [InlineData("1234", -2, 0, -1, 0)]
        [InlineData("1234", -1, 0, -1, 0)]
        [InlineData("1234", 0, 0, 0, 0)]
        [InlineData("1234", 1, 0, 0, 1)]
        [InlineData("1234", 2, 0, 0, 2)]
        [InlineData("1234", 3, 0, 0, 3)]
        [InlineData("1234", 4, 0, 0, 4)]
        [InlineData("1234", 5,0, 1, 0)]

        [InlineData("ฟิแก", -2, 0, -1, 0)]
        [InlineData("ฟิแก", -1, 0, -1, 0)]
        [InlineData("ฟิแก", 0, 0, 0, 0)]
        [InlineData("ฟิแก", 1, 0, 0, 0)]    // offs 1 偏向左侧
        [InlineData("ฟิแก", 0, 1, 0, 2)]    // offs 1 偏向右侧

        [InlineData("ฟิแก", 2, 0, 0, 2)]
        [InlineData("ฟิแก", 3, 0, 0, 3)]
        [InlineData("ฟิแก", 4, 0, 0, 4)]
        [InlineData("ฟิแก", 5, 0, 1, 0)]

        public void line_moveByOffs_01(string text, 
            int offs, 
            int direction,
            int correct_ret,
            int correct_offs)
        {
            var line = BuildLine(text);
            var ret = line.MoveByOffs(offs, direction, out HitInfo info);
            Assert.Equal(correct_ret, ret);
            Assert.Equal(correct_offs, info.Offs);
        }

        static Line BuildLine(string text)
        {

            using (var font = new Font("宋体", 12))
            using (var fonts = new FontContext(font))
            using (var bitmap = new Bitmap(1, 1))
            using (Graphics g = Graphics.FromImage(bitmap))
            {
                IContext context = new Context() {
                    GetFont = (p, o) => {
                        return fonts.Fonts;
                    }
                };

                var handle = g.GetHdc();
                var dc = new SafeHDC(handle);

                var line = new Line(null);
                var ret = line.ReplaceText(
                    context,
                    dc,
                    0,
                    -1,
                    text,
                    1000/*,
                    out string replaced,
                    out Rectangle update_rect,
                    out Rectangle scroll_rect,
                    out int scroll_distance*/);
                return line;
            }
        }

    }
}
