using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Spider.Common;

namespace Spider.Download
{
    public abstract class SuperDownload
    {
        #region private fields

        //下载链接的线程数量
        private int urlThreadCount = 4;
        //下载图片的线程数量
        private int imgThreadCount = 4;
        //下载链接的线程管理者
        private WorkingUnitCollection urlWorkingSignals;
        //下载图片的线程管理者
        private WorkingUnitCollection imgWorkingSignals;
        //停止标识
        private bool stop = true;
        //用户代理
        private string userAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0)";
        //接受的内容
        private string accept = "text/html";
        //请求方法
        private string method = "GET";
        //网页编码
        private Encoding pageEncoding = Encoding.UTF8;
        //链接下载的超时间隔
        private int UrlTimeOutInterval = 2 * 60 * 1000;
        //图片下载的超时间隔
        private int ImgTimeOutInterval = 2 * 60 * 1000;
        //检查是否完成的定时器
        private Timer checkTimer = null;
        //最小的图片尺寸
        private int imgMinSize = 50 * 1024;

        #endregion

        #region properties

        /// <summary>
        /// 保存路径
        /// </summary>
        public string SaveDirPath { get; set; }

        /// <summary>
        /// 关键词
        /// </summary>
        public string Key { get; set; }

        /// <summary>
        /// 网页编码类型
        /// </summary>
        public Encoding PageEncoding
        {
            get
            {
                return pageEncoding;
            }
            set
            {
                pageEncoding = value;
            }
        }

        /// <summary>
        /// 最小的图片尺寸
        /// </summary>
        public int ImgMinSize
        {
            get
            {
                return imgMinSize;
            }
            set
            {
                imgMinSize = Math.Max(value, 1);
            }
        }

        /// <summary>
        /// 已下载图片数量
        /// </summary>
        public int ImgDownloadCount
        {
            get
            {
                return imgsDownloaded.Count;
            }
        }

        /// <summary>
        /// 下载根Url
        /// </summary>
        public string RootUrl
        {
            get
            {
                return rootUrl;
            }
            set
            {
                if (!value.Contains("http://"))
                {
                    rootUrl = "http://" + value;
                }
                else
                {
                    rootUrl = value;
                }
                baseUrl = rootUrl.Replace("www.", "");
                baseUrl = baseUrl.Replace("http://", "");
                baseUrl = baseUrl.Substring(0, baseUrl.LastIndexOf('/'));
                baseUrl = baseUrl.TrimEnd('/');
            }
        }

        /// <summary>
        /// 图片总数量
        /// </summary>
        public int ImgTotalCount
        {
            get
            {
                return imgTotalCount;
            }
        }

        /// <summary>
        /// 网页链接的深度
        /// </summary>
        public int MaxDepth
        {
            get
            {
                return maxDepth;
            }
            set
            {
                maxDepth = value;
            }
        }

        /// <summary>
        /// 下载链接的线程数量
        /// </summary>
        public int UrlThreadCount
        {
            get { return urlThreadCount; }
            set { urlThreadCount = value; }
        }

        /// <summary>
        /// 下载图片的线程数量
        /// </summary>
        public int ImgThreadCount
        {
            get { return imgThreadCount; }
            set { imgThreadCount = value; }
        }

        #endregion

        #region inherit fields
        //链接已下载列表
        protected Dictionary<string, int> urlsLoaded = new Dictionary<string, int>();
        //链接未下载列表
        protected Dictionary<string, int> urlsUnload = new Dictionary<string, int>();
        //图片未下载列表
        protected List<string> imgsUnDownload = new List<string>();
        //图片已下载列表
        protected List<string> imgsDownloaded = new List<string>();
        //链接锁
        protected readonly object urlDownloadLocker = new object();
        //图片锁
        protected readonly object imgsDownladLocker = new object();
        //基地址 如http://news.sina.com.cn/，将会保存为news.sina.com.cn
        protected string baseUrl = null;
        //第一个下载的Url
        protected string rootUrl = null;
        //图片总数
        protected int imgTotalCount;
        //网页链接的深度
        protected int maxDepth = 2;

        #endregion

        #region events

        /// <summary>
        /// 所有图片下载完成事件
        /// </summary>
        public event Action ImgDownloadAllFinish = null;

        /// <summary>
        /// 每张图片下载完成事件
        /// </summary>
        public event Action<string, string> ImgDownloadFinish = null;

        #endregion

