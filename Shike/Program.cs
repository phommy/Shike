using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace Shike
{
    static class Program
    {
        /// <summary>
        /// 应用程序的主入口点。
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += Application_ThreadException;
            Application.Run(new 试客联盟());

        }

        public static List<string> GetMonths()
        {
            var list = new List<string>();
            foreach (var i in Enumerable.Range(1, 12))
            {
                list.Add(i.ToString("00"));
            }
            return list;
        }

        static void Application_ThreadException(object sender, ThreadExceptionEventArgs e)
        {
            MessageBox.Show("全局错误：" + e.Exception.ToString());
        }

        public static async Task<bool> SleepAsync(int millisecondsTimeout)
        {
            var tcs = new TaskCompletionSource<bool>();
            var t = new Timer(delegate
            {
                MessageBox.Show(Thread.CurrentThread.ManagedThreadId + "tap to start");

                try
                {
                    MessageBox.Show(tcs.Task.Result.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                    throw;
                }

                tcs.TrySetResult(true);
            },
                null,
                -1,
                -1);
            t.Change(millisecondsTimeout, -1);

            return await tcs.Task;
        }
    }
}