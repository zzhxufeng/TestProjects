using System.Diagnostics;

namespace MultiThreadTest
{
    [TestClass]
    public class ThreadUsageTest
    {
        #region 线程的基本使用
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
        #endregion

        #region 线程状态
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
        #endregion

        #region 线程优先级
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
        #endregion

        #region 前台线程和后台线程
        /// <summary>
        /// 默认情况下, 显式创建的线程是前台线程.
        /// 
        /// 前台线程和后台线程的区别是:
        ///     进程会等待所有的前台线程完成之后再结束, 但如果只剩下后台线程, 则不会等待后台线程, 而是直接结束.
        ///     如果定义了一个不会完成的前台线程, 则主程序不会正常结束.
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
        #endregion

        #region 线程传递参数的三种方式
        /// <summary>
        /// 向线程传递参数的方式:
        /// 1. 通过自定义类型, 然后在类型的ctor中传入参数, 并在类型中定义无返回值无参数列表的符合委托ThreadStart的方法, 传给Thread构造函数
        /// 2. 通过Thread构造函数传入ParameterizedThreadStart委托类型的方法, 并使用Thread.Start(object? parameter)传入参数
        /// 3. 闭包
        /// </summary>
        [TestMethod]
        public void TestPassParamsToThread()
        {
            // 1. 通过类型的构造函数传入参数给线程
            var cls = new PrintNumbersWithIterationsClass(10);
            ThreadStart ts = cls.PrintNumbersWithIterations;
            var thCls = new Thread(ts);
            thCls.Start();

            // 2. 通过Thread.Start(object? parameter)
            ParameterizedThreadStart pts = PrintNumbersWithParam;
            var thStart = new Thread(PrintNumbersWithParam);
            thStart.Start(10);

            // 3. 闭包, 和1一个意思
            var thCls1 = new Thread(() => PrintNumbersWithParam(10));
            thCls1.Start();

            thCls.Join();
            thStart.Join();
            thCls1.Join();
        }

        static void PrintNumbersWithParam(object iterations)
        {
            int it = (int)iterations;
            for (int i = 0; i < it; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                Console.WriteLine($"{Thread.CurrentThread.Name} prints {i}");
            }
        }
        #endregion

        #region lock
        /// <summary>
        /// 测试lock的用法
        /// Counter的线程同步和非线程同步的实现
        /// 
        /// 现象:
        /// 在不同线程中对同一变量做同样多次数的加减后
        ///     非线程同步的实现输出的结果不确定
        ///     线程同步的实现输出结果总为0
        /// 
        /// 用法:
        /// private readonly object _syncRoot = new object();
        /// 
        /// lock(_syncRoot) { // ... }
        /// </summary>
        [TestMethod]
        public void TestLock()
        {
            for (int testTimes = 0; testTimes < 10; testTimes++)
            {
                var c = new Counter();
                var t1 = new Thread(() => TestCounter(c));
                var t2 = new Thread(() => TestCounter(c));
                var t3 = new Thread(() => TestCounter(c));

                t1.Start();
                t2.Start();
                t3.Start();

                t1.Join();
                t2.Join();
                t3.Join();

                Console.WriteLine("[W] Total count: {0}", c.Count);
                Console.WriteLine("=========================");

                var cl = new CounterWithLock();
                t1 = new Thread(() => TestCounter(cl));
                t2 = new Thread(() => TestCounter(cl));
                t3 = new Thread(() => TestCounter(cl));

                t1.Start();
                t2.Start();
                t3.Start();

                t1.Join();
                t2.Join();
                t3.Join();
                Console.WriteLine("[R] Total count: {0}", cl.Count);
                Console.WriteLine("=========================");
            }
        }

        static void TestCounter(CounterBase c)
        {
            for (int i = 0; i < 100000; i++)
            {
                c.Increment();
                c.Decrement();
            }
        }

        class Counter : CounterBase
        {
            public override int Count { get; set; }

            public override void Increment()
            {
                Count++;
            }
            public override void Decrement()
            {
                Count--;
            }
        }

        class CounterWithLock : CounterBase
        {
            private readonly object _syncRoot = new object();
            public override int Count { get; set; }

            public override void Increment()
            {
                lock (_syncRoot)
                {
                    Count++;
                }
            }

            public override void Decrement()
            {
                lock (_syncRoot)
                {
                    Count--;
                }
            }
        }

        abstract class CounterBase
        {
            public abstract int Count { get; set; }
            public abstract void Increment();
            public abstract void Decrement();
        }

        #endregion

        #region Monitor
        static void LockTooMuch(object lock1, object lock2)
        {
            lock (lock1)
            {
                Thread.Sleep(1000);
                lock (lock2) ;
            }
        }

        /// <summary>
        /// t1              t2
        /// ---------------------------
        /// lock1           lock2
        /// |               |
        /// 1s              1s
        /// |               |
        /// 尝试拿lock2      尝试拿lock1
        /// 
        /// 循环等待, 死锁.
        /// </summary>
        [TestMethod]
        public void TestDeadLock()
        {
            object lock1 = new object();
            object lock2 = new object();

            new Thread(() => LockTooMuch(lock1, lock2)).Start();

            lock (lock2)
            {
                Thread.Sleep(1000);
                Console.WriteLine("Monitor.TryEnter在超时后退出");
                if (Monitor.TryEnter(lock1, TimeSpan.FromSeconds(5)))
                {
                    Console.WriteLine("获取资源成功!");
                }
                else
                {
                    Console.WriteLine("获取资源超时.");
                }
            }

            //Console.WriteLine("=======================");

            //new Thread(() => LockTooMuch(lock1, lock2)).Start();

            //lock (lock2)
            //{
            //    Console.WriteLine("死锁...");
            //    Thread.Sleep(1000);
            //    lock (lock1)
            //    {
            //        Console.WriteLine("获取资源成功!");
            //    }
            //}
        }
        #endregion

        #region try/catch
        [TestMethod]
        public void TestTryCatch()
        {
            string s1 = "";
            string s2 = "";

            var th1 = new Thread(() =>
            {
                try
                {
                    throw new Exception("An intended exception.");
                }
                catch (Exception ex)
                {
                    s1 = ex.Message;
                }
            });
            th1.Start();


            var th2 = new Thread(() =>
            {
                throw new Exception("An intended exception.");
            });
            try
            {
                th2.Start();
            }
            catch (Exception ex)
            {
                s2 = ex.Message;
            }
   

            th1.Join();

            Assert.AreEqual(s1, "An intended exception.");
            Assert.AreEqual(s2, "");
        }
        #endregion
    }
}
