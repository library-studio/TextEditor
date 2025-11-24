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
    /// <summary>
    /// 实用类
    /// </summary>
    public class Utility
    {
        public static Rectangle GetRectangle(PRECT block_rect)
        {
            return new Rectangle(block_rect.X,
    block_rect.Y,
    block_rect.Width,
    block_rect.Height);
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
