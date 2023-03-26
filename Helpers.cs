using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RutrackerMole_v2._1
{
    static partial class Helpers
    {
        public static string GetHTML(string strUrl)
        {
            int nTries = 0;

            Try:
            try
            {
                nTries++;

                HttpWebRequest htrRequest = (HttpWebRequest)WebRequest.Create(strUrl);
                //HttpWebResponse htrResponse = null;
                //if (htrRequest.GetResponseAsync().Wait(new TimeSpan(0, 0, 10)))
                //    htrResponse = (HttpWebResponse)htrRequest.GetResponse();
                //else return null;

                using (HttpWebResponse blip = null)
                {
                    HttpWebResponse htrResponse = null;

                    if (htrRequest.GetResponseAsync().Wait(new TimeSpan(0, 0, 10)))
                        htrResponse = (HttpWebResponse)htrRequest.GetResponse();
                    //else return null;
                    else throw new Exception();

                    if (htrResponse.StatusCode == HttpStatusCode.OK)
                    {
                        //Stream sReceiveStream = htrResponse.GetResponseStream();
                        //StreamReader srReader = null;

                        //if (htrResponse.CharacterSet == null)
                        //    srReader = new StreamReader(sReceiveStream);
                        //else
                        //    srReader = new StreamReader(sReceiveStream, Encoding.GetEncoding(htrResponse.CharacterSet));

                        //return srReader.ReadToEnd();

                        using (Stream sReceiveStream = htrResponse.GetResponseStream())
                        {
                            StreamReader srReader = null;

                            if (htrResponse.CharacterSet == null)
                                srReader = new StreamReader(sReceiveStream);
                            else
                                srReader = new StreamReader(sReceiveStream, Encoding.GetEncoding(htrResponse.CharacterSet));

                            return srReader.ReadToEnd();
                        }
                    }
                }

                //if (htrResponse.StatusCode == HttpStatusCode.OK)
                //{
                //    Stream sReceiveStream = htrResponse.GetResponseStream();
                //    StreamReader srReader = null;

                //    if (htrResponse.CharacterSet == null)
                //        srReader = new StreamReader(sReceiveStream);
                //    else
                //        srReader = new StreamReader(sReceiveStream, Encoding.GetEncoding(htrResponse.CharacterSet));

                //    return srReader.ReadToEnd();
                //}
            }
            catch (Exception)
            {
                if (nTries < 3)
                    goto Try;

                lock (oLock)
                    onAbort();
            }

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
            if (!IsUrl(strURL))
                strURL = $"https://rutracker.org/forum/viewtopic.php?t={strURL}&start={0}";

            string strHTML = GetHTML(strURL);
            int nFlagIndex = strHTML.IndexOf("</a>&nbsp;&nbsp;<a class=\"pg\"");
            strHTML = strHTML.Substring(nFlagIndex - 10);
            strHTML = strHTML.Substring(strHTML.IndexOf(">") + 1);

            return int.Parse(strHTML.Substring(0, strHTML.IndexOf("<")));
        }
        public static bool CheckHTML(string strHTML, string strFind)
            => strHTML.Contains(strFind);
        public static bool IsUrl(string strString)
            => !(new char[10] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' }).Contains(strString.First());
        public static ImageSource ToImageSource(this Icon iIcon)
        {
            Bitmap bitmap = iIcon.ToBitmap();
            IntPtr hBitmap = bitmap.GetHbitmap();

            ImageSource wpfBitmap = Imaging.CreateBitmapSourceFromHBitmap(
                hBitmap,
                IntPtr.Zero,
                Int32Rect.Empty,
                BitmapSizeOptions.FromEmptyOptions());

            if (!GDI32_dll.DeleteObject(hBitmap))
            {
                throw new Win32Exception();
            }

            return wpfBitmap;
        }
        public static List<string> GetForumIdsFromFile(string strFilePath)
        {
            List<string> lsRet = File.ReadAllLines(strFilePath).ToList();

            for (int i = 0; i < lsRet.Count; i++)
                lsRet[i] = IsUrl(lsRet[i]) ? GetForumIdFromURL(lsRet[i]) : lsRet[i];

            return lsRet;
        }
        public static List<int> GetPagesByForumIds(IEnumerable<string> iesForumIds)
            => (from id in iesForumIds
                let pages = CountPages(id)
                select pages).ToList();
        public static ObservableCollection<ForumIdAndPage_Display> Sort(this ObservableCollection<ForumIdAndPage_Display> ocfiapdSource)
        {
            List<ForumIdAndPage_Display> lfiapdTemp = new List<ForumIdAndPage_Display>();
            Dictionary<string, List<ForumIdAndPage_Display>> dslfiapdSorter = new Dictionary<string, List<ForumIdAndPage_Display>>();

            foreach (var item in ocfiapdSource)
            {
                if (!dslfiapdSorter.Keys.ToList().Contains(item.ForumID))
                    dslfiapdSorter.Add(item.ForumID, new List<ForumIdAndPage_Display>());

                dslfiapdSorter[item.ForumID].Add(item);
            }

            dslfiapdSorter = dslfiapdSorter.OrderBy(x => x.Key).ToDictionary(x => x.Key, y => y.Value);

            foreach (var item in dslfiapdSorter)
                lfiapdTemp.AddRange(item.Value.OrderBy(x => x.PageNumber).ToList());

            return new ObservableCollection<ForumIdAndPage_Display>(lfiapdTemp);
        }
    }

    static partial class Helpers
    {
        private static object oLock = new object();
    }

    static partial class Helpers
    {
        public static event EventHandler<EventArgs> Abort;

        private static void onAbort()
        {
            if (Abort != null)
                Abort(null, new EventArgs());
        }
    }

    static partial class Helpers
    {
        public class ForumIdAndPage_Display : IComparable
        {
            public string ForumID { get; set; }
            public int PageNumber { get; set; }

            public int CompareTo(object obj)
                => PageNumber.CompareTo(((ForumIdAndPage_Display)obj).PageNumber);

            public override string ToString()
                => $"{ForumID} => {PageNumber}";
        }
    }
}
