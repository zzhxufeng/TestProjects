using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.TextFormatting;

namespace TaskContextTest
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

        /// <summary>
        /// With the context: 00:00:04.4246247
        /// Without the context: 00:00:00.6350292
        /// Ratio: 6.97
        /// 
        /// The task that captures the current context will need more time to execute,
        /// because it runs on the UI thread(current context for desktop application).
        /// 
        /// In this case, we don't need to run the task on UI thread, so the
        ///     await t.ConfigureAwait(continueOnCapturedContext: false);
        /// is more reasonable.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            TextBlock.Text = "Calculating...";

            var resultWithContext = await TestWithContext();
            var resultNoContext = await TestNoContext();
            /*
             * The commented code below will cause the 
             *      TextBlock.Text = sb.ToString();
             * cannot be set, because the rest of the method will run on a thread pool
             * thread, which should be UI thread.
             */
            //var resultNoContext = await TestNoContext().ConfigureAwait(false);

            var sb = new StringBuilder();
            sb.AppendLine(string.Format("With the context: {0}", resultWithContext));
            sb.AppendLine(string.Format("Without the context: {0}", resultNoContext));
            sb.AppendLine(string.Format("Ratio: {0:0.00}", resultWithContext.TotalMilliseconds / resultNoContext.TotalMilliseconds));

            string thInfo = $"Is thread pool thread: {Thread.CurrentThread.IsThreadPoolThread}";
            MessageBox.Show(thInfo);

            TextBlock.Text = sb.ToString();
        }

        /// <summary>
        /// In the default situation, the await will capture the current context.
        /// Can use  
        ///     await t.ConfigureAwait(continueOnCapturedContext: false);
        /// to configure it.
        /// </summary>
        /// <returns></returns>
        async static Task<TimeSpan> TestWithContext()
        {
            const int iterationsNumber = 100000;
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterationsNumber; i++)
            {
                var t = Task.Run(() => { });
                await t;
            }
            sw.Stop();
            return sw.Elapsed;
        }

        /// <summary>
        /// continueOnCapturedContext: false
        /// </summary>
        /// <returns></returns>
        async static Task<TimeSpan> TestNoContext()
        {
            const int iterationsNumber = 100000;
            var sw = new Stopwatch();
            sw.Start();
            for (int i = 0; i < iterationsNumber; i++)
            {
                var t = Task.Run(() => { });
                /*There is the difference.*/
                await t.ConfigureAwait(continueOnCapturedContext: false);
            }
            sw.Stop(); 
            return sw.Elapsed;
        }
    }
}
