using System.ComponentModel;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace Shike
{
    [DataContract]
    public class Product : INotifyPropertyChanged
    {
        [DataMember]
        public string DetailUrl { get; set; }

        [DataMember]
        public string ImgUrl { get; set; }

        [DataMember]
        public string Caption { get; set; }

        /// <summary>
        /// 返回表示当前对象的字符串。
        /// </summary>
        /// <returns>
        /// 表示当前对象的字符串。
        /// </returns>
        public override string ToString()
        {
            return $"{Caption}({DetailUrl})";
        }

        public Image Image { get; set; }

        public string ID
        {
            get
            {
                if (string.IsNullOrWhiteSpace(DetailUrl))
                {
                    return null;
                }

                var match = Regex.Match(DetailUrl, "http://.*.shikee.com/(.*?).html");

                if (match.Groups.Count < 2)
                {
                    MessageBox.Show("根据网址获取不出ID：" + DetailUrl);
                    return null;
                }

                return match.Groups[1].Value;
            }
        }

        public bool LoadImage { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}