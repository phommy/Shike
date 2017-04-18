using System;
using Top.Api;

namespace TestTB
{
    class Program
    {
        static void Main(string[] args)
        {
            var url = "http://gw.api.taobao.com/router/rest";
            var secret = "f29662f6b97ce364d005477f235f47f6";
            var sessionKey = "";

            var req = new UserSellerGetRequest
                      {
                          Fields = "nick,sex"
                      };

            ITopClient client = new DefaultTopClient(url, req.GetTargetAppKey(), secret);
            var rsp = client.Execute(req, sessionKey);
            Console.WriteLine(rsp.Body);
        }
    }
}