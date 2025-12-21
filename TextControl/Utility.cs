using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Vanara.PInvoke;

namespace LibraryStudio.Forms
{
    public static class Extensions
    {
        public static void Offset(this Region region, int x, int y)
        {
            region.Translate(x, y);
        }

        public static Rectangle Larger(this Rectangle rect)
        {
            /*
            rect.Width += 1;
            rect.Height += 1;
            */
            return rect;
        }
    }

    /// <summary>
    /// 实用类
    /// </summary>
    public class Utility
    {

        // 判断两个 offs 范围是否有交叉
        // 如果 start == end，表示空范围。只用 start 探测一次，不用 end-1 进行探测
        public static bool Cross(int start1, int end1,
            int start2, int end2)
        {
            if (start1 > end1) 
                throw new ArgumentException("start1 必须 <= end1");
            if (start2 > end2)
                throw new ArgumentException("start2 必须 <= end2");

            if (InRange(start1, start2, end2))
                return true;
            if (end1 > start1
                && InRange(end1 - 1, start2, end2))
                return true;

            if (InRange(start2, start1, end1))
                return true;
            if (end2 > start2
                && InRange(end2 - 1, start1, end1))
                return true;
            return false;
        }

        public static bool InRange(int offs,
            int start,
            int end)
        {
            if (offs >= start && offs < end)
                return true;
            return false;
        }

        public static Rectangle EmptyRect = Rectangle.Empty;

        // PRECT 本身也有 operator Rectangle
        public static Rectangle GetRectangle(PRECT block_rect)
        {
            return new Rectangle(block_rect.X,
    block_rect.Y,
    block_rect.Width,
    block_rect.Height);
        }

        // 对 Empty 的 Rectangle 跳过 Offset()
        public static Rectangle Offset(ref Rectangle rect,
            int x,
            int y)
        {
            if (x == 0 && y == 0)
                return rect;
            if (rect.IsEmpty == false)
                rect.Offset(x, y);
            return rect;
        }

        public static Rectangle Offset(Rectangle rect,
    int x,
    int y)
        {
            if (x == 0 && y == 0)
                return rect;
            if (rect.IsEmpty == false)
                rect.Offset(x, y);
            return rect;
        }

        // 注: Rectangle.Union() 有个缺陷，对其中一个是 Empty 的情况依然对合并结果有影响，所以写了这个函数代替
        public static Rectangle Union(Rectangle rect1, Rectangle rect2)
        {
            if (rect1.IsEmpty)
                return rect2;
            if (rect2.IsEmpty)
                return rect1;
            return System.Drawing.Rectangle.Union(rect1, rect2);
        }

        #region

        [DllImport("SHCore.dll", SetLastError = true)]
        private static extern bool SetProcessDpiAwareness(PROCESS_DPI_AWARENESS awareness);

        [DllImport("SHCore.dll", SetLastError = true)]
        private static extern void GetProcessDpiAwareness(IntPtr hprocess, out PROCESS_DPI_AWARENESS awareness);

        private enum PROCESS_DPI_AWARENESS
        {
            Process_DPI_Unaware = 0,
            Process_System_DPI_Aware = 1,
            Process_Per_Monitor_DPI_Aware = 2
        }

        public static void SetDpiAwareness()
        {
            // Vista on up = 6
            // http://stackoverflow.com/questions/17406850/how-can-we-check-if-the-current-os-is-win8-or-blue
            if (
                Environment.OSVersion.Version.Major > 6
                || (Environment.OSVersion.Version.Major == 6
                    && Environment.OSVersion.Version.Minor >= 2)
                )
            {
                // http://stackoverflow.com/questions/32148151/setprocessdpiawareness-not-having-effect
                /*
I've been trying to disable the DPI awareness on a ClickOnce application.
I quickly found out, it is not possible to specify it in the manifest, because ClickOnce does not support asm.v3 in the manifest file.
                 * */
                try
                {
                    // https://msdn.microsoft.com/en-us/library/windows/desktop/dn302122(v=vs.85).aspx
                    var result = SetProcessDpiAwareness(PROCESS_DPI_AWARENESS.Process_System_DPI_Aware);
                    // var setDpiError = Marshal.GetLastWin32Error();
                }
                catch
                {

                }

            }
        }

        #endregion
    }

    public class DpiUtil
    {
        // 获得一个控件的 DPI 参数
        public static SizeF GetDpiXY(Control control)
        {
            // testing
            // return new SizeF(192, 192);

            using (Graphics g = control.CreateGraphics())
            {
                return new SizeF(g.DpiX, g.DpiY);
            }
        }

        // 将 96DPI 下的长宽数字转换为指定 DPI 下的长宽值
        public static Size GetScalingSize(SizeF dpi_xy, int x, int y)
        {
            int width = Convert.ToInt32(x * (dpi_xy.Width / 96F));
            int height = Convert.ToInt32(y * (dpi_xy.Height / 96F));
            return new Size(width, height);
        }

        public static void ScalingSize(SizeF dpi_xy, ref int x, ref int y)
        {
            x = Convert.ToInt32(x * (dpi_xy.Width / 96F));
            y = Convert.ToInt32(y * (dpi_xy.Height / 96F));
        }

        public static int GetScalingX(SizeF dpi_xy, int x)
        {
            return Convert.ToInt32(x * (dpi_xy.Width / 96F));
        }

        public static int GetScalingY(SizeF dpi_xy, int y)
        {
            return Convert.ToInt32(y * (dpi_xy.Height / 96F));
        }

        public static Rectangle GetScaingRectangle(SizeF dpi_xy, Rectangle rect)
        {
            return new Rectangle(
                Convert.ToInt32(rect.X * (dpi_xy.Width / 96F)),
                Convert.ToInt32(rect.Y * (dpi_xy.Height / 96F)),
                Convert.ToInt32(rect.Width * (dpi_xy.Width / 96F)),
                Convert.ToInt32(rect.Height * (dpi_xy.Height / 96F))
            );
        }

        public static int Get96ScalingX(SizeF dpi_xy, int x)
        {
            return Convert.ToInt32(x * (96F / dpi_xy.Width));
        }

        public static int Get96ScalingY(SizeF dpi_xy, int y)
        {
            return Convert.ToInt32(y * (96F / dpi_xy.Height));
        }

        public static SizeF Get96ScalingSize(SizeF dpi_xy, SizeF size)
        {
            return new SizeF(
                size.Width * (96F / dpi_xy.Width),
                size.Height * (96F / dpi_xy.Height)
            );
        }

        public static Point Get96ScalingPoint(SizeF dpi_xy, Point pt)
        {
            return new Point(
                Convert.ToInt32(pt.X * (96F / dpi_xy.Width)),
                Convert.ToInt32(pt.Y * (96F / dpi_xy.Height))
            );
        }
    }


}
