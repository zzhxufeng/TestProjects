using System;
using System.Diagnostics;

namespace MultiThreadTest
{
    [TestClass]
    public class ThreadUsageTest
    {
        /// <summary>
        /// 创建线程
        /// 
        /// new Thread({Action}).Start();
        /// 
        /// ThreadStart是一个委托(可以理解为函数指针), 要按照它的形状声明一个函数
        /// 
        /// public delegate void ThreadStart();
        /// </summary>
        [TestMethod]
        public void TestThreadStart()
        { 
            var th = new Thread(start: PrintNumbers);
            th.Name = "Created Thread";
            th.Start();

            PrintNumbers();
        }

        static void PrintNumbers()
        {
            Console.WriteLine("Starting ...");
            Console.WriteLine(
                $"I'm running on thread id: {Thread.CurrentThread.ManagedThreadId}, " +
                $"thread name: {Thread.CurrentThread.Name}");
            for (int i = 0; i < 10; i++)
            {
                Console.Write(i + " ");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 当线程处于Sleep状态时, 它会尽可能少的占用CPU时间.
        /// </summary>
        [TestMethod]
        public void TestThreadSleepJoin()
        {
            var th = new Thread(PrintNumbersWithSleep);
            th.Name = "Created Thread";
            th.Start();
            // Wait until this thread end
            th.Join();

            PrintNumbers();
        }

        static void PrintNumbersWithSleep()
        {
            Console.WriteLine("Starting ...");
            Console.WriteLine(
                $"I'm running on thread id: {Thread.CurrentThread.ManagedThreadId}, " +
                $"thread name: {Thread.CurrentThread.Name}");
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(3));
                Console.Write(i + " ");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 已废弃: Thread#Abort
        /// </summary>
        [TestMethod]
        public void TestThreadAbort() 
        {
            Assert.ThrowsException<System.PlatformNotSupportedException>(() =>
            {
                var th = new Thread(PrintNumbersWithSleep);
                th.Start();
                Thread.Sleep(TimeSpan.FromSeconds(6));
                th.Abort();
            });
        }

        /// <summary>
        /// 摘要:
        ///     Specifies the execution states of a System.Threading.Thread.
        ///     
        /// [Flags]
        /// public enum ThreadState
        /// {
        ///     Running = 0,
        ///     StopRequested = 1,
        ///     SuspendRequested = 2,
        ///     Background = 4,
        ///     Unstarted = 8,
        ///     Stopped = 16,
        ///     WaitSleepJoin = 32,
        ///     Suspended = 64,
        ///     AbortRequested = 128,
        ///     Aborted = 256
        /// }
        /// </summary>
        [TestMethod]
        public void TestThreadState()
        {
            var th = new Thread(PrintNumbersWithThreadState);
            th.Start();

            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(5));
                Console.WriteLine($"Created Thread State: \n {th.ThreadState}");
            }
            th.Join();
        }

        static void PrintNumbersWithThreadState()
        {
            Console.WriteLine("Starting ...");
            Console.WriteLine(
                $"I'm running on thread id: {Thread.CurrentThread.ManagedThreadId}, \n" +
                $"thread name: {Thread.CurrentThread.Name}\n" +
                $"thread state: {Thread.CurrentThread.ThreadState}\n");
            for (int i = 0; i < 10; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(3));
                Console.Write(i + " ");
            }
            Console.WriteLine();
        }

        /// <summary>
        /// 分别在多核和单核上跑两个线程.
        /// 如果是多核, 那么两个线程的计数会差不多.
        /// 如果是单核, 那么优先级高的计数更多.
        /// 
        /// 标准输出: 
        /// Current thread priority: Normal
        /// Running on all cores available
        /// Th 1 with Highest priority has a count = 1,424,243,286
        /// Th 2 with Lowest priority has a count = 1,417,798,669
        /// Running on a single core.
        /// Th 1 with Highest priority has a count = 8,919,356,544
        /// Th 2 with Lowest priority has a count = 48,719,917
        /// </summary>
        [TestMethod]
        public void TestPriority()
        {
            Console.WriteLine("Current thread priority: {0}",
                Thread.CurrentThread.Priority);

            // multi core
            Console.WriteLine("Running on all cores available");
            
            RunThreads();

            Thread.Sleep(TimeSpan.FromSeconds(2));

            // single core
            Console.WriteLine("Running on a single core.");
            Process.GetCurrentProcess().ProcessorAffinity = new IntPtr(1);

            RunThreads();
        }

        static void RunThreads()
        {
            var sample = new ThreadSample();
            var th1 = new Thread(sample.CountNumbers);
            th1.Name = "Th 1";
            var th2 = new Thread(sample.CountNumbers);
            th2.Name = "Th 2";

            th1.Priority = ThreadPriority.Highest;
            th2.Priority = ThreadPriority.Lowest;

            th1.Start();
            th2.Start();

            Thread.Sleep(TimeSpan.FromSeconds(2));
            sample.Stop();

            th1.Join();
            th2.Join();
        }

        class ThreadSample
        {
            private bool _isStopped = false;

            public void Stop()
            {
                _isStopped = true;
            }

            internal void CountNumbers(object? obj)
            {
                long counter = 0;
                while (!_isStopped)
                {
                    counter++;
                }

                Console.WriteLine("{0} with {1, 11} priority " +
                    "has a count = {2, 13}",
                    Thread.CurrentThread.Name,
                    Thread.CurrentThread.Priority,
                    counter.ToString("N0"));
            }
        }

        /// <summary>
        /// 默认情况下, 显式创建的线程是前台线程.
        /// 
        /// 前台线程和后台线程的区别是:
        ///     进程会等待所有的前台线程完成之后再结束, 但如果只剩下后台线程, 则不会等待后台线程, 而是直接结束.
        ///     如果定义了一个不会完成的前台线程, 则主程序不会正常结束.
        ///     
        /// 单元测试的情况下测不出来...为什么?
        /// </summary>
        [TestMethod]
        public void TestForegroundBackground()
        {
            var print1 = new PrintNumbersWithIterationsClass(10);
            var print2 = new PrintNumbersWithIterationsClass(20);
            // ThreadStart只能接受无参的, 所以要定义一个类, 然后把参数的初始化给构造函数
            var th1 = new Thread(print1.PrintNumbersWithIterations);
            th1.Name = "ForegroundThread";
            var th2 = new Thread(print2.PrintNumbersWithIterations);
            th2.Name = "BackgroundThread";

            th1.IsBackground = false;
            th2.IsBackground = true;

            th1.Start();
            th2.Start();

            th1.Join();
            th2.Join();
        }

        public class PrintNumbersWithIterationsClass
        {
            public int iterations;

            public PrintNumbersWithIterationsClass(int iterations)
            {
                this.iterations = iterations;
            }

            public void PrintNumbersWithIterations()
            {
                for (int i = 0; i < iterations; i++)
                {
                    Thread.Sleep(TimeSpan.FromSeconds(0.5));
                    Console.WriteLine($"{Thread.CurrentThread.Name} prints {i}");
                }
            }
        }
        
    }
}
