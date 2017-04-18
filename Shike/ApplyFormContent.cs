using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;

namespace Shike
{
    [DataContract]
    public class ApplyFormContent
    {
        const string fileName = "content.json";
        List<string> _blackKeys;
        List<string> _whiteKeys;
        List<string> applied;
        List<Product> failed;
        BindingList<Product> listToApply;
        BindingList<Product> listToDecide;
        List<string> refused;

        ApplyFormContent() { }

        public BindingList<Product> ListToApply
        {
            get { return listToApply ?? (listToApply = new BindingList<Product>()); }
        }

        public BindingList<Product> ListToDecide
        {
            get { return listToDecide ?? (listToDecide = new BindingList<Product>()); }
        }

        [DataMember]
        public List<string> WhiteKeys { get { return _whiteKeys ?? (_whiteKeys = new List<string>()); } }

        //[DataMember]
        public List<Product> Failed { get { return failed ?? (failed = new List<Product>()); } }

        [DataMember]
        public List<string> BlackKeys { get { return _blackKeys ?? (_blackKeys = new List<string>()); } }

        [DataMember]
        public List<string> RefusedList { get { return refused ?? (refused = new List<string>()); } }

        [DataMember]
        public List<string> Applied { get { return applied ?? (applied = new List<string>()); } }

        public int SuccessCount
        {
            get;
            set;
        }

        public void Refuse(Product item)
        {
            ListToDecide.Remove(item);
            ListToApply.Remove(item);
            //Failed.Remove(item);
            RefusedList.Add(item.ID);
        }

        public static ApplyFormContent Load()
        {
            var result = File.Exists(fileName)
                ? JsonHelper.ParseFromJson<ApplyFormContent>(File.ReadAllText(fileName, System.Text.Encoding.UTF8))
                : new ApplyFormContent();

            return result;
        }

        public void SaveXml() { File.WriteAllText(fileName, JsonHelper.GetJson(this)); }
    }
}