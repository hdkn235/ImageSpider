using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.DirectoryServices.Protocols;
using System.ServiceModel.Security;
using System.Net;
using System.IO;
using System.IO.Compression;
using System.Text.RegularExpressions;
using System.Threading;
using log4net;
using System.Runtime.InteropServices;

[assembly: log4net.Config.XmlConfigurator(Watch = true)]
namespace Spider
{
    public class CommonHelper
    {
        #region Http
        /// <summary>
        /// 操作系统及浏览器信息
        /// </summary>
        private static readonly string DefaultUserAgent = "Mozilla/5.0 (Windows NT 6.1) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/41.0.2272.101 Safari/537.36";

        /// <summary>
        /// 获取Get请求返回的字符串
        /// </summary>
        /// <param name="url">请求的URL</param>
        /// <param name="type">字符编码</param>
        /// <returns></returns>
        public static string GetResponseStrByGet(string url, string type)
        {
            HttpWebResponse res = CreateGetHttpResponse(url, null, null, null);
            Stream respStream = res.GetResponseStream();
            using (StreamReader reader = new StreamReader(respStream, Encoding.GetEncoding(type)))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>
        /// 获取Get请求返回的字符串
        /// </summary>
        /// <param name="url">请求的URL</param>
        /// <param name="type">字符编码</param>
        /// <returns></returns>
        public static string GetResponseStrByPost(string url, string type)
        {
            HttpWebResponse res = CreatePostHttpResponse(url, null, null, null, Encoding.GetEncoding(type), null);
            Stream respStream = res.GetResponseStream();
            using (StreamReader reader = new StreamReader(respStream, Encoding.GetEncoding(type)))
            {
                return reader.ReadToEnd();
            }
        }

        /// <summary>  
        /// 创建GET方式的HTTP请求  
        /// </summary>  
        /// <param name="url">请求的URL</param>  
        /// <param name="timeout">请求的超时时间</param>  
        /// <param name="userAgent">请求的客户端浏览器信息，可以为空</param>  
        /// <param name="cookies">随同HTTP请求发送的Cookie信息，如果不需要身份验证可以为空</param>  
        /// <returns></returns>  
        public static HttpWebResponse CreateGetHttpResponse(string url, int? timeout, string userAgent, CookieCollection cookies)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            HttpWebRequest request = WebRequest.Create(url) as HttpWebRequest;
            request.Method = "GET";
            request.UserAgent = DefaultUserAgent;
            if (!string.IsNullOrEmpty(userAgent))
            {
                request.UserAgent = userAgent;
            }
            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }
            return request.GetResponse() as HttpWebResponse;
        }

