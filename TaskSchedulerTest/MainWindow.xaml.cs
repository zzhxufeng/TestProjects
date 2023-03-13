using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace TaskSchedulerTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ButtonSync_Click(object sender, RoutedEventArgs e)
        {
            ContentTextBlock.Text = string.Empty;
            try
            {
                /*This is a deadlock, because the UI thread waits for the Result, 
                 * however, the Result needs the UI thread to run the task (but at the time, the UI thread is waitting for result).*/
                //string result = TaskMethod(TaskScheduler.FromCurrentSynchronizationContext()).Result;

                /*
                 * This is a sync method, the UI will not respond before the task finished.*/
                string result = TaskMethod().Result;
                ContentTextBlock.Text = result;
            }
            catch (System.Exception ex)
            {
                ContentTextBlock.Text = ex.Message;
            }
        }

        private void ButtonAsync_Click(object sender, RoutedEventArgs e)
        {
            ContentTextBlock.Text = string.Empty;
            Mouse.OverrideCursor = Cursors.Wait;
            
            /*
             * This is not call on UI context. The exception will be throwed.*/
            Task<string> task = TaskMethod();

            _ = task.ContinueWith(task =>
                {
                    ContentTextBlock.Text = task.Exception.InnerException.Message;
                    Mouse.OverrideCursor = null;
                }, 
                CancellationToken.None, 
                TaskContinuationOptions.OnlyOnFaulted,
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void ButtonAsyncOK_Click(object sender, RoutedEventArgs e)
        {
            ContentTextBlock.Text = string.Empty;
            Mouse.OverrideCursor = Cursors.Wait;

            Task<string> task = TaskMethod(
                TaskScheduler.FromCurrentSynchronizationContext());

            task.ContinueWith(t => Mouse.OverrideCursor = null,
                CancellationToken.None,
                TaskContinuationOptions.None,
                TaskScheduler.FromCurrentSynchronizationContext());
        }

        Task<string> TaskMethod()
        {
            return TaskMethod(TaskScheduler.Default);
        }

        Task<string> TaskMethod(TaskScheduler scheduler)
        {
            /*
             * This Task.Delay starts right away.
             * So, we don't need to call Start() on the return task of this method.*/
            Task delay = Task.Delay(TimeSpan.FromSeconds(5));

            return delay.ContinueWith(task =>
            {
                string str = $"Task is running on thread: {Thread.CurrentThread.ManagedThreadId}" +
                $"\n Is thread pool thread: {Thread.CurrentThread.IsThreadPoolThread}";

                /*
                 * Because this ContentTextBlock element belongs to the UI thread.
                 * The default TaskScheduler will not work. */
                ContentTextBlock.Text = str;

                return str;
            }, scheduler);
        }
    }
}
