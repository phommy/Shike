using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraEditors.Controls;
using Top.Api;

namespace Shike
{
    public partial class 试客联盟 : Form
    {
        private const int WM_LBUTTONDBLCLK = 0x203;
        private static string _agent;

        private readonly ApplyItemHelper applyItemHelper = new ApplyItemHelper();
        private readonly ApplyFormContent content;
        private readonly ApplyContext context = ApplyContext.Current;
        private readonly GetListHelper getListHelper = new GetListHelper();
        private readonly Semaphore semaphore = new Semaphore(0, int.MaxValue);

        private CancellationTokenSource cts;
        private bool ignoreBlack;
        private bool ignoreCode;
        private bool loadImage;
        private bool onlyBlack;
        private volatile bool onlyWhite;

        public 试客联盟()
        {
            InitializeComponent();

            content = ApplyFormContent.Load();
            applyItemHelper.Content = content;

            tabbedControlGroup1.SelectedTabPageIndex = 0;
            context.Message += ContextMessage;
            getListHelper.ProductFound += context_ProductFound;
            context.IE = ie;

            BindData();
            Task.Run((Action) DoApply);

            onlyWhite = btnOnlyWhite.Checked;
            onlyBlack = btnOnlyBlack.Checked;
            loadImage = chkShowImage.Checked;
            ignoreCode = btnIgnoreCode.Checked;
            ignoreBlack = btnIgnoreBlack.Checked;

            ie.ProgressChanged += Ie_ProgressChanged;
        }

        public static string Agent
        {
            get
            {
                while (_agent == null)
                {
                    Application.DoEvents();
                }

                return _agent;
            }
            set { _agent = value; }
        }

        private void Ie_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            setAgent();
        }

        private void DoApply()
        {
            while (semaphore.WaitOne())
            {
                var item = content.ListToApply[0];

                var applyItem = applyItemHelper.ApplyItem(item);
                if (!applyItem.HasValue)
                {
                    return;
                }

                if (applyItem.Value)
                {
                    Invoke((Action) (() => content.ListToApply.Remove(item)));
                    ApplyContext.Current.ShowMessage(item.Caption + "申请成功。本次第" + ++content.SuccessCount + "个");
                    content.Applied.Add(item.ID);
                    content.SaveXml();
                }
                else
                {
                    Invoke((Action) (() => content.ListToApply.Remove(item)));
                    //content.Failed.Add(item);
                    content.SaveXml();
                    ApplyContext.Current.ShowMessage(item + "申请失败");
                }
            }
        }

        private void context_ProductFound(object sender, Product e)
        {
            if (InvokeRequired && Created)
            {
                //不需要一次加载过多
                while (content.ListToDecide.Count > 25 || content.ListToApply.Count > 25)
                {
                    Thread.Sleep(1000);
                }

                Invoke((Action<object, Product>) context_ProductFound, sender, e);
                return;
            }

            if (content.RefusedList.Contains(e.ID) || content.Applied.Contains(e.ID) ||
                content.ListToApply.Any(i => i.ID == e.ID) || content.ListToDecide.Any(i => i.ID == e.ID) ||
                !ignoreBlack && content.BlackKeys.Any(s => !string.IsNullOrWhiteSpace(s) && e.Caption.Contains(s)))
            {
                return; //不处理，黑名单的或已经申请过的，或已经在列表的
            }

            ////排除扫码的
            var webRequest = (HttpWebRequest) WebRequest.Create(e.DetailUrl);

            setAgent();

            webRequest.UserAgent = Agent;
            var str = new StreamReader(webRequest.GetResponse().GetResponseStream(), Encoding.UTF8).ReadToEnd();
            if (str.Contains("扫码下载手机客户端"))
            {
                //&&str.Contains("下次开抢时间")
                ContextMessage(null, e.Caption + "跳过（扫码申请）");
                content.RefusedList.Add(e.ID);
                return;
            }

            if (str.Contains("寻找答案"))
            {
                ContextMessage(null, e.Caption + "跳过（需答案）");
                content.RefusedList.Add(e.ID);
                return;
            }

            if (str.Contains("该活动为二维码下单活动"))
            {
                if (ignoreCode)
                {
                    ContextMessage(null, e.Caption + "跳过（扫码下单）");
                    //content.RefusedList.Add(e.ID);
                    return;
                }

                e.Caption = "【扫码】" + e.Caption;
            }

            if (onlyBlack || content.WhiteKeys.Any(s => !string.IsNullOrWhiteSpace(s) && e.Caption.Contains(s)))
            {
                Apply(e);
            }
            else if (onlyWhite)
            {
                return;
            }
            else
            {
                content.ListToDecide.Add(e);
            }
            e.LoadImage = loadImage;
        }

