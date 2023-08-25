using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.Drawing;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using Genshin_Impact_Start.Properties;

namespace Genshin_Impact_Start
{
    internal class Detector
    {
        Func<Bitmap, long, bool, int> mCallback = null;
        string gamePath = "";
        SoundPlayer mSoundPlayer;
        public Detector(Func<Bitmap, long, bool, int> func) 
        {
            mCallback = func;

            mSoundPlayer = new SoundPlayer(Resources.ResourceManager.GetStream("Shed_a_Light"));

            // 初始化参数
            GetGamePath();

            // 启动检测线程
            Thread thread = new Thread(new ThreadStart(DetectorThread));
            thread.IsBackground = true; // 后台线程：关闭窗口后结束线程
            thread.Start();
        }

        // 定时循环监视屏幕
        private void DetectorThread()
        {
            while (true)
            {
                CheckAll();

                Thread.Sleep(400);
            }
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
            catch (Exception ex) { }

            // 如果截屏失败（常见于锁屏后），返回一个空的bitmap
            Bitmap bitmap2 = new Bitmap(100, 50, PixelFormat.Format24bppRgb);
            using (Graphics gfx = Graphics.FromImage(bitmap2))
                using (SolidBrush brush = new SolidBrush(Color.Black))  // 填充黑色
                {
                    gfx.FillRectangle(brush, 0, 0, bitmap2.Width, bitmap2.Height);
                }
            return bitmap2;
        }

        // 统计颜色个数
        private long CountColorNum(Bitmap bitmap, Color color, int error)
        {
            long count = 0;
            BitmapData bitMapData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, bitmap.PixelFormat);
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

                        if (rCheck && gCheck && bCheck)
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
            Console.WriteLine("PlayAll");
            if (File.Exists(gamePath))
            {
                try
                {
                    bool startSuccess = false;
                    // 启动游戏
                    Process process = new Process();   // params 为 string 类型的参数，多个参数以空格分隔，如果某个参数为空，可以传入””
                    ProcessStartInfo startInfo = new ProcessStartInfo(gamePath, "");
                    process.StartInfo = startInfo;
                    startSuccess = process.Start();
                    // 播放小曲儿
                    if (startSuccess)
                    {
                        //Stream stream = null;
                        //SoundPlayer player = new SoundPlayer("Shed_a_Light.wav");
                        mSoundPlayer.Play();
                    }
                }
                catch (Exception ex) { }
            }
        }

        // 检查游戏是否已经启动
        private bool IsGameRunning()
        {
            try
            {
                int processCount = Process.GetProcessesByName("YuanShen").Length;
                bool isRunning = processCount > 0;
                return isRunning;   // 已经运行了就不要再启动
            }
            catch (Exception ex) { }

            return true;
        }

        // 检查动作调用
        public void CheckAll()
        {
            bool isRunning = IsGameRunning();
            // 截取全屏像素
            Bitmap bitmap = ScreenSnapshot();

            // 计算白色占比
            long colorCount = CountColorNum(bitmap, Color.White, 6);
            long bitmapSize = bitmap.Width * bitmap.Height;
            double ratio = (double)colorCount / bitmapSize;

            // UI更新
            Bitmap copyBitMap = bitmap.Clone(
                new Rectangle(Point.Empty, bitmap.Size),
                bitmap.PixelFormat);
            mCallback(bitmap, colorCount, isRunning);

            // 白色占全屏90%时启动原神
            if (isRunning == false && ratio > 0.9)
                PlayAll();
        }

    }
}
