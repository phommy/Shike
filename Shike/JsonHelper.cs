using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Web.Script.Serialization;

namespace Shike
{
    public static class JsonHelper
    {
        public static string GetJson<T>(T obj)
        {
            var json = new DataContractJsonSerializer(obj.GetType());
            using (var stream = new MemoryStream())
            {
                json.WriteObject(stream, obj);
                return Encoding.UTF8.GetString(stream.ToArray());
            }
        }

        public static Dictionary<string, object> GetDictionaryFromJson(string JSON)
        {
            return (Dictionary<string, object>)new JavaScriptSerializer().DeserializeObject(JSON);
        }

        public static T ParseFromJson<T>(string JSON)
        {
            using (var ms = new MemoryStream(Encoding.UTF8.GetBytes(JSON)))
            {
                return (T)new DataContractJsonSerializer(typeof(T)).ReadObject(ms);
            }
        }
    }
}