        /// <summary>  
        /// 创建POST方式的HTTP请求  
        /// </summary>  
        /// <param name="url">请求的URL</param>  
        /// <param name="parameters">随同请求POST的参数名称及参数值字典</param>  
        /// <param name="timeout">请求的超时时间</param>  
        /// <param name="userAgent">请求的客户端浏览器信息，可以为空</param>  
        /// <param name="requestEncoding">发送HTTP请求时所用的编码</param>  
        /// <param name="cookies">随同HTTP请求发送的Cookie信息，如果不需要身份验证可以为空</param>  
        /// <returns></returns>  
        public static HttpWebResponse CreatePostHttpResponse(string url, IDictionary<string, string> parameters, int? timeout, string userAgent, Encoding requestEncoding, CookieCollection cookies)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException("url");
            }
            if (requestEncoding == null)
            {
                throw new ArgumentNullException("requestEncoding");
            }
            HttpWebRequest request = null;
            //如果是发送HTTPS请求  
            if (url.StartsWith("https", StringComparison.OrdinalIgnoreCase))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                request = WebRequest.Create(url) as HttpWebRequest;
                request.ProtocolVersion = HttpVersion.Version10;
            }
            else
            {
                request = WebRequest.Create(url) as HttpWebRequest;
            }
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";

            if (!string.IsNullOrEmpty(userAgent))
            {
                request.UserAgent = userAgent;
            }
            else
            {
                request.UserAgent = DefaultUserAgent;
            }

            if (timeout.HasValue)
            {
                request.Timeout = timeout.Value;
            }
            if (cookies != null)
            {
                request.CookieContainer = new CookieContainer();
                request.CookieContainer.Add(cookies);
            }
            //如果需要POST数据  
            if (!(parameters == null || parameters.Count == 0))
            {
                StringBuilder buffer = new StringBuilder();
                int i = 0;
                foreach (string key in parameters.Keys)
                {
                    if (i > 0)
                    {
                        buffer.AppendFormat("&{0}={1}", key, parameters[key]);
                    }
                    else
                    {
                        buffer.AppendFormat("{0}={1}", key, parameters[key]);
                    }
                    i++;
                }
                byte[] data = requestEncoding.GetBytes(buffer.ToString());
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            return request.GetResponse() as HttpWebResponse;
        }

        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }
        #endregion

        /// <summary>
        /// 检查是否是可用的链接
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsAvailableUrl(string url)
        {
            if (url.Contains(".jpg") || url.Contains(".gif")
                || url.Contains(".png") || url.Contains(".css")
                || url.Contains(".js"))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// 检查是否是可用的图片链接
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static bool IsAvailableImgUrl(string url)
        {
            if (url.Contains(".jpg") || url.Contains(".png"))
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取链接的html内容
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string GetHtml(string url)
        {
            string html = string.Empty;
            try
            {
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Method = "GET";
                req.Accept = "text/html";
                req.UserAgent = "Mozilla/4.0 (compatible; MSIE 8.0; Windows NT 6.1; Trident/4.0)";

                HttpWebResponse res = (HttpWebResponse)req.GetResponse();
                using (StreamReader reader = new StreamReader(res.GetResponseStream()))
                {
                    html = reader.ReadToEnd();
                }
            }
            catch (WebException ex)
            {

            }
            return html;
        }

        /// <summary>
        /// 获取链接html内容中的连接
        /// </summary>
        /// <param name="html"></param>
        /// <returns></returns>
        public static string[] GetLinks(string html)
        {
            string pattern = @"http://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";
            Regex reg = new Regex(pattern, RegexOptions.IgnoreCase);
            MatchCollection mc = reg.Matches(html);
            string[] links = new string[mc.Count];
            for (int i = 0; i < mc.Count; i++)
            {
                links[i] = mc[i].Value;
            }
            return links;
        }

        /// <summary>
        /// 日志错误记录
        /// </summary>
        /// <param name="type"></param>
        public static void LogError(Type type, Exception ex, string attachMessage)
        {
            log4net.ILog log = log4net.LogManager.GetLogger(type);
            string strMessage = string.Format("错误信息：{0}\r\n堆栈:{1}\r\n附加信息:{2}", ex.Message, ex.StackTrace, attachMessage);
            log.Error(strMessage);
            log = null;
        }

        /// <summary>
        /// 根据相对路径得到绝对路径
        /// </summary>
        /// <param name="sUrl">输入内容</param>
        /// <param name="sInput">原始网站地址</param>
        /// <param name="sRelativeUrl">相对链接地址</param>
        public static string GetUrl(string sInput, string sRelativeUrl)
        {
            string sReturnUrl = "";
            string sUrl = GetStandardUrlDepth(sInput);//返回了[url]http://www.163.com/news/[/url]这种形式

            if (sRelativeUrl.ToLower().StartsWith("http") || sRelativeUrl.ToLower().StartsWith("https"))
            {
                sReturnUrl = sRelativeUrl.Trim();
            }
            else if (sRelativeUrl.StartsWith("/"))
            {
                sReturnUrl = GetDomain(sInput) + sRelativeUrl;
            }
            else if (sRelativeUrl.StartsWith("../"))
            {
                sUrl = sUrl.Substring(0, sUrl.Length - 1);
                while (sRelativeUrl.IndexOf("../") >= 0)
                {
                    string temp = sUrl.Substring(0, sUrl.LastIndexOf('/'));
                    if (temp.Length > 6)
                    {//temp != "http:/"，否则的话，说明已经回溯到尽头了，"../"与网址的层次对应不上。存在这种情况，网页上面的链接是错误的，但浏览器还能正常显示
                        sUrl = temp;
                    }
                    sRelativeUrl = sRelativeUrl.Substring(3);
                }
                sReturnUrl = sUrl + "/" + sRelativeUrl.Trim();
            }
            else if (sRelativeUrl.StartsWith("./"))
            {
                sReturnUrl = sUrl + sRelativeUrl.Trim().Substring(2);
            }
            else if (sRelativeUrl.Trim() != "")
            {//2007images/modecss.css
                sReturnUrl = sUrl + sRelativeUrl.Trim();
            }
            else
            {
                sRelativeUrl = sUrl;
            }
            return sReturnUrl;
        }

        /// <summary>
        /// 获得标准的URL路径深度
        /// </summary>
        /// <param name="url"></param>
        /// <returns>返回标准的形式：[url]http://www.163.com/[/url]或[url]http://www.163.com/news/[/url]。</returns>
        private static string GetStandardUrlDepth(string url)
        {
            string sheep = url.Trim().ToLower();
            string header = "http://";
            if (sheep.IndexOf("https://") != -1)
            {
                header = "https://";
                sheep = sheep.Replace("https://", "");
            }
            else
            {
                sheep = sheep.Replace("http://", "");
            }

            int p = sheep.LastIndexOf("/");
            if (p == -1)
            {//www.163.com
                sheep += "/";
            }
            else if (p == sheep.Length - 1)
            {//传来的是：[url]http://www.163.com/news/[/url]
            }
            else if (sheep.Substring(p).IndexOf(".") != -1)
            {//传来的是：[url]http://www.163.com/news/hello.htm[/url] 这种形式
                sheep = sheep.Substring(0, p + 1);
            }
            else
            {
                sheep += "/";
            }

            return header + sheep;
        }

        /// <summary>
        /// 根据URL获得域名
        /// </summary>
        /// <param name="sUrl">输入内容</param>
        public static string GetDomain(string sInput)
        {
            return GetText(sInput, @"http(s)?://([\w-]+\.)+(\w){2,}", 0);
        }

        /// <summary>
        /// 单个匹配内容
        /// </summary>
        /// <param name="sInput">输入内容</param>
        /// <param name="sRegex">表达式字符串</param>
        /// <param name="iGroupIndex">分组序号, 从1开始, 0不分组</param>
        public static string GetText(string sInput, string sRegex, int iGroupIndex)
        {
            Regex re = new Regex(sRegex, RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Multiline);
            Match mc = re.Match(sInput);
            string result = "";
            if (mc.Success)
            {
                if (iGroupIndex > 0)
                {
                    result = mc.Groups[iGroupIndex].Value;
                }
                else
                {
                    result = mc.Value;
                }
            }
            return result;
        }

        /// <summary>
        /// 保存配置文件
        /// </summary>
        /// <param name="path"></param>
        /// <param name="content"></param>
        public static void SaveConfig(string path, string content)
        {
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                sw.Write(content);
            }
        }

        /// <summary>
        /// 读取配置文件字符串
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetConfigStr(string path)
        {
            string result = string.Empty;
            if (File.Exists(path))
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    result = sr.ReadToEnd();
                }   
            }
            return result;
        }

        #region 检查网络状态

        //检测网络状态
        [DllImport("wininet.dll")]
        private extern static bool InternetGetConnectedState(out int connectionDescription, int reservedValue);
        /// <summary>
        /// 检测网络状态
        /// </summary>
        public static bool IsConnected()
        {
            int I = 0;
            bool state = InternetGetConnectedState(out I, 0);
            return state;
        }

        #endregion
    }
}
