using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Spider.Download
{
    public class SpiderDownload : SuperDownload
    {
        #region override methods
        protected override void Init_Extend()
        {
            AddUrlsUnload(new string[1] { RootUrl }, 0);
        }

        protected override void ReceivedHtmlData_Extend(string url, string html, int depth)
        {
            string[] urls = GetUrls(url, html);
            AddUrlsUnload(urls, depth + 1);
            AddImgUrls(urls);
        }
        #endregion

        #region private methods
        /// <summary>
        /// 设置需要发送的链接列表
        /// </summary>
        /// <param name="urls"></param>
        /// <param name="depth"></param>
        private void AddUrlsUnload(string[] urls, int depth)
        {
            if (depth > maxDepth)
            {
                return;
            }
            foreach (string url in urls)
            {
                string cleanUrl = url.Trim();
                int end = cleanUrl.IndexOf(' ');
                if (end > 0)
                {
                    cleanUrl = cleanUrl.Substring(0, end);
                }
                cleanUrl = cleanUrl.TrimEnd('/');
                if (CommonHelper.IsAvailableUrl(cleanUrl) && !IsExistsUrl(cleanUrl))
                {
                    if (cleanUrl.Contains(baseUrl))
                    {
                        lock (urlDownloadLocker)
                        {
                            urlsUnload.Add(cleanUrl, depth);
                        }
                    }
                    else
                    {
                        // 外链
                        //urlsUnload.Add(cleanUrl, depth);
                    }
                }
            }
        }

        /// <summary>
        /// 从html中提取可用的链接
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        private string[] GetUrls(string url, string html)
        {
            List<string> list = new List<string>();
            string pattern = @"http://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";
            Regex r = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection m = r.Matches(html);
            for (int i = 0; i < m.Count; i++)
            {
                list.Add(m[i].ToString());
            }

            pattern = @"(?i)<a[^>]*href=([""'])?(?<href>[^'""]+)\1[^>]*>";
            r = new Regex(pattern, RegexOptions.IgnoreCase);
            m = r.Matches(html);

            string temp = string.Empty;
            for (int i = 0; i < m.Count; i++)
            {
                temp = m[i].Groups["href"].Value;

                if (temp.Length < 1)
                {
                    continue;
                }

                if (temp.IndexOf('#') == 0)
                {
                    continue;
                }

                if (list.Contains(temp))
                {
                    continue;
                }

                temp = CommonHelper.GetUrl(url, temp);

                list.Add(temp);
            }
            return list.ToArray();
        }

        /// <summary>
        /// 设置图片链接列表
        /// </summary>
        /// <param name="html"></param>
        private void AddImgUrls(string[] urls)
        {
            foreach (string url in urls)
            {
                string cleanUrl = url.Trim();
                int end = cleanUrl.IndexOf(' ');
                if (end > 0)
                {
                    cleanUrl = cleanUrl.Substring(0, end);
                }
                cleanUrl = cleanUrl.TrimEnd('/');
                if (CommonHelper.IsAvailableImgUrl(cleanUrl) && !IsExistsImgUrl(cleanUrl))
                {
                    lock (imgsDownladLocker)
                    {
                        imgsUnDownload.Add(cleanUrl);
                    }
                }
            }
        }
        #endregion
    }
}
