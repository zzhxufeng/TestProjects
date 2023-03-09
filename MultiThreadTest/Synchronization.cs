using System.Diagnostics;

namespace MultiThreadTest
{
    [TestClass]
    public class Synchronization
    {
        /// <summary>
        /// 原子操作
        /// - 此操作所处的层(OS)不能发现其内部实现与结构, 那么这个操作是一个原子操作.
        /// - 原子操作需要硬件的支持.
        /// </summary>
        [TestMethod]
        public void TestInterlock()
        {
            int a = 0;
            var th1 = new Thread(() => Interlocked.Decrement(ref a));
            var th2 = new Thread(() => Interlocked.Increment(ref a));
            var th3 = new Thread(() => Interlocked.Decrement(ref a));
            var th4 = new Thread(() => Interlocked.Increment(ref a));

            Assert.AreEqual(a, 0);
        }

        /// <summary>
        /// 互斥量
        /// - 具有相同名字的Mutex尽管是不同的C#对象, 但都代表同一个操作系统互斥量对象.
        /// </summary>
        [TestMethod]
        public void TestMutex()
        {
            var th = new Thread(() =>
            {
                using (var m = new Mutex(false, "TEST_MUTEX"))
                {
                    m.WaitOne();
                    Console.WriteLine($"'{Thread.CurrentThread.Name}' got the mutex.");
                    Thread.Sleep(5000);
                    m.ReleaseMutex();
                    Console.WriteLine($"'{Thread.CurrentThread.Name}' release the mutex.");
                }
            });
            th.Name = "TestThread";

            var th1 = new Thread(() =>
            {
                using (var m = new Mutex(false, "TEST_MUTEX"))
                {
                    var res = m.WaitOne(TimeSpan.FromSeconds(3));
                    Console.WriteLine($"'{Thread.CurrentThread.Name}' got " +
                        $"the system mutex named 'Mutex': {res}");
                    if (res)
                    {
                        Console.WriteLine("Because this thread didn't get the mutex, " +
                            "this line won't be printed.");
                        m.ReleaseMutex();
                    }
                }
            });
            th1.Name = "TestThread1";

            th.Start();
            th1.Start();

            th.Join();
            th1.Join();
        }

        /// <summary>
        /// 信号量: 控制访问资源的线程数量.
        /// </summary>
        [TestMethod]
        public void TestSemaphoreSlim()
        {
            var sp = new SemaphoreSlim(4);
            Task[] tasks = new Task[10];
            int padding = 100;

            for (int i = 0; i < 10; i++)
            {
                var task = Task.Run(() => {
                    Console.WriteLine($"{Task.CurrentId}\twaits to continue...");
                    sp.Wait();

                    Console.WriteLine($"{Task.CurrentId}\tis running...");

                    /*
                     * 这里用Thread.Sleep而不用await Task.Delay:
                     *  await Task.Delay之后看不到TaskId了.
                     */
                    Interlocked.Add(ref padding, 100);
                    Thread.Sleep(1000 + padding);
                    Console.WriteLine($"{Task.CurrentId}\t\tis completed.");

                    sp.Release();
                });
                tasks[i] = (task);
            }
            Task.WaitAll(tasks);
        }

        /// <summary>
        /// 一个线程通过后就自动"Reset".
        /// AutoResetEvent 走的是内核时间, 适合长时间等待的那种(避免频繁切换上下文).
        /// </summary>
        [TestMethod]
        public void TestAutoResetEvent()
        {
            var e1 = new AutoResetEvent(false);
            var e2 = new AutoResetEvent(false);

            var th1 = new Thread(() =>
            {
                var time = DateTime.Now;
                Console.WriteLine($"[{time}]");
                Thread.Sleep(2000);
                time = DateTime.Now;
                Console.WriteLine($"[{time}] Release event1 after 2s...");
                e1.Set();

                time = DateTime.Now;
                Console.WriteLine($"[{time}]");
                Thread.Sleep(3000);
                time = DateTime.Now;
                Console.WriteLine($"[{time}] Release event2 after 3s...");
                e2.Set();
            });
            th1.Name = "Thread1";

            var th2 = new Thread(() =>
            {
                var start = DateTime.Now;
                Console.WriteLine($"[{start}] Wait on event 1...");
                e1.WaitOne();
                var end = DateTime.Now;
                Console.WriteLine($"[{end}] Event 1 released!");

                start = DateTime.Now;
                Console.WriteLine($"[{start}] Wait on event 2...");
                e2.WaitOne();
                end = DateTime.Now;
                Console.WriteLine($"[{end}] Event 2 released!");
            });
            th2.Name = "Thread2";

            th1.Start();
            th2.Start();

            th1.Join();
            th2.Join();

            e1.Dispose();
            e2.Dispose();
        }

