using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using DevExpress.XtraBars;
using DevExpress.XtraEditors.Controls;
using DevExpress.XtraLayout.Utils;
using Top.Api;

namespace Shike
{
    public partial class 试客联盟 : Form
    {
        const int WM_LBUTTONDBLCLK = 0x203;
        static string _agent;

        readonly ApplyItemHelper applyItemHelper = new ApplyItemHelper();
        readonly ApplyFormContent content;
        readonly ApplyContext context = ApplyContext.Current;
        readonly GetListHelper getListHelper = new GetListHelper();
        readonly Semaphore semaphore = new Semaphore(0, int.MaxValue);
        bool ignoreBlack;
        bool ignoreCode;
        bool loadImage;
        bool onlyBlack;
        volatile bool onlyWhite;

        public 试客联盟()
        {
            InitializeComponent();

            content = ApplyFormContent.Load();
            applyItemHelper.Content = content;

            tabWeb.Visibility = LayoutVisibility.Never;
            tabbedControlGroup1.SelectedTabPageIndex = 0;
            context.Message += ContextMessage;
            getListHelper.ProductFound += context_ProductFound;
            context.IE = ie;

            BindData();
            Task.Run((Action)DoApply);

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
            set
            {
                _agent = value;
            }
        }

        void Ie_ProgressChanged(object sender, WebBrowserProgressChangedEventArgs e)
        {
            setAgent();
        }

        void DoApply()
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
                    Invoke((Action)(() => content.ListToApply.Remove(item)));
                    ApplyContext.Current.ShowMessage(item.Caption + "申请成功。本次第" + ++ content.SuccessCount + "个");
                    content.Applied.Add(item.ID);
                    content.SaveXml();
                }
                else
                {
                    Invoke((Action)(() => content.ListToApply.Remove(item)));
                    //content.Failed.Add(item);
                    content.SaveXml();
                    ApplyContext.Current.ShowMessage(item + "申请失败");
                }
            }
        }

        void context_ProductFound(object sender, Product e)
        {
            if (InvokeRequired && Created)
            {
                //不需要一次加载过多
                while (content.ListToDecide.Count > 25 || content.ListToApply.Count > 25)
                {
                    Thread.Sleep(1000);
                }

                Invoke((Action<object, Product>)context_ProductFound, sender, e);
                return;
            }

            if (content.RefusedList.Contains(e.ID) || content.Applied.Contains(e.ID) ||
                content.ListToApply.Any(i => i.ID == e.ID) || content.ListToDecide.Any(i => i.ID == e.ID) ||
                (!ignoreBlack && content.BlackKeys.Any(s => !string.IsNullOrWhiteSpace(s) && e.Caption.Contains(s))))
            {
                return; //不处理，黑名单的或已经申请过的，或已经在列表的
            }

            ////排除扫码的
            var webRequest = (HttpWebRequest)WebRequest.Create(e.DetailUrl);

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

        void setAgent()
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

        void ContextMessage(object sender, string e)
        {
            if (InvokeRequired)
            {
                Invoke((Action<object, string>)ContextMessage, sender, e);
                return;
            }

            txtMessage.Text = e;
            txtAllMessage.AppendText(DateTime.Now + "：" + e + Environment.NewLine);
        }

        void btnApplyItem_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            var item = layoutView1.GetFocusedRow() as Product;
            if (item == null)
            {
                return;
            }

            Apply(item);
        }

        void ie_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            setAgent();
            //context.ShowMessage("网页加载完毕（" + ie.ReadyState + "）。" + e.Url);
        }

        void btnRefuseItem_ButtonClick(object sender, ButtonPressedEventArgs e)
        {
            var item = layoutView1.GetFocusedRow() as Product;
            if (item == null)
            {
                return;
            }

            content.Refuse(item);
        }

        void btnGetList_CheckedChanged(object sender, ItemClickEventArgs e)
            => getListHelper.Running = btnGetList.Checked;

        void btnClose_ItemClick(object sender, ItemClickEventArgs e)
        {
            getListHelper.Running = false;
            Close();
        }

        void ie_NewWindow(object sender, CancelEventArgs e) => e.Cancel = true;

        void BindData()
        {
            gridControl1.DataSource = content.ListToDecide;
            gridControl2.DataSource = content.ListToApply;
        }

        void btnCancelApply_Click(object sender, EventArgs e)
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

        void btnAddToBlackList_Click(object sender, EventArgs e)
        {
            var item = layoutView2.GetFocusedRow() as Product;
            if (item == null)
            {
                return;
            }

            content.Refuse(item);
        }

        void 试客联盟_FormClosing(object sender, FormClosingEventArgs e) => content.SaveXml();
        void btnAddToWhite_ButtonClick(object sender, ButtonPressedEventArgs e) => AddItemToKeys(content.WhiteKeys);

        void AddItemToKeys(List<string> list)
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

        void btnAddToBlack_ButtonClick(object sender, ButtonPressedEventArgs e) => AddItemToKeys(content.BlackKeys);

        void ApplyKeys()
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

        void Apply(Product item)
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

        void btnOnlyWhite_CheckedChanged(object sender, ItemClickEventArgs e)
        {
            btnOnlyBlack.Checked = false;
            onlyBlack = false;
            onlyWhite = btnOnlyWhite.Checked;
        }

        void barCheckItem1_CheckedChanged(object sender, ItemClickEventArgs e)
        {
            btnOnlyWhite.Checked = false;
            onlyWhite = false;
            onlyBlack = btnOnlyBlack.Checked;
        }

        void chkShowImage_CheckedChanged(object sender, ItemClickEventArgs e) => loadImage = chkShowImage.Checked;

        void barButtonItem1_ItemClick(object sender, ItemClickEventArgs e)
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

        void barCheckItem1_CheckedChanged_1(object sender, ItemClickEventArgs e) => ignoreCode = btnIgnoreCode.Checked;
        void btnIgnoreBlack_CheckedChanged(object sender, ItemClickEventArgs e) => ignoreBlack = btnIgnoreBlack.Checked;

        void barButtonItem2_ItemClick(object sender, ItemClickEventArgs e)
        {
            content.RefusedList.Clear();
        }

        void barButtonItem3_ItemClick(object sender, ItemClickEventArgs e)
        {
            AddItemToKeys(content.BlackKeys);
        }

        void 试客联盟_Load(object sender, EventArgs e)
        {
            ie.Navigate("http://list.shikee.com/list-1.html?type=1");
        }

        void barButtonItem4_ItemClick(object sender, ItemClickEventArgs e)
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
    }

    public class UserSellerGetResponse : TopResponse
    {
    }

    public class UserSellerGetRequest : ITopRequest<UserSellerGetResponse>
    {
        public string Fields
        {
            get;
            set;
        }

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