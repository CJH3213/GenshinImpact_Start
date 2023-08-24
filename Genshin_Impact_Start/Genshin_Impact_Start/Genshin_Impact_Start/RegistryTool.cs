using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Genshin_Impact_Start
{
    // 注册表小工具
    internal class RegistryTool
    {
        // 根据应用名检索所有安装目录
        public static string[] GetSoftwarePath(string name)
        {
            string softName = name;
            List<string> softPaths = new List<string>();
            string softKeyPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\";

            //分别获取32位和64位注册表MACHINE项
            RegistryKey[] machineKeys = new RegistryKey[2];
            machineKeys[0] = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry32);
            machineKeys[1] = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Registry64);

            foreach (RegistryKey machineKey in machineKeys)
            {

                RegistryKey keys1 = machineKey.OpenSubKey(softKeyPath, false);     //获取注册表的软件卸载项

                if (keys1 != null)
                {

                    foreach (string keyName in keys1.GetSubKeyNames())  //遍历卸载项下的子项名（不一定是软件名）
                    {

                        RegistryKey keys2 = keys1.OpenSubKey(keyName, false);   //根据卸载项子项获取对应软件名
                        if (keys2 != null)
                        {
                            string path = keys2.GetValue("DisplayName", "").ToString();
                            if (path.Contains(softName)) //DisplayName匹配软件名
                            {

                                //尝试获取安装路径
                                string installPath = keys2.GetValue("InstallLocation", "").ToString();
                                if (installPath != null && installPath.Length > 0)
                                    softPaths.Add(installPath);
                                else
                                {
                                    //如果没有安装路径则获取图标路径
                                    installPath = keys2.GetValue("DisplayIcon", "").ToString();
                                    softPaths.Add(installPath);
                                }

                            }
                        }
                    }
                }
            }

            machineKeys[0].Close();
            machineKeys[1].Close();

            return softPaths.ToArray<string>();
        }

        // 关闭软件的用户账户控制弹窗
        public static void CloseUACPop(string exePath)
        {
            string tag = "RunAsInvoker";    // 为对应软件的值添加上这个标签
            string softKeyPath = @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\AppCompatFlags\Layers\";

            RegistryKey registryKey = RegistryKey.OpenBaseKey(RegistryHive.CurrentUser, RegistryView.Registry64);
            RegistryKey keys1 = registryKey.OpenSubKey(softKeyPath, true);     //获取注册表的软件标志项

            if (keys1 == null)
                return;

            object value = keys1.GetValue(exePath);
            if (value != null)    // 如果已存在的数据值中没有RAI标签，追加到末尾 
            {
                string oldTag = value.ToString();
                if (!oldTag.ToUpper().Contains(tag.ToUpper()))
                    tag = oldTag + ' ' + tag;
            }
            keys1.SetValue(exePath, tag);

            // 注册表Layers路径下的名称是软件名，值为RunAsInvoker时关闭启动弹窗
            registryKey.Close();
        }
    }
}
