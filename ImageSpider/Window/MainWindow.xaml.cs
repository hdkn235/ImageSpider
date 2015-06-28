using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Threading;
using System.Diagnostics;
using System.Windows.Threading;
using System.Collections;
using System.Collections.Specialized;
using Spider.Download;

namespace Spider
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        #region 变量
        private SuperDownload sd = null;
        #endregion

        #region 类型
        private class ImgDownload
        {
            public string Url { get; set; }

            public string ImgPath { get; set; }
        }

        #endregion

        #region 初始化
        public MainWindow()
        {
            InitializeComponent();
        }
        #endregion

        #region 爬虫下载
        /// <summary>
        /// 爬虫下载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSpiderDownload_Click(object sender, RoutedEventArgs e)
        {
            if (CheckIsConnected() && CheckSpiderInputPara())
            {
                BeforeSpiderDownload();

                InputPara config = GetInputPara();
                Thread th = new Thread(SpiderDownload);
                th.IsBackground = true;
                th.Start(config);
            }
        }

        /// <summary>
        /// 检查爬虫下载输入参数合法性
        /// </summary>
        /// <returns></returns>
        private bool CheckSpiderInputPara()
        {
            if (string.IsNullOrEmpty(txtUrl.Text.Trim()))
            {
                System.Windows.MessageBox.Show("请输入网址！");
                return false;
            }

            if (string.IsNullOrEmpty(txtOutputPath.Text.Trim()))
            {
                System.Windows.MessageBox.Show("请输入保存路径！");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 下载前更新控件状态
        /// </summary>
        private void BeforeSpiderDownload()
        {
            btnSpiderDownload.IsEnabled = false;
            btnSpiderDownload.Content = "下载中...";
            btnStop.IsEnabled = true;
            btnBaiduDownLoad.IsEnabled = false;

            tbProgress.Text = "正在下载中...";

            ListDownload.Items.Clear();

        }

        /// <summary>
        /// 爬虫图片下载
        /// </summary>
        /// <param name="obj"></param>
        private void SpiderDownload(object obj)
        {
            InputPara para = obj as InputPara;
            sd = new SpiderDownload();
            sd.RootUrl = para.Url;
            sd.SaveDirPath = para.SaveDirPath;
            sd.ImgDownloadFinish += sd_ImgDownloadFinish;
            sd.ImgDownloadAllFinish += sd_ImgDownloadAllFinish;
            sd.Download();
        }

        /// <summary>
        /// 爬虫下载完成回调方法
        /// </summary>
        /// <param name="url"></param>
        /// <param name="imgPath"></param>
        private void sd_ImgDownloadFinish(string url, string imgPath)
        {
            Action listDownloadAdd = () =>
            {
                ListDownload.Items.Add(new ImgDownload { Url = url, ImgPath = imgPath });
                tbProgress.Text = "正在下载中...已下载" + sd.ImgDownloadCount + "张图片";
            };
            Dispatcher.Invoke(listDownloadAdd);
        }

        /// <summary>
        /// 所有图片下载完成事件
        /// </summary>
        private void sd_ImgDownloadAllFinish()
        {
            System.Windows.Forms.MessageBox.Show("下载已完成！");
            Action h = () =>
            {
                Stop();
            };
            Dispatcher.Invoke(h);
        }

        #endregion

        #region 百度下载
        /// <summary>
        /// 百度下载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnBaiduDownLoad_Click(object sender, RoutedEventArgs e)
        {
            if (CheckIsConnected() && CheckBaiduInputPara())
            {
                BeforeBaiduDownload();
                InputPara para = GetInputPara();
                Thread th = new Thread(BaiduDownload);
                th.IsBackground = true;
                th.Start(para);
            }
        }

        /// <summary>
        /// 检查百度输入参数合法性
        /// </summary>
        /// <returns></returns>
        private bool CheckBaiduInputPara()
        {
            if (string.IsNullOrEmpty(txtKeyWord.Text.Trim()))
            {
                System.Windows.MessageBox.Show("请输入关键词！");
                return false;
            }

            if (string.IsNullOrEmpty(txtOutputPath.Text.Trim()))
            {
                System.Windows.MessageBox.Show("请输入保存路径！");
                return false;
            }

            return true;
        }

        /// <summary>
        /// 百度图片下载
        /// </summary>
        /// <param name="obj"></param>
        private void BaiduDownload(object obj)
        {
            InputPara para = obj as InputPara;
            sd = new BaiduDownload();
            sd.SaveDirPath = para.SaveDirPath;
            sd.Key = para.Key;
            sd.ImgDownloadFinish += bis_ImageDownloadFinish;
            sd.ImgDownloadAllFinish += bis_ImageDownloadAllFinish;
            sd.Download();
        }

        /// <summary>
        /// 百度下载完成回调方法
        /// </summary>
        /// <param name="url"></param>
        /// <param name="imgPath"></param>
        private void bis_ImageDownloadFinish(string url, string imgPath)
        {
            Action listDownloadAdd = () =>
            {
                ListDownload.Items.Add(new ImgDownload { Url = url, ImgPath = imgPath });
                tbProgress.Text = "正在下载中...已下载" + sd.ImgDownloadCount + "张图片，共" + sd.ImgTotalCount + "张图片";
            };
            Dispatcher.Invoke(listDownloadAdd);
        }

        /// <summary>
        /// 所有图片下载完成事件
        /// </summary>
        private void bis_ImageDownloadAllFinish()
        {
            System.Windows.Forms.MessageBox.Show("下载已完成！");
            Action h = () =>
            {
                Stop();
            };
            Dispatcher.Invoke(h);
        }

        /// <summary>
        /// 下载前更新控件状态
        /// </summary>
        private void BeforeBaiduDownload()
        {
            btnBaiduDownLoad.IsEnabled = false;
            btnBaiduDownLoad.Content = "下载中...";
            btnStop.IsEnabled = true;
            btnSpiderDownload.IsEnabled = false;

            tbProgress.Text = "正在下载中...";

            ListDownload.Items.Clear();
        }
        #endregion

        #region 停止
        /// <summary>
        /// 停止下载
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnStop_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.MessageBox.Show("下载已停止！");
            Stop();
        }

        /// <summary>
        /// 停止下载
        /// </summary>
        private void Stop()
        {
            if (sd != null)
            {
                sd.Abort();
            }
            btnSpiderDownload.IsEnabled = true;
            btnBaiduDownLoad.IsEnabled = true;
            btnStop.IsEnabled = false;
            btnSpiderDownload.Content = btnSpiderDownload.Tag.ToString();
            btnBaiduDownLoad.Content = btnBaiduDownLoad.Tag.ToString();

            tbProgress.Text = "下载结束，已下载" + sd.ImgDownloadCount + "张图片";
        }

        #endregion

        #region 选择文件夹
        /// <summary>
        /// 选择文件夹
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btnSelectFolderPath_Click(object sender, RoutedEventArgs e)
        {
            FolderBrowserDialog dialog = new FolderBrowserDialog();
            dialog.RootFolder = Environment.SpecialFolder.Desktop;
            dialog.Description = "请选择图片保存的目录";
            DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                this.txtOutputPath.Text = dialog.SelectedPath.Trim();
            }
        }
        #endregion

        #region 双击事件
        /// <summary>
        /// 双击事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ListDownload_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            ImgDownload img = ListDownload.SelectedItem as ImgDownload;
            if (img != null)
            {
                if (!File.Exists(img.ImgPath))
                {
                    System.Windows.Forms.MessageBox.Show("指定的文件不存在，请重新选择！");
                    return;
                }
                Process.Start(img.ImgPath);
            }
        }
        #endregion

        #region 设置按钮
        /// <summary>
        /// 设置按钮
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void BtnConfig_Click(object sender, RoutedEventArgs e)
        {
            ConfigWindow pw = new ConfigWindow()
            {
                WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner,
                Owner = this
            };
            pw.ShowDialog();
        }
        #endregion

        #region 右键菜单

        private void MenuItemCopy_Click(object sender, RoutedEventArgs e)
        {
            if (ListDownload.SelectedIndex == -1)
            {
                return;
            }
            CopyToClipboard(false);
        }

        private void MenuItemCut_Click(object sender, RoutedEventArgs e)
        {
            if (ListDownload.SelectedIndex == -1)
            {
                return;
            }
            CopyToClipboard(true);
        }

        private void CopyToClipboard(bool cut)
        {
            string[] files = GetSelection();
            if (files != null)
            {
                System.Windows.IDataObject data = new System.Windows.DataObject(System.Windows.DataFormats.FileDrop, files);
                MemoryStream memo = new MemoryStream(4);
                byte[] bytes = new byte[] { (byte)(cut ? 2 : 5), 0, 0, 0 };
                memo.Write(bytes, 0, bytes.Length);
                data.SetData("Preferred DropEffect", memo);
                System.Windows.Clipboard.SetDataObject(data, true);
            }
        }

        private string[] GetSelection()
        {
            IList items = ListDownload.SelectedItems;
            ImgDownload img = null;
            List<string> list = new List<string>();
            foreach (var item in items)
            {
                img = item as ImgDownload;
                if (img != null)
                {
                    list.Add(img.ImgPath);
                }
            }
            return list.ToArray();
        }

        private void MenuItemDelete_Click(object sender, RoutedEventArgs e)
        {
            if (ListDownload.SelectedIndex == -1)
            {
                return;
            }

            IList list = ListDownload.SelectedItems;
            ImgDownload img = null;
            foreach (var item in list)
            {
                img = item as ImgDownload;
                if (img != null)
                {
                    File.Delete(img.ImgPath);
                }
            }
        }

        private void MenuItemOpen_Click(object sender, RoutedEventArgs e)
        {
            ImgDownload img = ListDownload.SelectedItem as ImgDownload;
            if (img != null)
            {
                if (!File.Exists(img.ImgPath))
                {
                    System.Windows.Forms.MessageBox.Show("指定的文件不存在，请重新选择！");
                    return;
                }
                Process.Start(img.ImgPath);
            }

        }

        private void MenuItemOpenPath_Click(object sender, RoutedEventArgs e)
        {
            ImgDownload img = ListDownload.SelectedItem as ImgDownload;
            if (img != null)
            {
                string path = System.IO.Path.GetDirectoryName(img.ImgPath);
                if (!Directory.Exists(path))
                {
                    System.Windows.Forms.MessageBox.Show("指定的文件夹不存在，请重新选择！");
                    return;
                }
                Process.Start("Explorer", "/select," + img.ImgPath);
            }
        }
        #endregion

        #region 公用方法
        /// <summary>
        /// 检查网络连接是否正常
        /// </summary>
        /// <returns></returns>
        private bool CheckIsConnected()
        {
            if (CommonHelper.IsConnected())
            {
                return true;
            }
            else
            {
                System.Windows.Forms.MessageBox.Show("请检查网络连接是否正常！");
                return false;
            }
        }

        /// <summary>
        /// 获取输入参数
        /// </summary>
        /// <returns></returns>
        private InputPara GetInputPara()
        {
            InputPara para = InputPara.GetInstance();
            para.Key = txtKeyWord.Text.Trim();
            para.SaveDirPath = txtOutputPath.Text.Trim();
            para.Url = txtUrl.Text.Trim();
            return para;
        }
        #endregion
    }
}
