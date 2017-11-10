using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Zl.AutoUpgrade.Core;

namespace Demo.Client
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                var upgradeStatus = AutoUpdater.TryUpgrade(new UpgradeConfig()
                {
                    FtpHost = "127.0.0.1", //ftp server 地址
                    FtpUser = "zl",// ftp 用户名
                    FtpPassword = "temp",// ftp 密码
                    FtpOverTLS = true //是否基于TLS连接
                }, typeof(Demo.Updater.MainWindow).Assembly //升级程序exe位置或所在的程序集
                );
                switch (upgradeStatus)
                {
                    case UpgradeStatus.Started:
                    case UpgradeStatus.Upgrading:
                        //升级程序已启动，待升级程序升级结束后会再次启动当前exe，这里直接先结束当前exe
                        return;
                    case UpgradeStatus.NoNewVersion:
                        MessageBox.Show("没有新版不需要升级，可继续运行了！");
                        break;
                    case UpgradeStatus.Ended:
                        MessageBox.Show("升级过程已结束，可继续运行了！");
                        break;
                    default:
                        break;
                }
            }
            catch (Exception exc)
            {
                MessageBox.Show("启动升级程序时遇到错误，跳过本次升级，继续运行！");
            }
            //继续用户软件代码
        }
    }
}
