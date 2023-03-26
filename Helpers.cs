using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace RutrackerMole_v1_0
{
    static class Helpers
    {
        public static string GetHTML(string strUrl)
        {
            try
            {
                HttpWebRequest htrRequest = (HttpWebRequest)WebRequest.Create(strUrl);
                HttpWebResponse htrResponse = null;
                if (htrRequest.GetResponseAsync().Wait(new TimeSpan(0, 0, 10)))
                    htrResponse = (HttpWebResponse)htrRequest.GetResponse();
                else return null;

                if (htrResponse.StatusCode == HttpStatusCode.OK)
                {
                    Stream sReceiveStream = htrResponse.GetResponseStream();
                    StreamReader srReader = null;

                    if (htrResponse.CharacterSet == null)
                        srReader = new StreamReader(sReceiveStream);
                    else
                        srReader = new StreamReader(sReceiveStream, Encoding.GetEncoding(htrResponse.CharacterSet));

                    return srReader.ReadToEnd();
                }
            }
            catch (Exception) { }

            return null;
        }
        public static string GetForumIdFromURL(string strURL)
        {
            int nStartSeak = strURL.IndexOf("?t=") + 3;
            int nLen = strURL.Contains("&start=") ? strURL.IndexOf("&start=") - nStartSeak : -1;

            return strURL.Contains("&start=") ? strURL.Substring(nStartSeak, nLen) : strURL.Substring(nStartSeak);
        }
        public static int GetPageNumberByURL(string strURL)
            => strURL.Contains("&start=") ? int.Parse(strURL.Substring(strURL.IndexOf("&start=") + 7)) / 30 + 1 : 1;
        public static int CountPages(string strURL)
        {
            string strHTML = GetHTML(strURL);
            int nFlagIndex = strHTML.IndexOf("</a>&nbsp;&nbsp;<a class=\"pg\"");
            strHTML = strHTML.Substring(nFlagIndex - 10);
            strHTML = strHTML.Substring(strHTML.IndexOf(">") + 1);

            return int.Parse(strHTML.Substring(0, strHTML.IndexOf("<")));
        }
        public static bool CheckHTML(string strHTML, string strFind)
            => strHTML.Contains(strFind);
    }
}
