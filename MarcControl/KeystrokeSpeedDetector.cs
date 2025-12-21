// csharp MarcControl\KeystrokeSpeedDetector.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace LibraryStudio.Forms
{
    /// <summary>
    /// 监测用户击键速率（keys/sec）。基于滑动时间窗口。
    /// 使用：在每次按键时调用 RecordKey()。订阅 ThresholdExceeded/ThresholdCleared 事件。
    /// </summary>
    public class KeystrokeSpeedDetector
    {
        private readonly object _lock = new object();
        private readonly Queue<DateTime> _timestamps = new Queue<DateTime>();
        private readonly TimeSpan _window;
        private readonly double _thresholdKeysPerSecond;
        private bool _isAbove;

        /// <summary>
        /// 当速率超过阈值时触发（首次超过时触发一次）
        /// </summary>
        public event EventHandler ThresholdExceeded;

        /// <summary>
        /// 当速率从超阈值回落到阈值以下时触发（首次回落时触发一次）
        /// </summary>
        public event EventHandler ThresholdCleared;

        /// <summary>
        /// 构造。
        /// thresholdKeysPerSecond: 判定为“快速输入”的阈值（键/秒）
        /// window: 计算窗口长度，短窗口响应快，长窗口更平滑。缺省 1 秒。
        /// </summary>
        public KeystrokeSpeedDetector(double thresholdKeysPerSecond = 8.0, TimeSpan? window = null)
        {
            if (thresholdKeysPerSecond <= 0)
                throw new ArgumentOutOfRangeException(nameof(thresholdKeysPerSecond));
            _thresholdKeysPerSecond = thresholdKeysPerSecond;
            _window = window ?? TimeSpan.FromSeconds(1);
        }

        /// <summary>
        /// 在每次按键（可在 UI 线程）处调用。此方法会更新内部状态并在必要时触发事件。
        /// </summary>
        public void RecordKey()
        {
            var now = DateTime.UtcNow;
            bool justExceeded = false;
            bool justCleared = false;

            lock (_lock)
            {
                // 清理旧时间戳
                while (_timestamps.Count > 0 && now - _timestamps.Peek() > _window)
                    _timestamps.Dequeue();

                _timestamps.Enqueue(now);

                // 计算当前速率
                double currentRate = _timestamps.Count / Math.Max(1.0, _window.TotalSeconds);

                if (!_isAbove && currentRate > _thresholdKeysPerSecond)
                {
                    _isAbove = true;
                    justExceeded = true;
                }
                else if (_isAbove && currentRate <= _thresholdKeysPerSecond)
                {
                    _isAbove = false;
                    justCleared = true;
                }
            }

            // Debug.WriteLine(_isAbove);

            if (justExceeded)
                ThresholdExceeded?.Invoke(this, EventArgs.Empty);
            if (justCleared)
                ThresholdCleared?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// 当前窗口内估计的击键速率（keys/sec）。
        /// </summary>
        public double CurrentRate
        {
            get
            {
                lock (_lock)
                {
                    var now = DateTime.UtcNow;
                    while (_timestamps.Count > 0 && now - _timestamps.Peek() > _window)
                        _timestamps.Dequeue();
                    return _timestamps.Count / Math.Max(1.0, _window.TotalSeconds);
                }
            }
        }

        /// <summary>
        /// 是否当前处于“超过阈值”状态。
        /// </summary>
        public bool IsAboveThreshold
        {
            get { lock (_lock) { return _isAbove; } }
        }

        // 同时探测是否超过阈值和记录击键
        public bool Detect()
        {
            var ret = IsAboveThreshold;
            RecordKey();
            return ret;
        }

        /// <summary>
        /// 清除内部状态
        /// </summary>
        public void Reset()
        {
            lock (_lock)
            {
                _timestamps.Clear();
                _isAbove = false;
            }
        }
    }
}
