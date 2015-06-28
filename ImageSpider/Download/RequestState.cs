using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;

namespace Spider
{
    public class RequestState
    {
        /// <summary>  
        /// 缓冲区大小  
        /// </summary>  
        public int BUFFER_SIZE { get; set; }

        /// <summary>  
        /// 缓冲区  
        /// </summary>  
        public byte[] BufferRead { get; set; }

        /// <summary>
        /// Html
        /// </summary>
        private StringBuilder html = new StringBuilder();
        public StringBuilder Html { get { return html; } }

        /// <summary>  
        /// 请求流  
        /// </summary> 
        public HttpWebRequest Request { get; set; }

        /// <summary>  
        /// 响应流  
        /// </summary>  
        public HttpWebResponse Response { get; set; }

        /// <summary>
        /// 链接地址
        /// </summary>
        public string Url { get; set; }

        /// <summary>
        /// 工作线程索引
        /// </summary>
        public int Index { get; set; }

        /// <summary>  
        /// 流对象  
        /// </summary> 
        public Stream ResponseStream { get; set; }

        /// <summary>  
        /// 文件流  
        /// </summary>  
        public FileStream FileStream { get; set; }

        /// <summary>  
        /// 保存路径  
        /// </summary>  
        public string SavePath { get; set; }
        /// <summary>
        /// 网页深度
        /// </summary>
        public int Depth { get; set; }
    }
}
