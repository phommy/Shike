using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace AsyncWaiter
{
    public class ListenController
    {
        readonly ListenForm form;
        readonly BindingList<CallingMethod> methods;

        public ListenController()
        {
            methods = new BindingList<CallingMethod>();
            form = new ListenForm(methods);
        }

        public void ShowListenForm() { form.Show(); }

        int s() { return 0; }



        //该方法会被以同步方式调用。被调用时，在ListenForm的列表中创建一行记录，当该记录对应的行被用户点“返回”按钮时，本方法方返回。
        public async Task<string> CallMethod(string id)
        {
   

            var method = new CallingMethod {ID = id};
            form.Invoke((Action)(() => methods.Add(method)));
            SpinWait.SpinUntil(() => method.CanReturn);
            return id;
        }

        void F(object o) { throw new NotImplementedException(); }
    }

  
}