        #region constructor
        public SuperDownload()
        {
            Config config = Config.GetInstance();
            maxDepth = config.MaxDepth;
            urlThreadCount = config.UrlThreadCount;
            imgThreadCount = config.ImgThreadCount;
            imgMinSize = config.ImgMinSize * 1024;
        } 
        #endregion

        #region public methods

        /// <summary>
        /// 开始下载
        /// </summary>
        /// <param name="path">保存本地文件的目录</param>
        public void Download()
        {
            if (string.IsNullOrEmpty(SaveDirPath))
            {
                return;
            }
            Init();
            StartDownload();
        }

        /// <summary>
        /// 终止下载
        /// </summary>
        public void Abort()
        {
            stop = true;
            if (urlWorkingSignals != null)
            {
                urlWorkingSignals.AbortAllWork();
            }

            if (imgWorkingSignals != null)
            {
                imgWorkingSignals.AbortAllWork();
            }
        }

        #endregion

        #region private methods

        /// <summary>
        /// 初始化
        /// </summary>
        private void Init()
        {
            urlsLoaded.Clear();
            urlsUnload.Clear();
            imgsDownloaded.Clear();
            imgsUnDownload.Clear();

            urlWorkingSignals = new WorkingUnitCollection(urlThreadCount);
            imgWorkingSignals = new WorkingUnitCollection(imgThreadCount);
            stop = false;

            Init_Extend();
        }

        /// <summary>
        /// 开始下载
        /// </summary>
        private void StartDownload()
        {
            DispatchUrlWork();
            checkTimer = new Timer(new TimerCallback(CheckFinish), null, 0, 1000);
        }

        #region 链接下载
        /// <summary>
        /// 分配链接工作线程
        /// </summary>
        private void DispatchUrlWork()
        {
            if (stop)
            {
                return;
            }
            for (int i = 0; i < urlThreadCount; i++)
            {
                if (!urlWorkingSignals.IsWorking(i))
                {
                    RequestHtmlResource(i);
                }
            }
        }

