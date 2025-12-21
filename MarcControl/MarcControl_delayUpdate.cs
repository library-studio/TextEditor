using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 关于延迟更新的部分函数
    /// </summary>
    public partial class MarcControl
    {
        #region 延迟刷新

        // TODO: 可以用一个线程检查处理，当最后一个触键距离现在超过一定阈值，
        // 就集中把累积起来的 rect 进行一次 Invalidate()。这样如果连续输入，则一直不会刷新，等停顿了才会刷新
        // TODO: 虽然暂时不刷新，但需要立即 Update 输入位置的那一个字符，避免视觉感觉延迟

        private readonly object _invalidateLock = new object();
        private Rectangle _pendingInvalidateRect = System.Drawing.Rectangle.Empty;

        // 最近一次击键(并规划刷新)的时刻
        private DateTime _lastPendingTime = DateTime.MinValue;
        // 时间长度。最近一次击键距离现在的时间长度，超过这个长度才会兑现刷新
        private TimeSpan _idleLength = TimeSpan.FromMilliseconds(500);

        private System.Windows.Forms.Timer _invalidateTimer;
        // 时钟间隔多少时间触发一次检查
        private const int _invalidateDelayMs = 100; // 可调，30-100ms 常用范围


        void CreateTimer()
        {
            _invalidateTimer = new System.Windows.Forms.Timer { Interval = _invalidateDelayMs };
            _invalidateTimer.Tick += (s, e) => FlushInvalidate();
        }

        void DestroyTimer()
        {
            if (_invalidateTimer != null)
            {
                _invalidateTimer.Stop();
                _invalidateTimer.Tick -= (s, e) => FlushInvalidate(); // optional unsubscribe
                _invalidateTimer.Dispose();
                _invalidateTimer = null;
            }
        }

        private void ScheduleInvalidate(Rectangle rect)
        {
            //this.Invalidate(rect);
            //return;

            if (rect.IsEmpty)
                return;

            lock (_invalidateLock)
            {
                _pendingInvalidateRect = _pendingInvalidateRect.IsEmpty
                    ? rect
                    : Utility.Union(_pendingInvalidateRect, rect);
                _lastPendingTime = DateTime.UtcNow;
            }

            // 启动 debounce 计时器（如果尚未启动）
            if (!_invalidateTimer.Enabled)
                _invalidateTimer.Start();
        }

        private void FlushInvalidate()
        {
            if (DateTime.UtcNow < _lastPendingTime + _idleLength)
                return;

            Rectangle rect;
            lock (_invalidateLock)
            {
                rect = _pendingInvalidateRect;
                _pendingInvalidateRect = System.Drawing.Rectangle.Empty;
                _invalidateTimer.Stop();
            }

            if (rect.IsEmpty)
                return;

            // 如果覆盖了大部分或整个 client 区域，则失效整个控件
            if (rect.Width >= this.ClientSize.Width && rect.Height >= this.ClientSize.Height)
            {
                if (this.IsHandleCreated)
                    this.Invalidate();
                return;
            }

            // 保证在 UI 线程调用 Invalidate
            if (this.IsHandleCreated && this.InvokeRequired)
            {
                // throw new ArgumentException("不应在非 UI 线程调用");
                this.BeginInvoke((Action)(() =>
                {
                    this.Invalidate(rect);
                }));
            }
            else
                this.Invalidate(rect);
        }

        #endregion

    }
}
