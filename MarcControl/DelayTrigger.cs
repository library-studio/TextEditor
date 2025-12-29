using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 延时触发动作
    /// </summary>
    public class DelayTrigger : IDisposable
    {
        //private readonly object _invalidateLock = new object();

        // 最近一次击键(并规划刷新)的时刻
        private DateTime _lastPendingTime = DateTime.MinValue;
        // 时间长度。最近一次击键距离现在的时间长度，超过这个长度才会兑现刷新
        private TimeSpan _idleLength = TimeSpan.FromMilliseconds(500);

        private System.Windows.Forms.Timer _invalidateTimer;
        // 时钟间隔多少时间触发一次检查
        private int _interval = 100; // 可调，30-100ms 常用范围

        Action _action = null;

        public DelayTrigger(
            TimeSpan interval,
            TimeSpan idleLength)
        {
            _idleLength = idleLength;
            _interval = (int)interval.TotalMilliseconds;
            CreateTimer(_interval);
        }

        void CreateTimer(int interval)
        {
            _invalidateTimer = new System.Windows.Forms.Timer { Interval = interval };
            _invalidateTimer.Tick += (s, e) => Trigger();
        }

        void DestroyTimer()
        {
            if (_invalidateTimer != null)
            {
                _invalidateTimer.Stop();
                _invalidateTimer.Tick -= (s, e) => Trigger(); // optional unsubscribe
                _invalidateTimer.Dispose();
                _invalidateTimer = null;
            }
        }

        public void SetAction(Action action)
        {
            _action = null;
        }

        // parameters:
        //      action  要触发的动作。如果为 null，表示利用前次 Schedule() 设定的 action 来触发
        public void Schedule(Action action)
        {
            if (action != null)
                _action = action;

            {
                // _pendingInvalidateRect = parameter;
                _lastPendingTime = DateTime.UtcNow;
            }

            // 启动 debounce 计时器（如果尚未启动）
            if (!_invalidateTimer.Enabled)
                _invalidateTimer.Start();
        }

        private void Trigger()
        {
            if (DateTime.UtcNow < _lastPendingTime + _idleLength)
                return;

            _invalidateTimer.Stop();

            _action?.Invoke();
        }

        public void Dispose()
        {
            DestroyTimer();
        }
    }
}
