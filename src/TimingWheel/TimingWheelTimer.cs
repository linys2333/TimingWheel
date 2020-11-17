using DelayQueue;
using System;
using System.Threading;
using System.Threading.Tasks;
using TimingWheel.Extensions;
using TimingWheel.Interfaces;

namespace TimingWheel
{
    /// <summary>
    /// 时间轮计时器，参考kafka时间轮算法实现
    /// </summary>
    public class TimingWheelTimer : ITimer
    {
        /// <summary>
        /// 时间槽延时队列，和时间轮共用
        /// </summary>
        private readonly DelayQueue<TimeSlot> _delayQueue = new DelayQueue<TimeSlot>();

        /// <summary>
        /// 时间轮
        /// </summary>
        private readonly TimingWheel _timingWheel;

        /// <summary>
        /// 任务总数
        /// </summary>
        private readonly AtomicInt _taskCount = new AtomicInt();

        /// <summary>
        /// 任务总数
        /// </summary>
        public int TaskCount => _taskCount.Get();

        private volatile CancellationTokenSource _cancelTokenSource;

        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim();

        /// <summary>
        /// 
        /// </summary>
        /// <param name="tickSpan">时间槽大小，毫秒</param>
        /// <param name="slotCount">时间槽数量</param>
        /// <param name="startMs">起始时间戳，标识时间轮创建时间</param>
        private TimingWheelTimer(long tickSpan, int slotCount, long startMs)
        {
            _timingWheel = new TimingWheel(tickSpan, slotCount, startMs, _taskCount, _delayQueue);
        }

        /// <summary>
        /// 构建时间轮计时器
        /// </summary>
        /// <param name="tickSpan">时间槽大小</param>
        /// <param name="slotCount">时间槽数量</param>
        /// <param name="startMs">起始时间戳，标识时间轮创建时间，默认当前时间</param>
        public static ITimer Build(TimeSpan tickSpan, int slotCount, long? startMs = null)
        {
            return new TimingWheelTimer((long)tickSpan.TotalMilliseconds,
                slotCount,
                startMs ?? DateTimeHelper.GetTimestamp());
        }

        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="timeout">过期时间，相对时间</param>
        /// <param name="delegateTask">延时任务</param>
        /// <returns></returns>
        public ITimeTask AddTask(TimeSpan timeout, Action delegateTask)
        {
            Requires.NotNull(delegateTask, nameof(delegateTask));

            var timeoutMs = DateTimeHelper.GetTimestamp() + (long)timeout.TotalMilliseconds;
            return AddTask(timeoutMs, delegateTask);
        }

        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="timeoutMs">过期时间戳，绝对时间</param>
        /// <param name="delegateTask">延时任务</param>
        /// <returns></returns>
        public ITimeTask AddTask(long timeoutMs, Action delegateTask)
        {
            Requires.NotNull(delegateTask, nameof(delegateTask));

            _lock.EnterReadLock();
            try
            {
                var task = new TimeTask(timeoutMs, delegateTask);
                AddTask(task);
                return task;
            }
            finally
            {
                _lock.ExitReadLock();
            }
        }

        /// <summary>
        /// 启动
        /// </summary>
        public void Start()
        {
            if (_cancelTokenSource != null)
            {
                return;
            }

            _cancelTokenSource = new CancellationTokenSource();

            // 时间轮运行线程
            Task.Factory.StartNew(() => Run(_cancelTokenSource.Token),
                _cancelTokenSource.Token,
                TaskCreationOptions.LongRunning,
                TaskScheduler.Default);
        }

        /// <summary>
        /// 停止
        /// </summary>
        public void Stop()
        {
            Cancel();
            _delayQueue.Clear();
        }

        /// <summary>
        /// 暂停
        /// </summary>
        public void Pause()
        {
            Cancel();
        }

        /// <summary>
        /// 恢复
        /// </summary>
        public void Resume()
        {
            Start();
        }

        /// <summary>
        /// 取消任务
        /// </summary>
        private void Cancel()
        {
            if (_cancelTokenSource != null)
            {
                _cancelTokenSource.Cancel();
                _cancelTokenSource.Dispose();
                _cancelTokenSource = null;
            }
        }

        /// <summary>
        /// 运行
        /// </summary>
        private void Run(CancellationToken token)
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    Step(token);
                }
            }
            catch (Exception e)
            {
                if (e is OperationCanceledException)
                {
                    return;
                }

                throw;
            }
        }

        /// <summary>
        /// 推进时间轮
        /// </summary>
        /// <param name="token"></param>
        private void Step(CancellationToken token)
        {
            // 阻塞式获取，到期的时间槽才会出队
            if (_delayQueue.TryTake(out var slot, token))
            {
                _lock.EnterWriteLock();
                try
                {
                    while (!token.IsCancellationRequested)
                    {
                        // 推进时间轮
                        _timingWheel.Step(slot.TimeoutMs.Get());

                        // 到期的任务会重新添加进时间轮，那么下一层时间轮的任务重新计算后可能会进入上层时间轮。
                        // 这样就实现了任务在时间轮中的传递，由大精度的时间轮进入小精度的时间轮。
                        slot.Flush(AddTask);

                        // Flush之后可能有新的slot入队，可能仍旧过期，因此尝试继续处理，直到没有过期项。
                        if (!_delayQueue.TryTakeNoBlocking(out slot))
                        {
                            break;
                        }
                    }
                }
                finally
                {
                    _lock.ExitWriteLock();
                }
            }
        }

        /// <summary>
        /// 添加任务
        /// </summary>
        /// <param name="timeTask">延时任务</param>
        private void AddTask(TimeTask timeTask)
        {
            // 添加失败，说明该任务已到期，需要执行了
            if (!_timingWheel.AddTask(timeTask))
            {
                if (timeTask.IsWaiting)
                {
                    // TODO：是否放入自定义线程池
                    Task.Run(timeTask.Run);
                }
            }
        }
    }
}