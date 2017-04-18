using System;
using System.Linq;
using System.Threading;
using System.Windows.Forms;

namespace Shike
{
    class ApplyItemHelper
    {
        public ApplyFormContent Content
        {
            get;
            set;
        }

        public bool? ApplyItem(Product item)
        {
            var ie = ApplyContext.Current.IE;
            //打开
            var success =
                WebBrowserHelper.Current.WaitDocumentCompleteAsync(() => ie.Navigate(item.DetailUrl)).Wait(10000);
            if (!success)
            {
                return false;
            }
            HtmlElement btn = null;

            var state = 1;
            var beginTime = DateTime.Now;
            while (true)
            {
                //"申请提交成功"
                var p = ie.GetElementByTagAndClass("div", "succeed-msg");
                if (p != null)
                {
                    return true;
                }

                //已参加过
                p = ie.GetElementByTagAndClass("p", "error-title");
                if (p != null)
                {
                    var msg = ie.Invoke((Func<string>)(() => p.InnerText)) as string;
                    if (msg != null && (msg.Contains("已参与过该活动")))
                    {
                        ApplyContext.Current.ShowMessage(item.Caption + "已经申请过。");
                        Content.Applied.Add(item.ID);
                        return false;
                    }
                }

                //不符合参与条件
                p = ie.GetElementByTagAndClass("h1", "error-title");
                if (p != null)
                {
                    var msg = ie.Invoke((Func<string>)(() => p.InnerText)) as string;
                    if (msg != null && ((msg.Contains("您不符合参与条件")) || (msg.Contains("您已申请过商家的其他商品"))))
                    {
                        ApplyContext.Current.ShowMessage(item.Caption + "不符合参与条件或已申请过商家的其他商品。");
                        Content.Applied.Add(item.ID);
                        return false;
                    }
                }

                //已参加过
                p = ie.GetElementByTagAndClass("a", "tryEndBtn");
                if (p != null)
                {
                    var msg = ie.Invoke((Func<string>)(() => p.InnerText)) as string;
                    if (msg != null && (msg.Contains("已申请")))
                    {
                        ApplyContext.Current.ShowMessage(item.Caption + "已经申请过。");
                        Content.Applied.Add(item.ID);
                        return false;
                    }
                }

                p = ie.GetElementByTagAndClass("h1", "error-title");
                if (p != null)
                {
                    var msg = ie.Invoke((Func<string>)(() => p.InnerText)) as string;
                    if (msg != null && msg.Contains("您今天已经申请太多了"))
                    {
                        MessageBox.Show("申请数量上限，停止。单击确认退出程序。");
                        Application.Exit();
                        return null;
                    }
                }

                switch (state)
                {
                    case 1: //首次进来
                        //有确定按钮，单击
                        ie.Invoke((Action)(() =>
                        {
                            btn = ie.Document.GetElementById("J_btn");
                        }));
                        if (btn != null)
                        {
                            ie.Invoke((Action)(() =>
                            {
                                //btn.OuterHtml = btn.OuterHtml.Replace("href=\"javascript:Detail.apply()\"",
                                //    "onclick=\"javascript:Detail.apply()\"");

                                btn.Document.InvokeScript("eval", new object[] { "Detail.apply()"});

                                btn.InvokeMember("click");
                            }));
                            //ApplyContext.Current.ShowMessage("单击“确定”");
                            Thread.Sleep(1000);
                            beginTime = DateTime.Now;
                            state = 2;
                            continue;
                        }
                        break;
                    case 2:
                        //有收藏按钮，单击
                        btn =
                            ie.Invoke(
                                (Func<object>)
                                    (() =>
                                        ie.Document.GetElementsByTagName("a").Cast<HtmlElement>().FirstOrDefault(
                                            e =>
                                                e.InnerText != null &&
                                                (e.InnerText.Contains("去收藏") || e.InnerText.Contains("去分享"))))) as
                                HtmlElement;
                        if (btn != null)
                        {
                            ie.Invoke((Action)(() => btn.InvokeMember("click")));
                            //ApplyContext.Current.ShowMessage("单击“收藏”");
                            Thread.Sleep(6000);
                            beginTime = DateTime.Now;
                            state = 3;
                            continue;
                        }
                        //state = 1;
                        break;
                    case 3:
                        //有收藏后的确认按钮，单击
                        btn =
                            ie.Invoke(
                                (Func<object>)
                                    (() =>
                                        ie.Document.GetElementsByTagName("a").Cast<HtmlElement>().FirstOrDefault(
                                            e => e.InnerText != null && e.InnerText.Contains("确认申请")))) as HtmlElement;
                        if (btn != null)
                        {
                            //ApplyContext.Current.ShowMessage("单击“确认申请”");
                            ie.Invoke((Action)(() => btn.InvokeMember("click")));
                            Thread.Sleep(1000);
                            beginTime = DateTime.Now;
                            continue;
                        }
                        //state = 2;
                        break;
                }

                if (DateTime.Now > beginTime.AddSeconds(10)) //10秒后超时
                {
                    return false;
                }
                Thread.Sleep(1000);
            }
        }
    }
}