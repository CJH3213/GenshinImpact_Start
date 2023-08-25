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

    }
}