        private void setAgent()
        {
            if (ie.Document?.Window == null)
            {
                return;
            }
            var window = ie.Document.Window.DomWindow;
            var wt = window.GetType();
            var navigator = wt.InvokeMember("navigator",
                BindingFlags.GetProperty,
                null,
                window,
                new object[]
                {
                });
            var nt = navigator.GetType();
            var userAgent = nt.InvokeMember("userAgent",
                BindingFlags.GetProperty,
                null,
                navigator,
                new object[]
                {
                });
            Agent = userAgent.ToString();
        }

        private void ContextMessage(object sender, string e)
        {
            if (InvokeRequired)
            {
                Invoke((Action<object, string>) ContextMessage, sender, e);
                return;
            }

            txtMessage.Text = e;
            txtAllMessage.AppendText(DateTime.Now + "：" + e + Environment.NewLine);
        }

        private void btnApplyItem_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            var item = layoutView1.GetFocusedRow() as Product;
            if (item == null)
            {
                return;
            }

            Apply(item);
        }

        private void ie_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            setAgent();
            //context.ShowMessage("网页加载完毕（" + ie.ReadyState + "）。" + e.Url);
        }

        private void btnRefuseItem_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            var item = layoutView1.GetFocusedRow() as Product;
            if (item == null)
            {
                return;
            }

            content.Refuse(item);
        }

        private void btnGetList_CheckedChanged(object sender, ItemClickEventArgs e)
            => getListHelper.Running = btnGetList.Checked;

        private void btnClose_ItemClick(object sender, ItemClickEventArgs e)
        {
            getListHelper.Running = false;
            Close();
        }

        private void ie_NewWindow(object sender, CancelEventArgs e) => e.Cancel = true;

        private void BindData()
        {
            gridControl1.DataSource = content.ListToDecide;
            gridControl2.DataSource = content.ListToApply;
        }

        private void btnCancelApply_Click(object sender, EventArgs e)
        {
            var item = layoutView2.GetFocusedRow() as Product;
            if (item == null)
            {
                return;
            }
            if (item == content.ListToApply[0])
            {
                context.ShowMessage("当前正在申请的不能取消");
                return;
            }

            content.ListToApply.Remove(item);
        }

        private void btnAddToBlackList_Click(object sender, EventArgs e)
        {
            var item = layoutView2.GetFocusedRow() as Product;
            if (item == null)
            {
                return;
            }

            content.Refuse(item);
        }

        private void 试客联盟_FormClosing(object sender, FormClosingEventArgs e) => content.SaveXml();

        private void btnAddToWhite_ButtonClick(object sender, ButtonPressedEventArgs e) => AddItemToKeys(content
            .WhiteKeys);

        private void AddItemToKeys(List<string> list)
        {
            var curItem = layoutView1.GetFocusedRow() as Product;

            var f = new MultiLineEditForm();
            f.DocBox.Lines = list.ToArray();

            if (curItem != null)
            {
                f.DocBox.AppendText("\r\n" + curItem.Caption);
                ;
            }
            if (f.ShowDialog() == DialogResult.Cancel)
            {
                return;
            }

            list.Clear();
            list.AddRange(f.DocBox.Lines.Where(item => !string.IsNullOrWhiteSpace(item)));
            ApplyKeys();
        }

        private void btnAddToBlack_ButtonClick(object sender, ButtonPressedEventArgs e) => AddItemToKeys(content
            .BlackKeys);

        private void ApplyKeys()
        {
            foreach (var item in content.ListToDecide.ToArray())
            {
                if (content.WhiteKeys.Any(s => !string.IsNullOrWhiteSpace(s) && item.Caption.Contains(s)))
                {
                    Apply(item);
                }
                else if (!ignoreBlack &&
                         content.BlackKeys.Any(s => !string.IsNullOrWhiteSpace(s) && item.Caption.Contains(s)))
                {
                    content.Refuse(item);
                }
            }
        }

        private void Apply(Product item)
        {
            if (!LoginHelper.Current.Log())
            {
                return;
            }

            content.ListToApply.Add(item);
            semaphore.Release();
            content.ListToDecide.Remove(item);
            //content.Failed.RemoveAll(i => i.ID == item.ID);
        }

        private void btnOnlyWhite_CheckedChanged(object sender, ItemClickEventArgs e)
        {
            btnOnlyBlack.Checked = false;
            onlyBlack = false;
            onlyWhite = btnOnlyWhite.Checked;
        }

        private void barCheckItem1_CheckedChanged(object sender, ItemClickEventArgs e)
        {
            btnOnlyWhite.Checked = false;
            onlyWhite = false;
            onlyBlack = btnOnlyBlack.Checked;
        }

        private void chkShowImage_CheckedChanged(object sender, ItemClickEventArgs e) => loadImage =
            chkShowImage.Checked;

        private void barButtonItem1_ItemClick(object sender, ItemClickEventArgs e)
        {
            EventHandler handler = (o, e1) => MessageBox.Show("鼠标双击");

            Action<Control.ControlCollection> addListener = null;
            addListener = cs =>
            {
                foreach (Control item in cs)
                {
                    item.DoubleClick += handler;
                    addListener(item.Controls);
                }
            };

            addListener(Controls);
        }

        private void barCheckItem1_CheckedChanged_1(object sender, ItemClickEventArgs e) => ignoreCode =
            btnIgnoreCode.Checked;

        private void btnIgnoreBlack_CheckedChanged(object sender, ItemClickEventArgs e) => ignoreBlack =
            btnIgnoreBlack.Checked;

        private void barButtonItem2_ItemClick(object sender, ItemClickEventArgs e)
        {
            content.RefusedList.Clear();
        }

        private void barButtonItem3_ItemClick(object sender, ItemClickEventArgs e)
        {
            AddItemToKeys(content.BlackKeys);
        }

        private void 试客联盟_Load(object sender, EventArgs e)
        {
            ie.Navigate("http://list.shikee.com/list-1.html?type=1");
        }

        private void barButtonItem4_ItemClick(object sender, ItemClickEventArgs e)
        {
            var url = "http://gw.api.taobao.com/router/rest";
            var appkey = "23560991";
            var secret = "f29662f6b97ce364d005477f235f47f6";
            var sessionKey = "";

            ITopClient client = new DefaultTopClient(url, appkey, secret);
            var req = new UserSellerGetRequest();
            req.Fields = "nick,sex";
            var rsp = client.Execute(req, sessionKey);
            Console.WriteLine(rsp.Body);
        }

        private void button1_Click(object sender, EventArgs e) //加载列表
        {
            btnGetList.Checked = false;
            getListHelper.Running = false;

            if (!LoginHelper.Current.Log())
            {
                return;
            }

            ie.Navigate("http://user.shikee.com/buyer/join/pass_list/?state[]=1");
            SpinAwait(() => ie.ReadyState == WebBrowserReadyState.Complete, 5000);
        }

        private  async Task<bool> SpinAwait(Func<bool> v, int i)
        {
            Func<bool> b = () =>
            {
                if (InvokeRequired)
                {
                    return (bool) Invoke(v);
                }

                return v();
            };

            return await Task.Run(() =>
            {
                return SpinWait.SpinUntil(b, i);
            });
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            GetBuyLink(txtLink.Text);
        }

        async private void GetBuyLink(string text)
        {
            //如果没领过积分，先领取
            if (!File.Exists("mmjf.txt"))
            {
                File.WriteAllText("mmjf.txt", DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd"));
            }

            var date = DateTime.Parse(File.ReadAllText("mmjf.txt"));
            if (date.Date < DateTime.Now.Date)
            {
                ContextMessage(null, "首次打开，领积分");
                if (!await NavigateWait("http://www.52mmdp.com/index1.php?"))
                {
                    return;
                }

                ie.Document?.GetElementById("qiandao")?.InvokeMember("click");
                var t = Task.Delay(3000);
                File.WriteAllText("mmjf.txt", DateTime.Now.ToString("yyyy-MM-dd"));
                await t;
            }

            //分享
            if (!await NavigateWait("http://www.52mmdp.com/index1.php?m=picker&url=" + text))
            {
                return;
            }

            if (ie.DocumentText.Contains("不支持"))
            {
                ContextMessage(null, "不支持网站");
            }

            var btnForward = ie.Document.GetElementById("forwardMaga");
            if (btnForward==null)
            {
                ContextMessage(null, "无下一步按钮，中止");
                return;
            }

            btnForward.InvokeMember("click");
        }

        private async Task<bool> NavigateWait(string url)
        {
            cts?.Cancel();
            cts = new CancellationTokenSource();
            var ct = cts.Token;
            ie.Navigate(url);

            await SpinAwait(
                () => ct.IsCancellationRequested ||
                      (bool) Invoke((Func<bool>) (() => ie.ReadyState == WebBrowserReadyState.Complete)), 5000);

            return ie.ReadyState == WebBrowserReadyState.Complete;
        }

        [DllImport("User32.dll", EntryPoint = "FindWindow")]
        public static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        /// <param name="m">要处理的 Windows<see cref="T:System.Windows.Forms.Message" />。</param>
        protected override void DefWndProc(ref Message m)
        {
            if (m.Msg == 0x004A)
            {
                tv.SuspendLayout();
                var jsonStr = ((COPYDATASTRUCT)m.GetLParam(typeof(COPYDATASTRUCT))).lpData;

                getTV(jsonStr);
                return;
            }

            base.DefWndProc(ref m);
        }

        private void getTV(string jsonStr)
        {
            var json = new JavaScriptSerializer();
            dynamic obj;
            tv.Nodes.Clear();
            try
            {
                obj = json.Deserialize<object>(jsonStr);
            }
            catch (Exception)
            {
                return;
            }

            addToTV(obj["data"]["list"]);
            tv.ExpandAll();
            tv.ResumeLayout();
            return;
        }

        private void addToTV(dynamic o)
        {
            foreach (var item in o)
            {
                var n = tv.Nodes.Add((string) item["title"]);
                n.Nodes.Add("价格：" + item["price"]);
                n.Nodes.Add("下单地址：" + item["order_url"]);
            }
        }

        private async void tv_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Text.Contains("下单地址："))
            {
                var url = e.Node.Text.Replace("下单地址：", "");
                Clipboard.SetText(url);

                GetBuyLink(url);
            }
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            getTV(txtJson.Text);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct COPYDATASTRUCT
    {
        public IntPtr dwData;
        public int cbData;

        [MarshalAs(UnmanagedType.LPStr)] public string lpData;
    }

    public class UserSellerGetResponse : TopResponse
    {
    }

    public class UserSellerGetRequest : ITopRequest<UserSellerGetResponse>
    {
        public string Fields { get; set; }

        public string GetApiName()
        {
            return "taobao.tbk.item.info.get";
        }

        public string GetTargetAppKey()
        {
            return "23560991";
        }

        public IDictionary<string, string> GetParameters()
        {
            return new Dictionary<string, string>();
        }

        public IDictionary<string, string> GetHeaderParameters()
        {
            return new Dictionary<string, string>();
        }

        public string GetBatchApiSession()
        {
            return null;
        }

        public void SetBatchApiSession(string session)
        {
        }

        public int GetBatchApiOrder()
        {
            return 0;
        }

        public void SetBatchApiOrder(int order)
        {
        }

        public void Validate()
        {
        }
    }
}