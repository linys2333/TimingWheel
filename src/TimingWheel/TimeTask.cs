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
        /// 任务状态
        /// </summary>
        public TimeTaskStaus TaskStaus { get; private set; } = TimeTaskStaus.Wait;

        /// <summary>
        /// 任务是否等待中
        /// </summary>
        public bool IsWaiting => TaskStaus == TimeTaskStaus.Wait;

        private readonly object _lock = new object();

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
            if (!IsWaiting)
            {
                return;
            }

            lock (_lock)
            {
                if (IsWaiting)
                {
                    TaskStaus = TimeTaskStaus.Running;
                    Remove();
                }
            }

            if (TaskStaus == TimeTaskStaus.Running)
            {
                try
                {
                    DelayTask();
                    TaskStaus = TimeTaskStaus.Success;
                }
                catch
                {
                    // 由DelayTask内部处理异常，这里不处理
                    TaskStaus = TimeTaskStaus.Fail;
                }
            }
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        public bool Cancel()
        {
            if (!IsWaiting)
            {
                return false;
            }

            lock (_lock)
            {
                if (IsWaiting)
                {
                    TaskStaus = TimeTaskStaus.Cancel;
                    Remove();
                    return true;
                }
            }

            return false;
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