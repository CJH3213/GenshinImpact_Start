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
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Genshin_Impact_Start
{
    public partial class Form1 : Form
    {
        string gamePath = "";

        public Form1()
        {
            InitializeComponent();

            // 初始化参数
            GetGamePath();
        }

        // 获取全屏图像
        private Bitmap ScreenSnapshot()
        {
            try
            {
                Screen screen = Screen.AllScreens.FirstOrDefault();//获取当前第一块屏幕(根据需求也可以换其他屏幕)
                //创建需要截取的屏幕区域  (全屏)
                Rectangle rc = screen.Bounds;
                //生成截图的位图容器  
                Bitmap bitmap = new Bitmap(rc.Width, rc.Height, PixelFormat.Format24bppRgb);
                //GDI+图像画布  
                using (Graphics memoryGrahics = Graphics.FromImage(bitmap))
                {
                    memoryGrahics.CopyFromScreen(rc.X, rc.Y, 0, 0, rc.Size, CopyPixelOperation.SourceCopy);//对屏幕指定区域进行图像复制
                }

                return bitmap;
            }
            catch (Exception ex)
            {
                //异常处理
                MessageBox.Show(ex.ToString());
            }

            return null;
        }

        // 统计颜色个数
        private long CountColorNum(Bitmap bitmap, Color color, int error)
        {
            long count = 0;
            BitmapData bitMapData = bitmap.LockBits( new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
            byte[] pixel = new byte[bitmap.Width * bitmap.Height * 4];

            unsafe
            {
                // example assumes 24bpp image.  You need to verify your pixel depth
                // loop by row for better data locality
                for (int y = 0; y < bitMapData.Height; ++y)
                {
                    byte* pRow = (byte*)bitMapData.Scan0 + y * bitMapData.Stride;
                    for (int x = 0; x < bitMapData.Width; ++x)
                    {
                        // windows stores images in BGR pixel order
                        byte r = pRow[2];
                        byte g = pRow[1];
                        byte b = pRow[0];

                        // 计算颜色误差
                        bool rCheck = Math.Abs(r - color.R) < error;
                        bool gCheck = Math.Abs(g - color.G) < error;
                        bool bCheck = Math.Abs(b - color.B) < error;

                        if(rCheck && gCheck && bCheck)
                            count++;

                        // next pixel in the row
                        pRow += 3;
                    }
                }
            }

            bitmap.UnlockBits(bitMapData);
            GC.Collect();

            return count;
        }

        // 播放原神启动小曲
        private void PlayMusic()
        {
            SoundPlayer player = new SoundPlayer("Shed a Light.wav");
            player.Play();
        }

        // 初始化之获取游戏路径
        private void GetGamePath()
        {
            // 自动从注册表获取原神exe路径
            //string exefile = "E:\\Softwares\\Genshin Impact\\Genshin Impact Game\\YuanShen.exe";
            string[] paths = RegistryTool.GetSoftwarePath("原神");  // 此处找到的是启动器的路径
            int pos = paths[0].LastIndexOf('\\');
            string exePath = paths[0].Remove(pos + 1).Insert(pos + 1, "Genshin Impact Game\\YuanShen.exe");

            // 从注册表关闭软件的用户账户控制弹窗
            RegistryTool.CloseUACPop(exePath);

            gamePath = exePath;
        }

        // 启动游戏并播放音乐
        private void PlayAll()
        {
            if (File.Exists(gamePath))
            {
                bool startSuccess = false;
                try
                {

                    Process process = new Process();   // params 为 string 类型的参数，多个参数以空格分隔，如果某个参数为空，可以传入””
                    ProcessStartInfo startInfo = new ProcessStartInfo(gamePath, "");
                    process.StartInfo = startInfo;
                    startSuccess = process.Start();
                    
                }
                catch (Exception ex) { }

                if (startSuccess) PlayMusic();
            }
        }

        // 检查屏幕
        private bool CheckScreen()
        {

            // 截取全屏像素
            Bitmap bitmap = ScreenSnapshot();
            pictureBox1.Image = bitmap;

            // 计算白色占比
            long colorCount = CountColorNum(bitmap, Color.White, 10);
            long bitmapSize = bitmap.Width * bitmap.Height;
            double ratio = (double)colorCount / bitmapSize;

            //Console.WriteLine("总像素个数：{0}，白色像素个数为：{1}，占比{2:N2}%", bitmapSize, colorCount, ratio * 100);

            // 更新数据到文本标签
            lb_TotalPixels.Text = bitmapSize.ToString();
            lb_WhitePixels.Text = colorCount.ToString();
            lb_Ratio.Text = string.Format("{0:N2}%", ratio);

            // 如果全屏80%白色，原神启动
            return ratio > 0.8;
        }

        // 检查动作调用
        private void CheckAll()
        {
            try
            {
                int processCount = Process.GetProcessesByName("YuanShen").Length;
                bool isRunning = processCount > 0;
                lb_IsGameRunning.Text = isRunning ? "是" : "否";
                if (isRunning) return;   // 已经运行了就不要再启动

                // 原神没有启动，检测屏幕是否全白
                bool isAllWhite = CheckScreen();
                if (isAllWhite == false) return;

                // 启动启动
                PlayAll();

            }catch (Exception ex) { }
        }


        // 手动按键测试
        private void button1_Click(object sender, EventArgs e)
        {
            CheckAll();
        }

        // 定时循环监视屏幕
        private void timer1_Tick(object sender, EventArgs e)
        {
            CheckAll();
        }

    }
}