        /// <summary>
        /// 不手动Reset就一直打开.
        /// 混合时间.
        /// </summary>
        [TestMethod]
        public void TestManualResetEventSlim()
        {
            var e = new ManualResetEventSlim(false);
            var d = (string threadName, int seconds) => {
                Console.WriteLine($"{threadName} falls to sleep.");
                Thread.Sleep(TimeSpan.FromSeconds(seconds));
                Console.WriteLine($"{threadName} waits for the gates to open!");
                e.Wait();
                Console.WriteLine($"{threadName} enters the gates!");
            };

            var th1 = new Thread(() => { d("Thread 1", 5); });
            var th2 = new Thread(() => { d("Thread 2", 6); });
            var th3 = new Thread(() => { d("Thread 3", 12); });

            th1.Start();
            th2.Start();
            th3.Start();

            Thread.Sleep(TimeSpan.FromSeconds(6));
            
            // 第一次开门
            Console.WriteLine("===THE GATES ARE NOW OPEN===");
            e.Set();
            Thread.Sleep(TimeSpan.FromSeconds(2));
            e.Reset();
            Console.WriteLine("===THE GATES HAVE BEEN CLOSED===");

            Thread.Sleep(TimeSpan.FromSeconds(10));

            // 第二次开门
            Console.WriteLine("===THE GATES ARE NOW OPEN FOR SECOND TIME===");
            e.Set();
            Thread.Sleep(TimeSpan.FromSeconds(2));
            e.Reset();
            Console.WriteLine("===THE GATES HAVE BEEN CLOSED===");

            th1.Join();
            th2.Join();
            th3.Join();

            e.Dispose();
        }

        /// <summary>
        /// 适合需要等待多个异步任务执行完成的情况.
        /// </summary>
        [TestMethod]
        public void TestCountDownEvent()
        {
            var watch = new Stopwatch();
            watch.Start();

            var e = new CountdownEvent(4);
            var th1 = new Thread(() => { Thread.Sleep(TimeSpan.FromSeconds(5));  e.Signal(); });
            var th2 = new Thread(() => { Thread.Sleep(TimeSpan.FromSeconds(3));  e.Signal(); });
            var th3 = new Thread(() => { Thread.Sleep(TimeSpan.FromSeconds(7));  e.Signal(); });
            var th4 = new Thread(() => { Thread.Sleep(TimeSpan.FromSeconds(9));  e.Signal(); });

            th1.Start(); th2.Start(); th3.Start(); th4.Start();

            th1.Join(); th2.Join(); th3.Join(); th4.Join();

            e.Wait();
            watch.Stop();
            Assert.IsTrue(watch.Elapsed.Seconds >= 9);

            e.Dispose();
        }

        /// <summary>
        /// 等待符合数量的参与者都到达SignalAndWait, 然后执行回调.
        /// </summary>
        /// <exception cref="Exception"></exception>
        [TestMethod]
        public void TestBarrier()
        {
            int count = 0;

            Barrier barrier = new Barrier(3, 
                (b) => {
                    Console.WriteLine("Post-Phase action: count={0}, phase={1}", count, b.CurrentPhaseNumber);
                    if (b.CurrentPhaseNumber == 2) throw new Exception("D'oh!");
                });

            // Nope -- changed my mind.  Let's make it five participants.
            barrier.AddParticipants(2);

            // Nope -- let's settle on four participants.
            barrier.RemoveParticipant();

            // This is the logic run by all participants
            Action action = () =>
            {
                Interlocked.Increment(ref count);
                barrier.SignalAndWait(); // during the post-phase action, count should be 4 and phase should be 0

                Interlocked.Increment(ref count);
                barrier.SignalAndWait(); // during the post-phase action, count should be 8 and phase should be 1

                Interlocked.Increment(ref count);
                try
                {
                    // The third time, SignalAndWait() will throw an exception and all participants will see it
                    barrier.SignalAndWait();
                }
                catch (BarrierPostPhaseException bppe)
                {
                    Console.WriteLine("Caught BarrierPostPhaseException: {0}", bppe.Message);
                }

                // The fourth time should be hunky-dory
                Interlocked.Increment(ref count);
                barrier.SignalAndWait(); // during the post-phase action, count should be 16 and phase should be 3
            };

            // Now launch 4 parallel actions to serve as 4 participants
            Parallel.Invoke(action, action, action, action);

            // It's good form to Dispose() a barrier when you're done with it.
            barrier.Dispose();
        }

    }
}
