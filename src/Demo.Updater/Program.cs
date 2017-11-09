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
            //等待小会，避免宿主程序尚未关闭而无法覆盖升级
            Thread.Sleep(1000);
            new MainWindow().ShowDialog();
            AutoUpdater.RunManagedExe();
        }
    }
}
