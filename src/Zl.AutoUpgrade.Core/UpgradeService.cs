using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Zl.AutoUpgrade.Shared;
using System.Security.Authentication;

namespace Zl.AutoUpgrade.Core
{
    public class UpgradeService : IUpgradeService
    {
        private string _targetFolder;
        private string _ftpServerIp;
        private string _ftpUser;
        private string _ftpPassword;
        private bool _ftpOverTLS;
        private VersionService _versionService;
        private const string VersionFileName = "versionInfo.xml";
        private static readonly string NewVersionTempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "newVersionTemp");
        private static readonly string LastVersionBakFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "lastVersionBak");

        public int DetectInterval { get; set; } = 1 * 60 * 1000;
        public bool AutoUpgrade { get; set; } = true;

        public event EventHandler<NewVersionDetectedArgs> NewVersionDetected;
        public event EventHandler UpgradeStarted;
        public event EventHandler<UpgradeProgressArgs> UpgradeProgressChanged;
        public event EventHandler<UpgradeEndedArgs> UpgradeEnded;


        public UpgradeService(UpgradeConfig config)
        {
            this._targetFolder = config.TargetFolder;
            this._ftpServerIp = config.FtpHost;
            this._ftpUser = config.FtpUser;
            this._ftpPassword = config.FtpPassword;
            this._ftpOverTLS = config.FtpOverTLS;
            _versionService = new VersionService(config.VersionInfoSecretKey ?? "Zl.AutoUpgrade.SecretKey");
        }

        public void StartAutoDetect()
        {
            throw new NotImplementedException();
        }

        public void StopAutoDetect()
        {
            throw new NotImplementedException();
        }

        public bool DetectNewVersion()
        {
            using (FtpClient client = this.CreateFtpClient())
            {
                PackageVersionInfo diffVersionInfo = GetDifferenceVersionInfoWithServer(client);
                return diffVersionInfo != null && diffVersionInfo.Files.Length > 0;
            }
        }

        public bool TryUpgradeNow()
        {
            using (FtpClient client = this.CreateFtpClient())
            {
                PackageVersionInfo diffVersionInfo = GetDifferenceVersionInfoWithServer(client);
                if (diffVersionInfo == null || diffVersionInfo.Files.Length == 0)
                {
                    return false;
                }
                UpgradeNow(client, diffVersionInfo);
                return true;
            }
        }

        private void UpgradeNow(FtpClient client, PackageVersionInfo diffVersionInfo)
        {
            this.RaiseUpgradeStarted();
            float percent = 0f;
            try
            {
                Directory.CreateDirectory(_targetFolder);
                var currVersionInfo = this._versionService.ComputeVersionInfo(_targetFolder);
                XmlSerializer.SaveToFile(diffVersionInfo, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "versionInfo_diff.xml"));
                XmlSerializer.SaveToFile(currVersionInfo, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "versionInfo_curr.xml"));
                this.RaiseUpgradeProgress(percent += 0.01f);
            }
            catch (Exception exc)
            {
                string msg = "计算版本信息文件出错";
                Exception nexc = new CreateVersionInfoException(msg, exc);
                this.RaiseUpgradeEnded(msg, nexc);
                throw nexc;
            }
            //下载新版本，占比 90%
            string curentDownFile = string.Empty;
            try
            {
                DirectoryInfo newVersionTemp = Directory.CreateDirectory(NewVersionTempFolder);
                client.RetryAttempts = 3;
                long downLength = 0;
                foreach (var item in diffVersionInfo.Files)
                {
                    curentDownFile = item.File;
                    client.DownloadFile(Path.Combine(newVersionTemp.FullName, item.File.TrimStart('\\', '/')), item.File.Replace('\\', '/'), true, FtpVerify.Retry);
                    downLength += item.Length;
                    this.RaiseUpgradeProgress(percent += (float)Math.Round((downLength * 1.0 / diffVersionInfo.TotalLength * 0.9), 2));
                }
                XmlSerializer.SaveToFile(diffVersionInfo, Path.Combine(newVersionTemp.FullName, VersionFileName));
            }
            catch (Exception exc)
            {
                string msg = string.Format("下载新版文件({0})出错", curentDownFile);
                Exception nexc = new DownFileException(msg, exc);
                this.RaiseUpgradeEnded(msg, nexc);
                throw nexc;
            }
            //验证文件合法性，防篡改，占比 1%
            try
            {
                if (!this._versionService.Verify(diffVersionInfo, NewVersionTempFolder))
                {
                    string msg = "新版文件不合法";
                    Exception nexc = new UnlawfulException(msg, null);
                    this.RaiseUpgradeEnded(msg, nexc);
                    throw nexc;
                }
                this.RaiseUpgradeProgress(percent += 0.01f);
            }
            catch (UnlawfulException)
            {
                throw;
            }
            catch (Exception exc)
            {
                string msg = "新版文件验证出错";
                Exception nexc = new UnlawfulException(msg, exc);
                this.RaiseUpgradeEnded(msg, nexc);
                throw nexc;
            }
            //备份当前版本，占比 3%
            try
            {
                CopyVersionFile(diffVersionInfo, this._targetFolder, LastVersionBakFolder);
                this.RaiseUpgradeProgress(percent += 0.03f);
            }
            catch (Exception exc)
            {
                string msg = "版本当前版本出错";
                Exception nexc = new BackupFileException(msg, exc);
                this.RaiseUpgradeEnded(msg, nexc);
                throw nexc;
            }
            //新版覆盖当前版，占比 4%
            try
            {
                CopyVersionFile(diffVersionInfo, NewVersionTempFolder, this._targetFolder);
                XmlSerializer.SaveToFile(diffVersionInfo, Path.Combine(this._targetFolder, VersionFileName));
                this.RaiseUpgradeProgress(percent += 0.04f);
            }
            catch (Exception exc)
            {
                try
                {
                    CopyVersionFile(diffVersionInfo, LastVersionBakFolder, this._targetFolder);
                }
                catch (Exception exc1)
                {
                    string msg1 = string.Format("新版覆盖当前版出错后回滚出错", curentDownFile);
                    Exception nexc1 = new RollbackException(msg1, exc1);
                    LogHelper.Log(nexc1, "升级忽略错误");
                }
                string msg = string.Format("新版覆盖当前版出错", curentDownFile);
                Exception nexc = new ReplaceNewVersionException(msg, exc);
                this.RaiseUpgradeEnded(msg, nexc);
                throw nexc;
            }
            //删除临时文件，占比 1%
            try
            {
                Directory.Delete(NewVersionTempFolder, true);
                Directory.Delete(LastVersionBakFolder, true);
                this.RaiseUpgradeProgress(percent += 0.01f);
            }
            catch (Exception exc)
            {
                string msg = string.Format("删除临时文件出错", curentDownFile);
                Exception nexc = new DeleteTempFileException(msg, exc);
                LogHelper.Log(nexc, "升级忽略错误");
            }
            this.RaiseUpgradeProgress(percent = 1f);
            this.RaiseUpgradeEnded();
        }

        private static void CopyVersionFile(PackageVersionInfo versionInfo, string sourceFolder, string toFolder)
        {
            foreach (var item in versionInfo.Files)
            {
                string filePath = Path.Combine(sourceFolder, item.File.TrimStart('\\', '/'));
                if (!File.Exists(filePath))
                {
                    continue;
                }
                string newFilePath = Path.Combine(toFolder, item.File.TrimStart('\\', '/'));
                Directory.CreateDirectory(Directory.GetParent(newFilePath).FullName);
                File.Copy(filePath, newFilePath, true);
            }
        }

        private FtpClient CreateFtpClient()
        {
            FtpClient client = new FtpClient(_ftpServerIp, _ftpUser, _ftpPassword);
            client.DataConnectionType = FtpDataConnectionType.AutoActive;
            if (_ftpOverTLS)
            {
                client.EncryptionMode = FtpEncryptionMode.Explicit;
                client.SslProtocols = SslProtocols.Tls;
                client.ValidateCertificate += new FtpSslValidation(OnValidateCertificate);
            }
            return client;
        }
        private void OnValidateCertificate(FtpClient control, FtpSslValidationEventArgs e)
        {
            // add logic to test if certificate is valid here
            e.Accept = true;
        }

        private PackageVersionInfo GetDifferenceVersionInfoWithServer(FtpClient client)
        {
            if (!client.FileExists("/" + VersionFileName))
            {
                return null;
            }
            byte[] versionData = null;
            if (!client.Download(out versionData, "/" + VersionFileName))
            {
                return null;
            }
            PackageVersionInfo pvi = XmlSerializer.ToObject<PackageVersionInfo>(versionData);
            return this._versionService.CompareDifference(_targetFolder, pvi);
        }

        /// <summary>
        /// 复制文件夹（及文件夹下所有子文件夹和文件） 
        /// </summary>
        /// <param name="sourcePath">待复制的文件夹路径</param>
        /// <param name="destinationPath">目标路径</param>
        private static void CopyDirectory(String sourcePath, String destinationPath, params DirectoryInfo[] withoutFolders)
        {
            DirectoryInfo info = new DirectoryInfo(sourcePath);
            Directory.CreateDirectory(destinationPath);
            foreach (FileSystemInfo fsi in info.GetFileSystemInfos())
            {
                String destName = Path.Combine(destinationPath, fsi.Name);

                if (fsi is System.IO.FileInfo)
                    File.Copy(fsi.FullName, destName, true);
                else
                {
                    if (withoutFolders != null && withoutFolders.Any(m => m.FullName.ToUpper().TrimEnd('/', '\\')
                           == fsi.FullName.ToUpper().TrimEnd('/', '\\')))
                    {
                        break;
                    }
                    Directory.CreateDirectory(destName);
                    CopyDirectory(fsi.FullName, destName);
                }
            }
        }

        private void RaiseUpgradeStarted()
        {
            EventHandler handler = this.UpgradeStarted;
            if (handler != null)
            {
                handler.BeginInvoke(this, EventArgs.Empty, null, null);
            }
        }
        private void RaiseUpgradeEnded()
        {
            EventHandler<UpgradeEndedArgs> handler = this.UpgradeEnded;
            if (handler != null)
            {
                handler.BeginInvoke(this, new UpgradeEndedArgs(), null, null);
            }
        }
        private void RaiseUpgradeEnded(string errorMessage, Exception errorException)
        {
            EventHandler<UpgradeEndedArgs> handler = this.UpgradeEnded;
            if (handler != null)
            {
                handler.BeginInvoke(this, new UpgradeEndedArgs(errorMessage, errorException), null, null);
            }
        }
        private void RaiseUpgradeProgress(float percent)
        {
            EventHandler<UpgradeProgressArgs> handler = this.UpgradeProgressChanged;
            if (handler != null)
            {
                handler.BeginInvoke(this, new UpgradeProgressArgs() { ProgressPercent = percent }, null, null);
            }
        }
    }
}
