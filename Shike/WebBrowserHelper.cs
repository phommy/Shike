using System;
using System.Threading.Tasks;
using System.Windows.Forms;
using Timer = System.Threading.Timer;

namespace Shike
{
    class WebBrowserHelper
    {
        static WebBrowserHelper _current = new WebBrowserHelper();
        public static WebBrowserHelper Current { get { return _current; } }

        public Task<WebBrowser> WaitDocumentCompleteAsync(Action action, Func<bool> QuitCondition = null)
        {
            return runInternal(action, QuitCondition);
        }

        static Task<WebBrowser> runInternal(Action action, Func<bool> QuitCondition)
        {
            try
            {
                var t = new TaskCompletionSource<WebBrowser>();
                var ie = ApplyContext.Current.IE;

                //如果有退出条件，检查
                if (QuitCondition != null)
                {
                    var timer = new Timer(state =>
                    {
                        bool result;

                        if (ie.InvokeRequired && ie.Created)
                        {
                            result = (bool)ie.Invoke(QuitCondition);
                        }
                        else
                        {
                            result = QuitCondition();
                        }

                        if (result)
                        {
                            t.TrySetResult(ie);
                            (state as Timer).Dispose();
                        }
                    });
                    timer.Change(1000, 1000);
                }

                WebBrowserDocumentCompletedEventHandler callBack = null;
                callBack = (o, e) =>
                {
                    if (e.Url == ie.Url && ie.ReadyState == WebBrowserReadyState.Complete)
                    {
                        t.TrySetResult(ie);
                        ie.DocumentCompleted -= callBack;
                    }
                };
                ie.DocumentCompleted += callBack;

                if (action != null)
                {
                    action();
                }
                return t.Task;
            }
            catch (Exception ex)
            {
                ApplyContext.Current.ShowMessage(ex.Message);
                throw;
            }
        }
    }
}