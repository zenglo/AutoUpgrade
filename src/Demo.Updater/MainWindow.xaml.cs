using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Zl.AutoUpgrade.Core;

namespace Demo.Updater
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        private Thread _updateThread;
        private IUpgradeService _upgradeService;
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!AutoUpdater.TryResolveUpgradeService(out _upgradeService))
            {
                this.Close();
            }
            _updateThread = new Thread(this.DoUpdate);
            _updateThread.Start();
        }

        private void DoUpdate()
        {
            if (_upgradeService.DetectNewVersion())
            {
                _upgradeService.UpgradeStarted += UpgradeService_UpgradeStarted;
                _upgradeService.UpgradeProgressChanged += UpgradeService_UpgradeProgressChanged;
                _upgradeService.UpgradeEnded += UpgradeService_UpgradeEnded;
                _upgradeService.TryUpgradeNow();
                _upgradeService.UpgradeStarted -= UpgradeService_UpgradeStarted;
                _upgradeService.UpgradeProgressChanged -= UpgradeService_UpgradeProgressChanged;
                _upgradeService.UpgradeEnded -= UpgradeService_UpgradeEnded;
            }
        }
        private void UpgradeService_UpgradeStarted(object sender, EventArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.txtlab.Text = "检测到新版本，正在升级...";
            }));
        }

        private void UpgradeService_UpgradeProgressChanged(object sender, UpgradeProgressArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                this.probar.Value = e.ProgressPercent;
                this.txtlab.Text = string.Format("正在升级({0:P0})...", e.ProgressPercent);
            }));
        }

        private void UpgradeService_UpgradeEnded(object sender, UpgradeEndedArgs e)
        {
            this.Dispatcher.BeginInvoke(new Action(() =>
            {
                if (e.EndedType == UpgradeEndedType.Completed)
                {
                    this.txtlab.Text = "升级成功";
                    this.DialogResult = true;
                }
                else if (e.ErrorException is ThreadAbortException)
                {
                    this.txtlab.Text = "已取消升级";
                }
                else
                {
                    this.txtlab.Text = "升级中遇到错误";
                    MessageBox.Show(this, e.ErrorMessage, "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    this.DialogResult = true;
                }
            }));
        }


        private void Button_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            if (this.DialogResult != true)
            {
                //取消升级
                if (_updateThread != null)
                    _updateThread.Abort();
            }
        }
    }
}
