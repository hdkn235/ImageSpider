using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json.Linq;

namespace Spider.Download
{
    public class BaiduDownload : SuperDownload
    {
        #region private fields
        //每页最大的数量
        private readonly int MaxPageCount = 60;

        //默认的链接
        //http://image.baidu.com/i?tn=baiduimagejson&ie=utf-8&word={0}&rn={1}&pn={2}
        //http://image.baidu.com/channel/listjson?tag1={0}&tag2=全部&ie=utf8&rn={1}&pn={2}
        private string defaultUrl = "http://image.baidu.com/data/imgs?col={col}&tag={tag}&sort=0&pn={pn}&rn={rn}&p=channel&from=1"; 
        #endregion

        #region override methods
        protected override void Init_Extend()
        {
            SetDefaultUrl();
            AddUrlsUnload();
        }

        protected override void ReceivedHtmlData_Extend(string url,string html, int depth)
        {
            AddImgUrls(html);
        } 
        #endregion

        #region private methods
        /// <summary>
        /// 设置默认链接
        /// </summary>
        private void SetDefaultUrl()
        {
            string tag = string.Empty;
            string col = string.Empty;
            string[] keyArray = Key.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (keyArray.Length > 1)
            {
                col = keyArray[0];
                tag = keyArray[1];
            }
            else
            {
                col = Key;
                tag = "全部";
            }
            defaultUrl = defaultUrl.Replace("{col}", col).Replace("{tag}", tag);
        }

        /// <summary>
        /// 设置需要发送的链接列表
        /// </summary>
        private void AddUrlsUnload()
        {
            imgTotalCount = GetTotalCount();
            Dictionary<int, int> dicPage = GetDicPage(imgTotalCount);
            foreach (var num in dicPage.Keys)
            {
                string url = GetUrl(dicPage[num], num);
                urlsUnload.Add(url, 1);
            }
        }

        /// <summary>
        /// 获取图片的总数量
        /// </summary>
        /// <returns></returns>
        private int GetTotalCount()
        {
            int num = 0;
            string url = GetUrl(1, 1);
            string responsStr = CommonHelper.GetResponseStrByGet(url, "utf-8");
            if (!string.IsNullOrEmpty(responsStr))
            {
                JObject json = JObject.Parse(responsStr);
                num = Convert.ToInt32(json["totalNum"]);
            }
            return num;
        }

        /// <summary>
        /// 获取页数字典
        /// </summary>
        /// <param name="totalCount"></param>
        private Dictionary<int, int> GetDicPage(int totalCount)
        {
            Dictionary<int, int> dicPage = new Dictionary<int, int>();
            double count = totalCount / (MaxPageCount * 1.0);

            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    dicPage.Add(i * MaxPageCount, MaxPageCount);
                }
            }

            return dicPage;
        }

        /// <summary>
        /// 获取百度图片的请求链接
        /// </summary>
        /// <param name="pageCount">每页显示图片数量</param>
        /// <param name="pageNumber">图片显示的页码</param>
        /// <returns></returns>
        private string GetUrl(int pageCount, int pageNumber)
        {
            string link = defaultUrl
                .Replace("{pn}", pageNumber.ToString())
                .Replace("{rn}", pageCount.ToString());
            return link;
        }

        /// <summary>
        /// 设置图片链接列表
        /// </summary>
        /// <param name="jsonStr"></param>
        private void AddImgUrls(string jsonStr)
        {
            if (!string.IsNullOrEmpty(jsonStr))
            {
                JObject json = JObject.Parse(jsonStr);
                var links = from p in json["imgs"].Children()["objUrl"].Values<string>()
                            where !imgsUnDownload.Any(i => i == p) && !imgsDownloaded.Any(i => i == p)
                            select p;
                lock (imgsDownladLocker)
                {
                    imgsUnDownload.AddRange(links);
                }
            }
        } 
        #endregion
    }
}
