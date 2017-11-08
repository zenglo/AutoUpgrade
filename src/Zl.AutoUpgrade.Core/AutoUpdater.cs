using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Zl.AutoUpgrade.Shared;

namespace Zl.AutoUpgrade.Core
{
    public static class AutoUpdater
    {
        private static UpgradeCmdArg _upgradeCmdArg;

        /// <summary>
        /// Updater是否正在运行
        /// </summary>
        /// <returns></returns>
        public static bool IsUpdaterRunning()
        {
            bool updating = false;
            Mutex appMutex = new Mutex(true, "Zl.AutoUpgrade.Core.AutoUpdater", out updating);
            if (updating)
            {
                return true;
            }
            return false;
        }

        /// <summary>
        /// 由托管exe调用，尝试升级。如果返回true，托管exe应该退出程序，否则会因托管exe正在运行而无法正常升级，升级成功后Updater有义务调用RunManagedExe()再重新启动托管exe。
        /// </summary>
        /// <param name="upgradeConfig">升级配置</param>
        /// <param name="updaterExeFileName">自定义的Updater程序集</param>
        /// <returns>是否需要升级且已启动升级</returns>
        public static bool TryUpgrade(UpgradeConfig upgradeConfig, Assembly updaterExeAssembly)
        {
            return TryUpgrade(upgradeConfig, updaterExeAssembly.Location);
        }

        /// <summary>
        /// 由托管exe调用，尝试升级。如果返回true，托管exe应该退出程序，否则会因托管exe正在运行而无法正常升级，升级成功后Updater有义务调用RunManagedExe()再重新启动托管exe。
        /// </summary>
        /// <param name="upgradeConfig">升级配置</param>
        /// <param name="updaterExeFileName">自定义的Updater Exe文件路径</param>
        /// <returns>是否需要升级且已启动升级</returns>
        public static bool TryUpgrade(UpgradeConfig upgradeConfig, string updaterExeFileName)
        {
            if (IsUpdaterRunning())
            {
                return true;
            }
            FileInfo fileInfo = new FileInfo(updaterExeFileName);
            if (!fileInfo.Exists)
            {
                throw new Exception("Can't found the updater exe");
            }
            IUpgradeService upgradeService = new UpgradeService(upgradeConfig);
            if (upgradeService.DetectNewVersion())
            {
                if (string.IsNullOrEmpty(upgradeConfig.TargetFolder))
                {
                    upgradeConfig.TargetFolder = AppDomain.CurrentDomain.BaseDirectory;
                }
                else
                {
                    upgradeConfig.TargetFolder = new DirectoryInfo(upgradeConfig.TargetFolder).FullName;
                }
                UpgradeCmdArg arg = new UpgradeCmdArg()
                {
                    Config = upgradeConfig,
                    ManagedExeFileName = System.Reflection.Assembly.GetEntryAssembly().Location,
                    ManagedExeArguments = string.Join(" ", Environment.GetCommandLineArgs() ?? new string[0])
                };
                System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
                startInfo.FileName = fileInfo.FullName;
                startInfo.Arguments = "\"" + Convert.ToBase64String(XmlSerializer.ToBinary(arg)) + "\"";
                System.Diagnostics.Process.Start(startInfo);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 由Updater升级前调用，尝试创建升级服务对象，如果不能从启动程序中获取到启动配置参数则创建失败。
        /// </summary>
        /// <param name="upgradeService"></param>
        /// <returns>是否成功创建了升级服务</returns>
        public static bool TryResolveUpgradeService(out IUpgradeService upgradeService)
        {
            upgradeService = null;
            string[] runArgs = Environment.GetCommandLineArgs();
            if (runArgs == null || runArgs.Length == 0)
                return false;
            try
            {
                byte[] argData = Convert.FromBase64String(runArgs[0]);
                _upgradeCmdArg = XmlSerializer.ToObject<UpgradeCmdArg>(argData);
                upgradeService = new UpgradeService(_upgradeCmdArg.Config);
                return true;
            }
            catch (Exception exc)
            {
                return false;
            }
        }

        /// <summary>
        /// 由Updater升级完毕后调用，运行托管exe
        /// </summary>
        public static void RunManagedExe()
        {
            System.Diagnostics.ProcessStartInfo startInfo = new System.Diagnostics.ProcessStartInfo();
            startInfo.FileName = _upgradeCmdArg.ManagedExeFileName;
            startInfo.Arguments = _upgradeCmdArg.ManagedExeArguments;
            System.Diagnostics.Process.Start(startInfo);
        }
    }
}
