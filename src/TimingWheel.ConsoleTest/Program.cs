using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TimingWheel.Interfaces;

namespace TimingWheel.ConsoleTest
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var timer = TimingWheelTimer.Build(TimeSpan.FromSeconds(1), 60);
            timer.Start();

            // 多线程测试
            var tasks = new List<Task>();
            for (int i = 0; i < 10; i++)
            {
                tasks.Add(Task.Factory.StartNew(() => AddTasks(timer).GetAwaiter().GetResult(),
                    TaskCreationOptions.LongRunning));
            }

            await Task.WhenAll(tasks);
            Console.WriteLine("任务添加完毕！");

            while (timer.TaskCount > 0)
            {
                Console.WriteLine($"[{DateTime.Now}] 剩余任务数：{timer.TaskCount}");
                Console.WriteLine($"剩余任务数：{timer.TaskCount}");

                await Task.Delay(TimeSpan.FromMinutes(1));
            }

            timer.Stop();
        }

        private static async Task AddTasks(ITimer timer)
        {
            var r = new Random();
            var taskCount = 0;
            var threadId = Thread.CurrentThread.ManagedThreadId;

            while (true)
            {
                for (int i = 0; i < 20; i++, taskCount++)
                {
                    var now = DateTime.Now;
                    var delay = TimeSpan.FromSeconds(r.Next(10, 7200));

                    timer.AddTask(delay, () =>
                    {
                        var runTime = DateTime.Now;
                        var actualDelay = CutOffMillisecond(runTime) - CutOffMillisecond(now);
                        var actualDelay2 = runTime - now;

                        Console.WriteLine($"添加任务线程：{threadId}，" +
                                          $"起始时间：{now:HH:mm:ss.fff}，" +
                                          $"执行时间：{runTime:HH:mm:ss.fff}，" +
                                          $"预期延时：{delay.TotalSeconds}s，" +
                                          $"实际延时：{actualDelay.TotalSeconds}s，" +
                                          $"精确延时：{actualDelay2.TotalSeconds}s");
                    });
                }

                Console.WriteLine($"[{DateTime.Now}][线程{threadId}] 累计任务数：{taskCount}");
                Console.WriteLine($"累计任务数：{taskCount}");

                if (taskCount >= 100)
                {
                    break;
                }

                await Task.Delay(TimeSpan.FromHours(1));
            }
        }

        /// <summary>
        /// 截掉毫秒部分
        /// </summary>
        /// <param name="dt"></param>
        /// <returns></returns>
        private static DateTime CutOffMillisecond(DateTime dt)
        {
            return new DateTime(dt.Ticks - (dt.Ticks % TimeSpan.TicksPerSecond), dt.Kind);
        }
    }
}
