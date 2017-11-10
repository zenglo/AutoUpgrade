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

namespace Demo.Updater
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (AutoUpdater.IsUpdaterRunning())
            {
                return;
            }
            //等待小会，避免用户软件尚未关闭而无法覆盖升级
            Thread.Sleep(1000);
            new MainWindow().ShowDialog();
            AutoUpdater.RunManagedExe();
        }

        //[STAThread]
        //public static void Main(string[] args)
        //{
        //    //如果已有升级程序正在运行则退出，避免多个进程同时升级
        //    if (AutoUpdater.IsUpdaterRunning())
        //    {
        //        return;
        //    }
        //    //等待小会，避免用户软件尚未关闭而无法覆盖升级
        //    Thread.Sleep(1000);
        //    IUpgradeService upgradeService;
        //    //获取升级服务，该种方式的服务获取仅针对由用户软件内通过AutoUpdater.TryUpgrade启动的升级程序有效
        //    if (AutoUpdater.TryResolveUpgradeService(out upgradeService))
        //    {
        //        //尝试升级
        //        upgradeService.TryUpgradeNow();
        //    }
        //    //尝试升级结束，重新启动托管的用户软件
        //    AutoUpdater.RunManagedExe();
        //}
    }
}
