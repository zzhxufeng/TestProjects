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


    }
}
