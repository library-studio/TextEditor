using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 提示区和右侧之间的分割条
    /// </summary>
    public partial class MarcControl
    {
        // 顶级分割条位置
        [Browsable(false)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public int CaptionPixelWidth
        {
            get
            {
                return _marcMetrics.CaptionPixelWidth;
            }
            set
            {
                if (value < 0)
                    value = 0;

                if (value < Metrics.DefaultSplitterPixelWidth)
                {
                    value = Metrics.DefaultSplitterPixelWidth;
                }

                if (_marcMetrics.CaptionPixelWidth != value)
                {
                    _marcMetrics.CaptionPixelWidth = value;
                    // 迫使重新布局 Layout
                    Relayout(_record.MergeText());
                    // TODO: 折行位置发生变化，可能会让 _caretInfo.X 失效
                    _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标 
                }
            }
        }

        // 分割条拖动过程中的 x 位置
        int _splitterX = 0;
        int _splitterStartX = 0;
        bool _splitting = false;

        void StartSplitting(int x)
        {
            _splitterX = x;
            _splitterStartX = x;

            _splitting = true;

            DrawTraker();
        }

        void MoveSplitting(int x)
        {
            Cursor = Cursors.SizeWE;

            // 消上次残余的一根
            DrawTraker();

            _splitterX = x;

            // 绘制本次的一根
            DrawTraker();
        }

        bool FinishSplitting(int x)
        {
            // 消最后残余的一根
            DrawTraker();

            // 计算差额
            var delta = _splitterX - _splitterStartX;

            var changed = _marcMetrics.DeltaCaptionWidth(1, delta);
            /*
            _fieldProperty.CaptionPixelWidth += delta;
            _fieldProperty.CaptionPixelWidth = Math.Max(_fieldProperty.SplitterPixelWidth, _fieldProperty.CaptionPixelWidth);
            */

            _splitting = false;
            _splitterStartX = 0;
            _splitterX = 0;
            if (changed)
            {
                // 迫使重新布局 Layout
                Relayout(_record.MergeText());
                // TODO: 折行位置发生变化，可能会让 _caretInfo.X 失效
                _lastX = _caretInfo.X; // 调整最后一次左右移动的 x 坐标
                // this.Invalidate();
                return true;
            }
            return false;
        }

        void DrawTraker()
        {
            // Debug.WriteLine($"_splitterX={_splitterX}");

            Point p1 = new Point(_splitterX, 0);
            p1 = this.PointToScreen(p1);

            Point p2 = new Point(_splitterX, this.ClientSize.Height);
            p2 = this.PointToScreen(p2);

            /*
            // 获取当前屏幕的 DPI 缩放因子
            // float dpiScale = this.DeviceDpi / 96f;
            // 逻辑坐标转为物理像素
            p1 = this.PointToScreen(DpiUtil.Get96ScalingPoint(_dpiXY, p1));
            p2 = this.PointToScreen(DpiUtil.Get96ScalingPoint(_dpiXY, p2));
            */

            // 注意，必须用当前屏幕的实际 DPI 来绘制
            ControlPaint.DrawReversibleLine(p1,
                p2,
                SystemColors.Control);
        }

        // 检测分割条和 Caption 区域
        // 参见 enum FieldRegion 定义
        // return:
        //      -3  按钮区域
        //      -2  Caption 区域
        //      -1  Splitter 区域
        //      0   其它区域(包括 name indicator 和 content 区域)
        int TestSplitterArea(int x)
        {
            x += this.HorizontalScroll.Value;   // 窗口坐标变换为内容坐标
            return _marcMetrics.TestSplitterArea(x);
        }

    }
}
