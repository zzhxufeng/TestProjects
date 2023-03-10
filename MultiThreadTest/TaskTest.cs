using System.ComponentModel;
using System.Net.WebSockets;

namespace MultiThreadTest
{
    [TestClass]
    public class TaskTest
    {
        #region Run task
        [TestMethod]
        public void TestTask()
        {
            var t1 = new Task(() => TaskMethod("Task 1"));
            t1.Start();

            Task.Run(() => TaskMethod("Task 2"));

            Task.Factory.StartNew(() => TaskMethod("Task 3"));

            // LongRunning Task not on thread pool thread.
            Task.Factory.StartNew(() => TaskMethod("Task 4"), TaskCreationOptions.LongRunning);

            // wait for all background threads.
            Thread.Sleep(1000);
        }

        static void TaskMethod(string name)
        {
            Console.WriteLine($"Task {name} is running on thread: {Thread.CurrentThread.ManagedThreadId}.\t" +
                $"IsThreadPoolThread: {Thread.CurrentThread.IsThreadPoolThread}");
        }
        #endregion

        #region Task.ContinueWith
        [TestMethod]
        public void TestContinueWith()
        {
            var tk = new Task(() => 
            {
                Thread.Sleep(TimeSpan.FromSeconds(2));
                Console.WriteLine("Start running task...");
                Thread.Sleep(TimeSpan.FromSeconds(2));
                Console.WriteLine("Task completed.");
            });

            tk.ContinueWith(_ =>
            {
                Console.WriteLine("Running after task completed...");
            });

            tk.Start();

            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
        #endregion

        #region Cancellation
        [TestMethod]
        public void TestCancellationTask()
        {
            // Cancel task before task start.
            var cts = new CancellationTokenSource();

            var longTask = new Task<int>(() => TaskMethod("Task 1", 10, cts.Token), cts.Token);
            Console.WriteLine(longTask.Status);
                
            cts.Cancel();

            Console.WriteLine(longTask.Status);

            Console.WriteLine("First task has been cancelled before execution.");

            Assert.ThrowsException<InvalidOperationException>(() => longTask.Start());
            
            cts.Dispose();

            // Cancel task during the running.
            cts = new CancellationTokenSource();
            // 10s
            longTask = new Task<int>(() => TaskMethod("Task 2", 10, cts.Token), cts.Token);
            longTask.Start();
         
            // 2.5s
            PrintTaskStatus(longTask);
            
            cts.Cancel();
            
            PrintTaskStatus(longTask);

            Console.WriteLine($"A task has been completed with result {longTask.Result}");
        }

        static void PrintTaskStatus(Task task)
        {
            for (int i = 0; i < 5; i++)
            {
                Thread.Sleep(TimeSpan.FromSeconds(0.5));
                Console.WriteLine(task.Status);
            }
        }

        static int TaskMethod(string name, int seconds, CancellationToken token)
        {
            for (int i = 0; i < seconds; i++)
            {
                Console.WriteLine($"Task running... {i}");
                Thread.Sleep(TimeSpan.FromSeconds(1));
                if (token.IsCancellationRequested)
                {
                    return -1;
                }
            }
            return 42 * seconds;
        }
        #endregion

        #region EAP
        static int TaskMethod(string name, int seconds)
        {
            Console.WriteLine(
                $"Task {name} is running on a thread id {Thread.CurrentThread.ManagedThreadId}. " +
                $"\nIs thread pool thread: {Thread.CurrentThread.IsThreadPoolThread}");
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            return 42 * seconds;
        }

        [TestMethod]
        public void TestEAPWithTask()
        {
            // This is a Task management.
            var tsc = new TaskCompletionSource<int>();

            var bw = new BackgroundWorker();
            bw.DoWork += (sender, args) =>
            {
                args.Result = TaskMethod("Background worker", 5);
            };

            bw.RunWorkerCompleted += (sender, args) =>
            {
                if (args.Error != null)
                {
                    tsc.SetException(args.Error);
                }
                else if (args.Cancelled)
                {
                    tsc.SetCanceled();
                }
                else
                {
                    tsc.SetResult((int)args.Result!);
                }
            };

            bw.RunWorkerAsync();

            int res = tsc.Task.Result;

            Console.WriteLine($"Result {res}");

            Thread.Sleep(TimeSpan.FromSeconds(7));
        }
        #endregion

        #region Exception
        static int TaskMethodThrowEx(string name, int seconds)
        {
            Console.WriteLine(
                $"Task {name} is running on a thread id {Thread.CurrentThread.ManagedThreadId}. " +
                $"\nIs thread pool thread: {Thread.CurrentThread.IsThreadPoolThread}");
            Thread.Sleep(TimeSpan.FromSeconds(seconds));
            throw new Exception("Boom!");
            return 42 * seconds;
        }

        [TestMethod]
        public void TestTaskException()
        {
            try
            {
                var task1 = Task.Run(() => TaskMethodThrowEx("Task 1", 2));
                int res = task1.Result;
                Console.WriteLine($"Result {res}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("===");

            try
            {
                var task2 = Task.Run(() => TaskMethodThrowEx("Task 2", 2));
                int res = task2.GetAwaiter().GetResult();
                Console.WriteLine($"Result {res}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }

            Console.WriteLine("===");

            var task3 = new Task<int>(() => TaskMethodThrowEx("Task 3", 3));
            var task4 = new Task<int>(() => TaskMethodThrowEx("Task 4", 2));
            var complexTask = Task.WhenAll(task3, task4);
            var exceptionHandler = complexTask.ContinueWith(t => Console.WriteLine(t.Exception));
            task3.Start(); task4.Start();

            Thread.Sleep(TimeSpan.FromSeconds(5));
        }
        #endregion
    }
}
