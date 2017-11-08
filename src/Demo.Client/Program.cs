using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Zl.AutoUpgrade.Core;

namespace Demo.Client
{
    class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            if (AutoUpdater.TryUpgrade(new UpgradeConfig()
            {
                FtpHost = "127.0.0.1",
                FtpUser = "zl",
                FtpPassword = "temp",
                FtpOverTLS = true
            }, typeof(AutoUpdater).Assembly))
            {
                return;
            }
            var app = new App();
            app.InitializeComponent();
            app.Run();
        }
    }
}
