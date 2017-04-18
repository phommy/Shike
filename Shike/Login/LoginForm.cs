using System;
using System.Linq;
using System.Windows.Forms;

namespace Shike.Login
{
    public partial class LoginForm : Form
    {
        public LoginForm()
        {
            InitializeComponent();
            if (LocalConfig.Current.AutoLogin)
            {
                txtUser.Text = LocalConfig.Current.LoginEmail;
                txtPwd.Text = LocalConfig.Current.LoginPassword;
            }
        }

        async void btnOK_Click(object sender, EventArgs e)
        {
            //登陆
            btnOK.Enabled = false;
            var loginFail = false;
            btnOK.Text = "登陆中";
            try
            {
                var ie = ApplyContext.Current.IE;

                var loginUri = new Uri("http://login.shikee.com/");
                if (ie.ReadyState != WebBrowserReadyState.Complete || ie.Url != loginUri)
                {
                    await WebBrowserHelper.Current.WaitDocumentCompleteAsync(() => ie.Navigate(loginUri));
                    if (ie.Url == new Uri("http://user.shikee.com/buyer")) //already logged
                    {
                        DialogResult = DialogResult.OK;
                        return;
                    }

                    if (!Created)
                    {
                        return;
                    }
                }
                ie.Document.GetElementById("username").SetAttribute("value", txtUser.Text);
                ie.Document.GetElementById("password").SetAttribute("value", txtPwd.Text);
                var inputMessage =
                    ie.Document.GetElementsByTagName("span").Cast<HtmlElement>().FirstOrDefault(
                        he => he.GetAttribute("className") == "login-msg-error");
                if (inputMessage != null)
                {
                    inputMessage.InnerText = "";
                }

                var waitDocumentCompleteAsync =
                    WebBrowserHelper.Current.WaitDocumentCompleteAsync(
                        () => ie.Document.GetElementById("J_submit").InvokeMember("click"),
                        () =>
                        {
                            if (inputMessage != null && !inputMessage.InnerText.IsEmpty())
                            {
                                loginFail = true;
                                return true;
                            }
                            return false;
                        });
                await waitDocumentCompleteAsync;

                if (!Created)
                {
                    return;
                }

                if (loginFail)
                {
                    txtMsg.Text = inputMessage.InnerText;
                    return;
                }

                var config = LocalConfig.Current;
                config.AutoLogin = chkRememberMe.Checked;
                config.LoginEmail = txtUser.Text;
                config.LoginPassword = txtPwd.Text;
                config.Save();
                DialogResult = DialogResult.OK;
            }
            finally
            {
                btnOK.Text = "登陆";
                btnOK.Enabled = true;
            }
        }
    }

    interface IMarkupContainer2
    {
        void CreateChangeLog(LoginForm loginForm, out object changeLog, int i, int i1);
        void RegisterForDirtyRange(LoginForm loginForm, out object mCookie);
    }
}