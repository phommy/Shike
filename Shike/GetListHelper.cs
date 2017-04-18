using System;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Shike.Properties;

namespace Shike
{
    class GetListHelper
    {
        bool _running;

        public bool Running
        {
            get
            {
                return _running;
            }
            set
            {
                if (value == _running)
                {
                    return;
                }

                if (value)
                {
                    _running = true;
                    Task.Run((Action)GetList);
                    ApplyContext.Current.ShowMessage("开始获取列表。");
                }
                else
                {
                    _running = false;
                    ApplyContext.Current.ShowMessage("停止获取列表。");
                }
            }
        }

        public event EventHandler<Product> ProductFound;

        void GetList()
        {
            if (ProductFound == null)
            {
                return;
            }

            if (!Running)
            {
                return;
            }

            var re = new Regex(@"<dd class=""item"">.*?<a href=""(.*?)"".*?data-url=""(.*?)"".*?alt=""(.*?)""",
                RegexOptions.Singleline);

            for (var i = 1; i <= 2; i++)
            {
                var page = 0;
                while (true)
                {
                    page++;
                    //Create request to URL.
                    try
                    {
                        ApplyContext.Current.ShowMessage("加载第" + page + "页");

                        var request =
                            (HttpWebRequest)WebRequest.Create("http://list.shikee.com/list-" + page + ".html?type=" + i);

                        //Set request headers.
                        request.KeepAlive = true;
                        request.Headers.Set(HttpRequestHeader.CacheControl, "max-age=0");
                        request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
                        request.UserAgent = 试客联盟.Agent;
                        request.Headers.Add("DNT", @"1");
                        request.Headers.Set(HttpRequestHeader.AcceptEncoding, "gzip,deflate,sdch");
                        request.Headers.Set(HttpRequestHeader.AcceptLanguage, "zh-CN,zh;q=0.8");

                        //Get response to request.
                        var response = (HttpWebResponse)request.GetResponse();

                        using (var responseStream = response.GetResponseStream())
                        {
                            var streamToRead = responseStream;
                            if (response.ContentEncoding.ToLower().Contains("gzip"))
                            {
                                streamToRead = new GZipStream(streamToRead, CompressionMode.Decompress);
                            }
                            else if (response.ContentEncoding.ToLower().Contains("deflate"))
                            {
                                streamToRead = new DeflateStream(streamToRead, CompressionMode.Decompress);
                            }

                            using (var streamReader = new StreamReader(streamToRead, Encoding.UTF8))
                            {
                                var html = streamReader.ReadToEnd();
                                var matches = re.Matches(html);
                                if (matches.Count == 0)
                                {
                                    break;
                                }
                                foreach (Match item in  matches)
                                {
                                    if (!Running)
                                    {
                                        return;
                                    }
                                    var p = new Product
                                            {
                                                DetailUrl = item.Groups[1].Value,
                                                ImgUrl = item.Groups[2].Value,
                                                Caption = item.Groups[3].Value
                                            };

                                    ProductFound(this, p);
                                    if (p.LoadImage)
                                    {
                                        p.Image =
                                            Image.FromStream(new MemoryStream(new WebClient().DownloadData(p.ImgUrl)));
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        ApplyContext.Current.ShowMessage("获取第" + page + "页数据列表出错：" + ex.Message);
                        return;
                    }
                }
            }
        }
    }
}