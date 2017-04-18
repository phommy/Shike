using System.Collections.Generic;
using Top.Api;

namespace TestTB
{
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