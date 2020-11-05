# TimingWheel

C#版分层时间轮算法，参考kafka TimingWheel实现。

## NuGet

| .NET Standard 2.0+ |
| ----- |
| Install-Package [TimingWheel.Net](https://www.nuget.org/packages/TimingWheel.Net) |

## 使用

``` csharp

// 定义一个秒级时间轮
var timer = TimingWheelTimer.Build(TimeSpan.FromSeconds(1), 60);
timer.Start();

// 添加延时任务
var task1 = timer.AddTask(TimeSpan.FromSeconds(5), () =>
{
    Console.WriteLine($"{DateTime.Now}, Task1: 执行");
});
var task2 = timer.AddTask(TimeSpan.FromSeconds(5), () =>
{
    Console.WriteLine($"{DateTime.Now}, Task2: 执行");
    throw new Exception();
});

Console.WriteLine($"{DateTime.Now}, Task1: {task1.TaskStaus}");
Console.WriteLine($"{DateTime.Now}, Task2: {task2.TaskStaus}");

// 等待到期执行
await Task.Delay(TimeSpan.FromSeconds(6));

Console.WriteLine($"{DateTime.Now}, Task1: {task1.TaskStaus}");
Console.WriteLine($"{DateTime.Now}, Task2: {task2.TaskStaus}");

timer.Stop();

// 控制台输出
11/5 星期四 14:48:25, Task1: Wait
11/5 星期四 14:48:25, Task2: Wait
11/5 星期四 14:48:30, Task1: 执行
11/5 星期四 14:48:30, Task2: 执行
11/5 星期四 14:48:31, Task1: Success
11/5 星期四 14:48:31, Task2: Fail

```

## 原理

待补充...

## 更新日志

v1.0.1更新内容：  
1、细分任务状态