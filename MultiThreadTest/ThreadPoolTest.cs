using System.ComponentModel;

namespace MultiThreadTest
{
    [TestClass]
    public class ThreadPoolTest
    {
        #region Delegate.BeginInvoke
        /// <summary>
        /// The BeginInvoke is no longer supported since .NET Framework 4.5.
        /// https://devblogs.microsoft.com/dotnet/migrating-delegate-begininvoke-calls-for-net-core/
        /// </summary>
        [TestMethod]
        public void TestDelegateInThreadPool()
        {
            Assert.ThrowsException<System.PlatformNotSupportedException>(() => { 
                int threadId = 0;
                RunOnThreadPool poolDelegate = Test;

                var t = new Thread(() => { Test(out threadId); });
                t.Start();
                t.Join();
                Console.WriteLine("Thread id: {0}", threadId);


                var r = poolDelegate.BeginInvoke(out threadId, CallBack, "a delegate async call");
                var res = poolDelegate.EndInvoke(out threadId, r);

                Console.WriteLine("Thread pool worker thread id : {0}", threadId);
                Console.WriteLine(res);

                Thread.Sleep(TimeSpan.FromSeconds(2));
            });
        }
        private delegate string RunOnThreadPool(out int threadId);
        private static void CallBack(IAsyncResult ar)
        {
            Console.WriteLine("Starting a callback");
            Console.WriteLine("State passed to a callback: {0}", ar.AsyncState);
            Console.WriteLine("Is thread pool thread: {0}", Thread.CurrentThread.IsThreadPoolThread);
            Console.WriteLine("Thread pool thread id {0}", Thread.CurrentThread.ManagedThreadId);
        }

        private static string Test(out int threadId)
        {
            Console.WriteLine("Starting");
            Console.WriteLine("Is thread pool thread: {0}", Thread.CurrentThread.IsThreadPoolThread);
            Thread.Sleep(TimeSpan.FromSeconds(2));
            threadId = Thread.CurrentThread.ManagedThreadId;
            return string.Format("Thread pool worker id: {0}", threadId);
        }
       #endregion

        #region ThreadPool.QueueUserWorkItem
        [TestMethod]
        public void TestQueueUserWorkItem()
        {
            var stateInfo = "some info";
            // Queue the task.
            ThreadPool.QueueUserWorkItem((state) =>
            {
                /*This 'state' is passed from the api 'QueueUserWorkItem', which could be null if
                it doesn't passed in.*/
                Console.WriteLine(state.ToString());
                Console.WriteLine($"Hello from thread pool thread: {Thread.CurrentThread.ManagedThreadId}");
            }, stateInfo);

            Console.WriteLine("Main thread does some work, then sleeps.");
            Thread.Sleep(1000);

            Console.WriteLine("Main thread exits.");
        }
        #endregion

        #region CancellationToken
        [TestMethod]
        public void TestCancellationToken()
        {
            using (var cts = new CancellationTokenSource())
            {
                var token = cts.Token;
                ThreadPool.QueueUserWorkItem(_ => AsyncOperation(token));
                Thread.Sleep(TimeSpan.FromSeconds(2));
                cts.Cancel();
            }

            Console.WriteLine("===");

            using (var cts = new CancellationTokenSource())
            {
                var token = cts.Token;
                ThreadPool.QueueUserWorkItem(_ => AsyncOperation(token));
                Thread.Sleep(TimeSpan.FromSeconds(2));
                //cts.Cancel();
            }
            // Wait for background threads to be finished.
            Thread.Sleep(TimeSpan.FromSeconds(10));
        }

        static void AsyncOperation(CancellationToken token)
        {
            Console.WriteLine("Task started.");
            for (int i = 0; i < 5; i++)
            {
                if (token.IsCancellationRequested)
                {
                    Console.WriteLine("Cancellation requested, return.");
                    return;
                }
                Thread.Sleep(1000);
                Console.WriteLine($"Do something... {i}");
            }
            Console.WriteLine("Task completed.");
        }
        #endregion

        #region System.Threading.Timer
        /// <summary>
        /// 在线程池上以一定时间间隔不断执行回调.
        /// 每次的回调执行不一定在同一个线程上.
        /// </summary>
        [TestMethod]
        public void TestTimer()
        {
            var a = 0;
            var timer = new Timer(_ =>
            {
                Interlocked.Increment(ref a);
                Console.WriteLine($"Thread id : {Thread.CurrentThread.ManagedThreadId}");
            }, null, 1000, 1000);

            Thread.Sleep(6500);
            timer.Dispose();

            Assert.AreEqual(a, 6);
        }
        #endregion

        #region BackgroundWorker
        static void Worker_DoWork(object? sender, DoWorkEventArgs e)
        {
            var bw = (BackgroundWorker) sender;
            Console.WriteLine("Do something in thread pool thread.");
            for (int i = 1; i <= 5; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(1));
                bw.ReportProgress(i * 20);
            }
            e.Result = 100;
        }

        static void Worker_ProgressChanged(object? sender, ProgressChangedEventArgs e)
        {
            Console.WriteLine(e.ProgressPercentage);
        }

        static void Worker_Completed(object? sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Error != null) Console.WriteLine(e.Error.ToString());
            else if (e.Cancelled) Console.WriteLine(e.Cancelled.ToString());
            else Console.WriteLine("Result: " + e.Result);
        }

        [TestMethod]
        public void TestBackgroundWorker()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;

            worker.DoWork += Worker_DoWork;
            worker.ProgressChanged += Worker_ProgressChanged;
            worker.RunWorkerCompleted += Worker_Completed;

            worker.RunWorkerAsync();

            // wait for background threads to finish.
            Thread.Sleep(10000);
        }
        #endregion
    }
}
