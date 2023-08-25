using Genshin_Impact_Start.Properties;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Net.Mime.MediaTypeNames;

namespace Genshin_Impact_Start
{
    public partial class Form1 : Form
    {
        Detector mDetector;

        public Form1()
        {
            InitializeComponent();

            mDetector = new Detector(ContentUpdate);
        }

        // UI内容更新
        private int ContentUpdate(Bitmap bitmap, long colorCount, bool isRunning)
        {
            // 拷贝图像副本给pictureBox，否则在锁上bitmap处理数据时显示会报错

            // 是否需要委托调用？是否在其他线程？
            if(pictureBox1.InvokeRequired == false)
            {
                pictureBox1.Image = bitmap;

                long bitmapSize = bitmap.Width * bitmap.Height;
                double ratio = (double)colorCount / bitmapSize;

                // 更新数据到文本标签
                lb_TotalPixels.Text = bitmapSize.ToString();
                lb_WhitePixels.Text = colorCount.ToString();
                lb_Ratio.Text = string.Format("{0:N2}%", ratio);
                lb_IsGameRunning.Text = isRunning?"是":"否";
            }
            else
            {
                BeginInvoke(new Action(() => {
                    ContentUpdate(bitmap, colorCount, isRunning);
                }));
            }

            return 0;
        }

        // 手动按键测试
        private void button1_Click(object sender, EventArgs e)
        {
            mDetector.CheckAll();
        }

        private void Form1_SizeChanged(object sender, EventArgs e)
        {
            //当点击最小化按钮时，隐藏软件到任务栏托盘
            if (this.WindowState == FormWindowState.Minimized)
            {
                //将程序从任务栏移除显示
                ShowInTaskbar = false;
                //隐藏窗口
                Visible = false;
                //显示托盘图标
                notifyIcon1.Visible = true;
            }
        }

        // 从最小化托盘还原窗口
        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            //设置程序允许显示在任务栏
            ShowInTaskbar = true;
            //设置窗口可见
            Visible = true;
            //设置窗口状态
            WindowState = FormWindowState.Normal;
            //设置窗口为活动状态，防止被其他窗口遮挡。
            Activate();
        }

    }   // 类结束
}   // 命名空间结束
