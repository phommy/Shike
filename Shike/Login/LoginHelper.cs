using System.IO;
using System.Windows.Forms;
using Shike.Login;

namespace Shike
{
    class LoginHelper
    {
        public static readonly LoginHelper Current = new LoginHelper();

        bool logged;
        LoginHelper() { }

        public bool Log()
        {
            if (logged)
            {
                return true;
            }

            ApplyContext.Current.ShowMessage("开始登陆");
            var frm = new LoginForm();
            if (frm.ShowDialog() == DialogResult.Cancel)
            {
                ApplyContext.Current.ShowMessage("取消登陆");
                return false;
            }
            ApplyContext.Current.ShowMessage("登陆成功");
            return logged = true;
        }
    }

    public class LocalConfig
    {
        const string fileName = "localConfig.json";
        public static readonly LocalConfig Current = GetLocalConfig();

        LocalConfig() { }

        public bool AutoLogin { get; set; }
        public string LoginEmail { get; set; }
        public string LoginPassword { get; set; }
        public string Cookie { get; set; }

        public string RecipeID { get; set; }

        public string StorageID { get; set; }

        public string RecipeEnglishName { get; set; }

        public string RecipeName { get; set; }

        static LocalConfig GetLocalConfig()
        {
            try
            {
                return JsonHelper.ParseFromJson<LocalConfig>(File.ReadAllText(fileName));
            }
            catch
            {
                return new LocalConfig();
            }
        }

        internal void Save() { File.WriteAllText(fileName, JsonHelper.GetJson(this)); }
    }
}