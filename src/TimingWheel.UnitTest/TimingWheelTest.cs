using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace TimingWheel.UnitTest
{
    public class TimingWheelTest
    {
        [SetUp]
        public void Setup()
        {
        }

        /// <summary>
        /// 测试时间轮
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestTimingWheel()
        {
            var outputs = new Dictionary<string, DateTime>();

            // 秒级时间轮
            var timer = TimingWheelTimer.Build(TimeSpan.FromSeconds(1), 10);

            outputs.Add("00", DateTime.Now);

            timer.AddTask(TimeSpan.FromMilliseconds(5000), () => { outputs.Add("20", DateTime.Now); });
            timer.AddTask(TimeSpan.FromMilliseconds(2000), () => { outputs.Add("11", DateTime.Now); });

            timer.Start();

            timer.AddTask(TimeSpan.FromSeconds(12), () => { outputs.Add("30", DateTime.Now); });
            timer.AddTask(TimeSpan.FromSeconds(2), () => { outputs.Add("12", DateTime.Now); });

            await Task.Delay(TimeSpan.FromSeconds(15));
            timer.Stop();

            outputs.Add("99", DateTime.Now);

            Console.WriteLine(string.Join(Environment.NewLine, outputs.Select(o => $"{o.Key}, {o.Value:HH:mm:ss.ffff}")));

            Assert.AreEqual(6, outputs.Count);
            Assert.AreEqual(2, Calc(outputs.Skip(1).First().Value, outputs.First().Value));
            Assert.AreEqual(2, Calc(outputs.Skip(2).First().Value, outputs.First().Value));
            Assert.AreEqual(5, Calc(outputs.Skip(3).First().Value, outputs.First().Value));
            Assert.AreEqual(12, Calc(outputs.Skip(4).First().Value, outputs.First().Value));
        }

        /// <summary>
        /// 测试任务状态
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestTaskStatus()
        {
            var timer = TimingWheelTimer.Build(TimeSpan.FromSeconds(1), 10);
            timer.Start();

            var task1 = timer.AddTask(TimeSpan.FromSeconds(5), () => { Thread.Sleep(3000); });
            var task2 = timer.AddTask(TimeSpan.FromSeconds(5), () => { throw new Exception(); });
            var task3 = timer.AddTask(TimeSpan.FromSeconds(5), () => { throw new Exception(); });

            Assert.AreEqual(TimeTaskStatus.Wait, task1.TaskStatus);
            Assert.AreEqual(TimeTaskStatus.Wait, task2.TaskStatus);
            Assert.AreEqual(TimeTaskStatus.Wait, task3.TaskStatus);

            task3.Cancel();
            await Task.Delay(TimeSpan.FromSeconds(6));

            Assert.AreEqual(TimeTaskStatus.Running, task1.TaskStatus);
            Assert.AreEqual(TimeTaskStatus.Fail, task2.TaskStatus);
            Assert.AreEqual(TimeTaskStatus.Cancel, task3.TaskStatus);

            await Task.Delay(TimeSpan.FromSeconds(4));
            Assert.AreEqual(TimeTaskStatus.Success, task1.TaskStatus);

            timer.Stop();
        }

        private static int Calc(DateTime dt1, DateTime dt2)
        {
            return (int)(CutOffMillisecond(dt1) - CutOffMillisecond(dt2)).TotalSeconds;
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