namespace MultiThreadTest
{
    [TestClass]
    public class AwaitAsync
    {
        #region AwaitAsync
        async Task<string> GetStringAsync()
        {
            await Task.Delay(1000);
            return "Hello, World!";
        }

        /// <summary>
        /// Just a simple demo, never implement async/await this way.
        /// 
        /// DO NOT use sync method in the async funciton, that may cause deadlock.
        /// </summary>
        [TestMethod]
        public void TestAsync()
        {
            /*
             * Task.GetAwaiter().GetResult() is preferred over Task.Wait and Task.
             * Result because it propagates exceptions rather than wrapping them 
             * in an AggregateException. However, all three methods cause the potential 
             * for deadlock and thread pool starvation issues. They should all be 
             * avoided in favor of async/await.
             * 
             * https://stackoverflow.com/questions/17284517/is-task-result-the-same-as-getawaiter-getresult
             */
            var res = GetStringAsync().GetAwaiter().GetResult();
            Assert.AreEqual(res, "Hello, World!");
        }
        #endregion

        #region Continue with async
        [TestMethod]
        public async Task TestAsyncContinueWith()
        {
            await AsynchronyWithAwait();
        }
        async static Task AsynchronyWithAwait()
        {
            try
            {
                string result = await GetInfoAsync("Async 1");
                Console.WriteLine(result);
                string result2 = await GetInfoAsyncError("Async 2");
                Console.WriteLine(result2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        async static Task<string> GetInfoAsyncError(string name)
        {
            Console.WriteLine($"Task {name} started!");
            await Task.Delay(2000);
            throw new NotImplementedException();
        }

        async static Task<string> GetInfoAsync(string name)
        {
            Console.WriteLine($"Task {name} started!");
            await Task.Delay(2000);
            return $"Task is running on thread pool thread: {Thread.CurrentThread.IsThreadPoolThread}";
        }
        #endregion

        #region parallel task with await
        [TestMethod]
        public async Task TestParallelTaskAwait()
        {
            /*
             * These 2 taks could print the same thread id, why could this happen?
             * That's because the async method is splitted to multi parts, each part
             * runs on a thread seperately, and the two parts after the Task.Delay could
             * have the opportunity to run on the same thread pool thread.
             */
            var t1 = GetInfoAsync("Task 1", 3);
            var t2 = GetInfoAsync("Task 2", 5);

            string[] results = await Task.WhenAll(t1, t2);

            foreach (var res in results)
            {
                Console.WriteLine(res);
            }
        }

        async static Task<string> GetInfoAsync(string name, int seconds)
        {
            Console.WriteLine($"Task {name} started!");
            await Task.Delay(TimeSpan.FromSeconds(seconds));
            return $"Task is running on thread pool thread: {Thread.CurrentThread.IsThreadPoolThread}\n" +
                $"Thread id: {Thread.CurrentThread.ManagedThreadId}";
        }
        #endregion

        #region exceptions in parallel tasks
        [TestMethod]
        public async Task TestSingleException()
        {
            try
            {
                var a = await GetInfoAsyncError("Task error");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// This can only get the first exception in the WhenAll exceptions.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestMultiExceptionNoFlatten()
        {
            try
            {
                var t1 = GetInfoAsyncError("Task error1");
                var t2 = GetInfoAsyncError("Task error2");
                // waits for 2 tasks to finish.
                await Task.WhenAll(t1, t2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// Flatten the aggregation exceptions!
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestMultiExceptionFlatten()
        {
            var t1 = GetInfoAsyncError("Task error1");
            var t2 = GetInfoAsyncError("Task error2");
            var tasks = Task.WhenAll(t1, t2);
            try
            {
                await tasks;
            }
            catch
            {
                if (tasks.Exception == null)
                {
                    return;
                }
                var aggreExs = tasks.Exception.Flatten();
                var exceptions = aggreExs.InnerExceptions;

                Console.WriteLine($"Exceptions caught: {exceptions.Count}");

                foreach (var e in exceptions)
                {
                    Console.WriteLine($"Exception details: {e}");
                    Console.WriteLine();
                }
            }
        }
        #endregion

        #region async void
        /// <summary>
        /// Suggest return Task not void.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task TestAsyncVoid()
        {
            await AsyncTask();
            await AsyncVoid();

            try
            {
                await AsyncTaskWithErrors();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        async static Task AsyncTask()
        {
            string result = await GetInfoAsync("Async Task");
            Console.WriteLine(result);
        }

        async static Task AsyncVoid()
        {
            string result = await GetInfoAsync("Async Void");
            Console.WriteLine(result);
        }

        async static Task AsyncTaskWithErrors()
        {
            string result = await GetInfoAsyncError("Async Task With Errors");
            Console.WriteLine(result);
        }

        async static void AsyncVoidWithErrors()
        {
            string result = await GetInfoAsyncError("Async Void With Errors");
            Console.WriteLine(result);
        }
        #endregion
    }
}
