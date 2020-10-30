using System;
using TimingWheel.Extensions;
using TimingWheel.Interfaces;

namespace TimingWheel
{
    /// <summary>
    /// 定时任务
    /// </summary>
    internal class TimeTask : ITimeTask
    {
        /// <summary>
        /// 过期时间戳
        /// </summary>
        public long TimeoutMs { get; }

        /// <summary>
        /// 延时任务
        /// </summary>
        public Action DelayTask { get; }

        /// <summary>
        /// 所属时间槽
        /// </summary>
        public volatile TimeSlot TimeSlot;

        /// <summary>
        /// 任务是否已结束
        /// </summary>
        public bool IsEnd => _isEnd;
        private volatile bool _isEnd;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeout">过期时间，相对时间</param>
        /// <param name="delayTask">延时任务</param>
        public TimeTask(TimeSpan timeout, Action delayTask)
        {
            TimeoutMs = DateTimeHelper.GetTimestamp() + (long)timeout.TotalMilliseconds;
            DelayTask = delayTask;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="timeoutMs">过期时间戳，绝对时间</param>
        /// <param name="delayTask">延时任务</param>
        public TimeTask(long timeoutMs, Action delayTask)
        {
            TimeoutMs = timeoutMs;
            DelayTask = delayTask;
        }

        /// <summary>
        /// 执行任务
        /// </summary>
        public void Run()
        {
            if (!_isEnd)
            {
                _isEnd = true;
                Remove();
                DelayTask();
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public void Cancel()
        {
            if (!_isEnd)
            {
                _isEnd = true;
                Remove();
            }
        }

        /// <summary>
        /// 移除任务
        /// </summary>
        public void Remove()
        {
            while (TimeSlot != null && !TimeSlot.RemoveTask(this))
            {
                // 如果task被另一个线程移动到了其它slot中，就会移除失败，需要重试
            }

            TimeSlot = null;
        }
    }
}