        /// <summary>
        /// 发送获取html请求
        /// </summary>
        /// <param name="index"></param>
        private void RequestHtmlResource(int index)
        {
            int depth;
            string url = string.Empty;
            try
            {
                lock (urlDownloadLocker)
                {
                    if (urlsUnload.Count <= 0)
                    {
                        urlWorkingSignals.FinishWorking(index);
                        return;
                    }
                    urlWorkingSignals.StartWorking(index);
                    depth = urlsUnload.First().Value;
                    url = urlsUnload.First().Key;
                    urlsLoaded.Add(url, depth);
                    urlsUnload.Remove(url);
                }

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = method;
                req.Accept = accept;
                req.UserAgent = userAgent;

                RequestState rs = new RequestState();
                rs.Request = req;
                rs.Url = url;
                rs.Depth = depth;
                rs.Index = index;
                rs.BUFFER_SIZE = 131072;
                rs.BufferRead = new byte[rs.BUFFER_SIZE];
                var result = req.BeginGetResponse(new AsyncCallback(ReceivedHtmlResource), rs);
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle,
                        UrlTimeoutCallback, rs, UrlTimeOutInterval, true);
            }
            catch (Exception e)
            {
                DealUrlException(index, url, e);
            }
        }

        /// <summary>
        /// 请求资源回调方法
        /// </summary>
        /// <param name="ar"></param>
        private void ReceivedHtmlResource(IAsyncResult ar)
        {
            RequestState rs = (RequestState)ar.AsyncState;
            HttpWebRequest req = rs.Request;
            try
            {
                if (stop)
                {
                    req.Abort();
                    return;
                }

                HttpWebResponse res = (HttpWebResponse)req.EndGetResponse(ar);
                if (res != null && res.StatusCode == HttpStatusCode.OK)
                {
                    Stream resStream = res.GetResponseStream();
                    rs.ResponseStream = resStream;
                    var result = resStream.BeginRead(rs.BufferRead, 0, rs.BufferRead.Length,
                        new AsyncCallback(ReceivedHtmlData), rs);
                }
                else
                {
                    res.Close();
                    req.Abort();
                    urlWorkingSignals.FinishWorking(rs.Index);
                    DispatchUrlWork();
                }
            }
            catch (Exception e)
            {
                DealUrlException(rs.Index, rs.Url, e);
            }
        }

        /// <summary>
        /// 请求返回的数据回调方法
        /// </summary>
        /// <param name="ar"></param>
        private void ReceivedHtmlData(IAsyncResult ar)
        {
            RequestState rs = (RequestState)ar.AsyncState;
            HttpWebRequest req = rs.Request;
            Stream resStream = rs.ResponseStream;
            string url = rs.Url;
            int depth = rs.Depth;
            string html = null;
            int index = rs.Index;
            int read = 0;

            try
            {
                read = resStream.EndRead(ar);
                if (stop)
                {
                    rs.ResponseStream.Close();
                    req.Abort();
                    return;
                }
                if (read > 0)
                {
                    MemoryStream ms = new MemoryStream(rs.BufferRead, 0, read);
                    StreamReader reader = new StreamReader(ms, pageEncoding);
                    string str = reader.ReadToEnd();
                    rs.Html.Append(str);
                    var result = resStream.BeginRead(rs.BufferRead, 0, rs.BufferRead.Length,
                        new AsyncCallback(ReceivedHtmlData), rs);
                    return;
                }
                html = rs.Html.ToString();

                ReceivedHtmlData_Extend(rs.Url, html, depth);

                urlWorkingSignals.FinishWorking(rs.Index);
                DispatchUrlWork();
                DispatchImgWork();
            }
            catch (Exception e)
            {
                DealUrlException(index, url, e);
            }
        }

        /// <summary>
        /// 请求超时处理
        /// </summary>
        /// <param name="state"></param>
        /// <param name="timedOut"></param>
        private void UrlTimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                RequestState rs = state as RequestState;
                if (rs != null)
                {
                    rs.Request.Abort();
                }
                urlWorkingSignals.FinishWorking(rs.Index);
                DispatchUrlWork();
            }
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        /// <param name="index"></param>
        /// <param name="url"></param>
        /// <param name="e"></param>
        private void DealUrlException(int index, string url, Exception e)
        {
            CommonHelper.LogError(typeof(SuperDownload), e, " 链接：" + url);
            urlWorkingSignals.FinishWorking(index);
            DispatchUrlWork();
        }

        /// <summary>
        /// 检查工作线程是否全部完成
        /// </summary>
        /// <param name="param"></param>
        private void CheckFinish(object param)
        {
            if (urlWorkingSignals.IsAllFinished() && imgWorkingSignals.IsAllFinished())
            {
                checkTimer.Dispose();
                checkTimer = null;
                if (!stop && ImgDownloadAllFinish != null)
                {
                    ImgDownloadAllFinish();
                }
            }
        }
        #endregion

        #region 图片下载
        /// <summary>
        /// 分配下载图片线程
        /// </summary>
        private void DispatchImgWork()
        {
            if (stop)
            {
                return;
            }
            for (int i = 0; i < imgThreadCount; i++)
            {
                if (!imgWorkingSignals.IsWorking(i))
                {
                    RequestImgResource(i);
                }
            }
        }

        /// <summary>
        /// 下载图片
        /// </summary>
        /// <param name="state"></param>
        private void RequestImgResource(int index)
        {
            string imgUrl = string.Empty;
            try
            {
                lock (imgsDownladLocker)
                {
                    if (imgsUnDownload.Count <= 0)
                    {
                        imgWorkingSignals.FinishWorking(index);
                        return;
                    }
                    imgWorkingSignals.StartWorking(index);
                    imgUrl = imgsUnDownload.First();
                    imgsUnDownload.Remove(imgUrl);
                }

                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(imgUrl);
                RequestState rs = new RequestState();
                rs.BUFFER_SIZE = 1024;
                rs.BufferRead = new byte[rs.BUFFER_SIZE];
                rs.Request = request;
                rs.Url = imgUrl;
                rs.Index = index;
                rs.SavePath = Path.Combine(
                    SaveDirPath,
                    Guid.NewGuid().ToString("N") 
                    + Path.GetExtension(imgUrl.Contains(".jpg") ? imgUrl : (imgUrl + ".jpg")));

                //开始异步向服务器发起请求
                var result = request.BeginGetResponse(new AsyncCallback(ReceivedImgResource), rs);
                //使用托管线程池创建后台线程,处理超时情况
                ThreadPool.RegisterWaitForSingleObject(result.AsyncWaitHandle, ImgTimeoutCallback, rs, ImgTimeOutInterval, true);
            }
            catch (Exception e)
            {
                DealImgException(index, imgUrl, e);
            }
        }

        /// <summary>
        /// 请求图片回调方法
        /// </summary>
        /// <param name="ar"></param>
        private void ReceivedImgResource(IAsyncResult ar)
        {
            RequestState rs = (RequestState)ar.AsyncState;
            try
            {
                if (stop)
                {
                    rs.Request.Abort();
                    return;
                }

                rs.Response = (HttpWebResponse)rs.Request.EndGetResponse(ar);

                if (rs.Response != null && rs.Response.StatusCode == HttpStatusCode.OK)
                {
                    if (rs.Response.ContentLength > ImgMinSize)
                    {
                        rs.FileStream = new FileStream(rs.SavePath, FileMode.OpenOrCreate);
                        Stream resStream = rs.Response.GetResponseStream();
                        rs.ResponseStream = resStream;
                        resStream.BeginRead(rs.BufferRead, 0, rs.BufferRead.Length, ReceivedImgData, rs);
                    }
                    else
                    {
                        ReleaseImgResource(rs);
                    }
                }
                else
                {
                    ReleaseImgResource(rs);
                }
            }
            catch (Exception e)
            {
                DealImgException(rs.Index, rs.Url, e);
            }
        }

        /// <summary>
        /// 保存图片回调方法
        /// </summary>
        /// <param name="ar"></param>
        private void ReceivedImgData(IAsyncResult ar)
        {
            RequestState rs = (RequestState)ar.AsyncState;
            try
            {
                if (stop)
                {
                    rs.ResponseStream.Close();
                    rs.Request.Abort();
                    rs.FileStream.Close();
                    return;
                }

                int read = rs.ResponseStream.EndRead(ar);
                if (read > 0)
                {
                    rs.FileStream.Write(rs.BufferRead, 0, read);
                    rs.ResponseStream.BeginRead(rs.BufferRead, 0, rs.BufferRead.Length, ReceivedImgData, rs);
                }
                else
                {

                    lock (imgsDownladLocker)
                    {
                        imgsDownloaded.Add(rs.Url);
                    }

                    if (ImgDownloadFinish != null)
                    {
                        ImgDownloadFinish(rs.Url, rs.SavePath);
                    }
                    ReleaseImgResource(rs);
                }
            }
            catch (Exception e)
            {
                DealImgException(rs.Index, rs.Url, e);
            }
        }

        /// <summary>
        /// 释放图片链接
        /// </summary>
        /// <param name="rs"></param>
        private void ReleaseImgResource(RequestState rs)
        {
            if (rs.Response != null)
            {
                rs.Response.Close();
            }
            if (rs.Request != null)
            {
                rs.Request.Abort();
            }
            if (rs.ResponseStream != null)
            {
                rs.ResponseStream.Close();
            }
            if (rs.FileStream != null)
            {
                rs.FileStream.Close();
            }
            imgWorkingSignals.FinishWorking(rs.Index);
            DispatchImgWork();
        }

        /// <summary>
        /// 处理异常
        /// </summary>
        /// <param name="index"></param>
        /// <param name="url"></param>
        /// <param name="e"></param>
        private void DealImgException(int index, string imgUrl, Exception e)
        {
            CommonHelper.LogError(typeof(SuperDownload), e, " 链接：" + imgUrl);
            imgWorkingSignals.FinishWorking(index);
            DispatchImgWork();
        }

        /// <summary>
        /// 图片请求超时处理
        /// </summary>
        /// <param name="state"></param>
        /// <param name="timedOut"></param>
        private void ImgTimeoutCallback(object state, bool timedOut)
        {
            if (timedOut)
            {
                ReleaseImgResource(state as RequestState);
            }
        }
        #endregion

        #endregion

        #region inherit methods

        /// <summary>
        /// 是否存在Url
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected bool IsExistsUrl(string url)
        {
            bool result = urlsUnload.ContainsKey(url);
            result |= urlsLoaded.ContainsKey(url);
            return result;
        }

        /// <summary>
        /// 是否存在图片
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        protected bool IsExistsImgUrl(string url)
        {
            bool result = imgsUnDownload.Contains(url);
            result |= imgsDownloaded.Contains(url);
            return result;
        }

        #endregion

        #region abstract methods

        /// <summary>
        /// 初始化扩展方法
        /// </summary>
        protected abstract void Init_Extend();

        /// <summary>
        /// 接收html后处理扩展方法
        /// </summary>
        /// <param name="html"></param>
        /// <param name="depth"></param>
        protected abstract void ReceivedHtmlData_Extend(string url, string html, int depth);

        #endregion
    }
}
