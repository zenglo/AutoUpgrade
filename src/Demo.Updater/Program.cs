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
            if (new MainWindow().ShowDialog() != true)
            {
                //取消升级

            }
            //var app = new Application();
            //app.ShutdownMode = ShutdownMode.OnMainWindowClose;
            //app.Run();
        }
    }
}
