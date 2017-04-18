using System;
using System.Windows.Forms;

namespace Shike
{
    public class ApplyContext
    {
        static ApplyContext current = new ApplyContext();

        ApplyContext() { }

        public static ApplyContext Current { get { return current; } }
        public WebBrowser IE { get; set; }

        public event EventHandler<string> Message;

        public void ShowMessage(string message)
        {
            if (Message == null)
            {
                return;
            }

            Message(this, message);
        }
    }
}