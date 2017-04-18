using System;
using System.Linq;
using System.Windows.Forms;

namespace Shike
{
    static class SPI
    {
        public static double GetJSTime() { return (DateTime.Now - new DateTime(1970, 1, 1)).TotalMilliseconds; }

        public static bool IsEmpty(this string input) { return string.IsNullOrWhiteSpace(input); }

        public static HtmlElement GetElementByTagAndClass(this WebBrowser ie, string tagName, string className)
        {
            if (ie.InvokeRequired)
            {
                return
                    ie.Invoke((Func<string, string, HtmlElement>)ie.GetElementByTagAndClass, tagName, className) as
                        HtmlElement;
            }

            return
                ie.Document.GetElementsByTagName(tagName).Cast<HtmlElement>().FirstOrDefault(
                    i => i.GetAttribute("className") == className);
        }
    }
}