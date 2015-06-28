using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Spider
{
    public class InputPara
    {
        private string url;

        public string Url
        {
            get { return url.Trim(); }
            set { url = value; }
        }
        private string key;

        public string Key
        {
            get { return key.Trim(); }
            set { key = value; }
        }
        private string saveDirPath;

        public string SaveDirPath
        {
            get
            {
                return string.IsNullOrEmpty(saveDirPath.Trim()) ? "Images" : saveDirPath.Trim();
            }
            set { saveDirPath = value; }
        }

        private static InputPara instance = null;
        private static object _lock = new object();

        private InputPara()
        {

        }

        public static InputPara GetInstance()
        {
            if (instance == null)
            {
                lock (_lock)
                {
                    if (instance == null)
                    {
                        instance = new InputPara();
                    }
                }
            }

            return instance;
        }
    }
}
