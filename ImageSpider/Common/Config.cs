using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Spider.Common
{
    [JsonObject(Newtonsoft.Json.MemberSerialization.OptIn)]
    public sealed class Config
    {
        private int maxDepth = 1;

        [JsonProperty]
        public int MaxDepth
        {
            get { return maxDepth; }
            set { maxDepth = value; }
        }

        private int urlThreadCount = 3;

        [JsonProperty]
        public int UrlThreadCount
        {
            get { return urlThreadCount; }
            set { urlThreadCount = value; }
        }


        private int imgThreadCount = 3;

        [JsonProperty]
        public int ImgThreadCount
        {
            get { return imgThreadCount; }
            set { imgThreadCount = value; }
        }

        private int imgMinSize = 100;

        [JsonProperty]
        public int ImgMinSize
        {
            get { return imgMinSize; }
            set { imgMinSize = value; }
        }

        private static Config instance = null;
        private static readonly object locker = new object();
        private static readonly string path = "Settings.config";

        private Config()
        {

        }

        public static Config GetInstance()
        {
            if (instance == null)
            {
                lock (locker)
                {
                    if (instance == null)
                    {
                        instance = GetConfig();
                    }
                }
            }
            return instance;
        }

        public void Save()
        {
            string json = JsonConvert.SerializeObject(this, Formatting.Indented);
            CommonHelper.SaveConfig(path, json);
        }

        private static Config GetConfig()
        {
            string configStr = CommonHelper.GetConfigStr(path);
            Config config = null;
            if (string.IsNullOrEmpty(configStr))
            {
                config = new Config();
                string json = JsonConvert.SerializeObject(instance, Formatting.Indented);
                CommonHelper.SaveConfig(path, json);
            }
            else
            {
                config = JsonConvert.DeserializeObject<Config>(configStr);
            }
            return config;
        }
    }
}
