using FluentFTP;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Zl.AutoUpgrade.Shared;

namespace Zl.AutoUpgrade.Core
{
    public class UpgradeService : IUpgradeService
    {
        private string _targetFolder;
        private string _ftpServerIp;
        private string _uid;
        private string _pwd;
        private const string VersionFileName = "versionInfo.xml";
        private static readonly string NewVersionTempFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "newVersionTemp");
        private static readonly string CurVersionBakFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "curVersionBak");

        public int DetectInterval { get; set; } = 1 * 60 * 1000;
        public bool AutoUpgrade { get; set; } = true;

        public event EventHandler<NewVersionDetectedArgs> NewVersionDetected;
        public event EventHandler UpgradeStarted;
        public event EventHandler<UpgradeProgressArgs> UpgradeProgressChanged;
        public event EventHandler<UpgradeEndedArgs> UpgradeEnded;


        public UpgradeService(string targetFolder, string ftpServerIp, string uid, string pwd)
        {
            this._targetFolder = targetFolder;
            this._ftpServerIp = ftpServerIp;
            this._uid = uid;
            this._pwd = pwd;
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
                PackageVersionInfo needUpdateVersionInfo = GetNeedVersionInfo(client);
                return needUpdateVersionInfo != null && needUpdateVersionInfo.Files.Length > 0;
            }
        }

        public bool TryUpgradeNow()
        {
            using (FtpClient client = this.CreateFtpClient())
            {
                PackageVersionInfo needUpdateVersionInfo = GetNeedVersionInfo(client);
                if (needUpdateVersionInfo == null || needUpdateVersionInfo.Files.Length == 0)
                {
                    return false;
                }
                UpgradeNow(client, needUpdateVersionInfo);
                return true;
            }
        }

        private void UpgradeNow(FtpClient client, PackageVersionInfo needUpdateVersionInfo)
        {
            this.RaiseUpgradeStarted();
            float percent = 0f;
            this.RaiseUpgradeProgress(percent += 0.01f);
            DirectoryInfo newVersionTemp = Directory.CreateDirectory(NewVersionTempFolder);
            //下载新版本，占比 90%
            string curentDownFile = string.Empty;
            try
            {
                client.RetryAttempts = 3;
                long downLength = 0;
                foreach (var item in needUpdateVersionInfo.Files)
                {
                    curentDownFile = item.File;
                    client.DownloadFile(Path.Combine(newVersionTemp.FullName, item.File.TrimStart('\\', '/')), item.File.Replace('\\', '/'), true, FtpVerify.Retry);
                    downLength += item.Length;
                    this.RaiseUpgradeProgress(percent += (float)Math.Round((downLength * 1.0 / needUpdateVersionInfo.TotalLength * 0.9), 2));
                }
            }
            catch (Exception exc)
            {
                string msg = string.Format("下载新版文件({0})出错", curentDownFile);
                Exception nexc = new DownFileException(msg, exc);
                this.RaiseUpgradeEnded(msg, nexc);
                throw nexc;
            }
            //备份当前版本，占比 4%
            try
            {
                DirectoryInfo bakFolder = new DirectoryInfo(CurVersionBakFolder);
                foreach (var item in needUpdateVersionInfo.Files)
                {
                    string filePath = Path.Combine(this._targetFolder, item.File.TrimStart('\\', '/'));
                    if (!File.Exists(filePath))
                    {
                        continue;
                    }
                    string newFilePath = Path.Combine(bakFolder.FullName, item.File.TrimStart('\\', '/'));
                    Directory.CreateDirectory(Directory.GetParent(newFilePath).FullName);
                    File.Copy(filePath, newFilePath, true);
                }
                this.RaiseUpgradeProgress(percent += 0.04f);
            }
            catch (Exception exc)
            {
                string msg = "版本当前版本出错";
                Exception nexc = new BakFileException(msg, exc);
                this.RaiseUpgradeEnded(msg, nexc);
                throw nexc;
            }
            //新版覆盖当前版，占比 4%
            try
            {
                CopyDirectory(NewVersionTempFolder, this._targetFolder);
                XmlSerializer.SaveToFile(needUpdateVersionInfo, Path.Combine(this._targetFolder, VersionFileName));
                this.RaiseUpgradeProgress(percent += 0.04f);
            }
            catch (Exception exc)
            {
                try
                {
                    this.Rollback();
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
                //Directory.Delete(NewVersionTempFolder, true);
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


        private FtpClient CreateFtpClient()
        {
            FtpClient client = new FtpClient(_ftpServerIp);
            client.Credentials = new NetworkCredential(_uid, _pwd);
            return client;
        }

        private PackageVersionInfo GetNeedVersionInfo(FtpClient client)
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
            return VersionService.CompareDifference(_targetFolder, pvi);
        }



        private void Rollback()
        {
            CopyDirectorySkipError(CurVersionBakFolder, this._targetFolder);
        }

        private static void CopyDirectorySkipError(String sourcePath, String destinationPath)
        {
            DirectoryInfo info = new DirectoryInfo(sourcePath);
            Directory.CreateDirectory(destinationPath);
            foreach (FileSystemInfo fsi in info.GetFileSystemInfos())
            {
                String destName = Path.Combine(destinationPath, fsi.Name);
                if (fsi is System.IO.FileInfo)
                    try
                    {
                        File.Copy(fsi.FullName, destName);
                    }
                    catch (Exception)
                    {
                    }
                else
                {
                    Directory.CreateDirectory(destName);
                    CopyDirectorySkipError(fsi.FullName, destName);
                }
            }
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
