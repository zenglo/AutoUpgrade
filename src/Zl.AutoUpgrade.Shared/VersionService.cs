using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Zl.AutoUpgrade.Shared
{

    class VersionService
    {
        private string SecretKey = "Zl.AutoUpgrade.SecretKey";

        /// <summary>
        /// 构造版本服务
        /// </summary>
        /// <param name="secretKey">版本信息秘钥，用语加密版本信息</param>
        public VersionService(string secretKey)
        {
            SecretKey = secretKey;
        }

        /// <summary>
        /// 对比版本包中的文件emd5值是否完全与指定文件夹中对应文件相同，鉴别是否中途遭恶意更改
        /// </summary>
        /// <param name="localFolderPath"></param>
        /// <param name="romotePackageVersionInfo"></param>
        /// <returns></returns>
        public bool Verify(PackageVersionInfo romotePackageVersionInfo, string localFolderPath)
        {
            CspParameters param = new CspParameters();
            param.KeyContainerName = SecretKey;
            DirectoryInfo directoryInfo = new DirectoryInfo(localFolderPath + "/");
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
            {
                using (MD5 md5 = new MD5CryptoServiceProvider())
                {
                    foreach (var item in romotePackageVersionInfo.Files)
                    {
                        string filePath = System.IO.Path.Combine(directoryInfo.FullName, item.File.TrimStart('\\', '/'));
                        FileInfo fileInfo = new FileInfo(filePath);
                        if (File.Exists(filePath))
                        {
                            return false;
                        }
                        string emd5Str = ComputeEmd5(fileInfo, md5, rsa);
                        if (emd5Str != item.Emd5)
                        {
                            return false;
                        }
                    }
                }
            }
            return true;
        }
        /// <summary>
        /// 对比本地文件与远程文件的版本信息，得出需要升级的所有文件版本信息
        /// </summary>
        /// <param name="localFolderPath"></param>
        /// <param name="romotePackageVersionInfo"></param>
        /// <returns></returns>
        public PackageVersionInfo CompareDifference(string localFolderPath, PackageVersionInfo romotePackageVersionInfo)
        {
            long totalLength = 0;
            List<FileVersionInfo> files = new List<FileVersionInfo>();
            CspParameters param = new CspParameters();
            param.KeyContainerName = SecretKey;
            DirectoryInfo directoryInfo = new DirectoryInfo(localFolderPath + "/");
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
            {
                using (MD5 md5 = new MD5CryptoServiceProvider())
                {
                    foreach (var item in romotePackageVersionInfo.Files)
                    {
                        string filePath = System.IO.Path.Combine(directoryInfo.FullName, item.File.TrimStart('\\', '/'));
                        FileInfo fileInfo = new FileInfo(filePath);
                        string emd5Str = ComputeEmd5(fileInfo, md5, rsa);
                        if (File.Exists(filePath))
                        {
                            if (emd5Str == item.Emd5)
                            {
                                continue;
                            }
                        }
                        files.Add(item);
                        totalLength += item.Length;
                    }
                    return new PackageVersionInfo
                    {
                        Files = files.ToArray(),
                        TotalLength = totalLength,
                        PackageDate = romotePackageVersionInfo.PackageDate
                    };
                }
            }
        }

        /// <summary>
        /// 计算本地指定目录的文件版本信息
        /// </summary>
        /// <param name="folderPath"></param>
        /// <param name="ignoreFileNames"></param>
        /// <returns></returns>
        public PackageVersionInfo ComputeVersionInfo(string folderPath, params string[] ignoreFileNames)
        {
            long totalLength = 0;
            List<FileVersionInfo> files = new List<FileVersionInfo>();
            CspParameters param = new CspParameters();
            param.KeyContainerName = SecretKey;
            DirectoryInfo directoryInfo = new DirectoryInfo(folderPath + "/");
            using (RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(param))
            {
                using (MD5 md5 = new MD5CryptoServiceProvider())
                {
                    RecursionFile(folderPath, null, filePath =>
                    {
                        if (ignoreFileNames != null && ignoreFileNames.Any(m => filePath.ToLower().EndsWith(m.ToLower())))
                        {
                            return;
                        }
                        FileInfo fileInfo = new FileInfo(filePath);
                        string emd5Str = ComputeEmd5(fileInfo, md5, rsa);
                        files.Add(new FileVersionInfo
                        {
                            File = fileInfo.FullName.Substring(directoryInfo.FullName.Length - 1),
                            Emd5 = emd5Str,
                            Length = fileInfo.Length
                        });
                        totalLength += fileInfo.Length;
                    });
                    return new PackageVersionInfo
                    {
                        Files = files.ToArray(),
                        TotalLength = totalLength,
                        PackageDate = DateTime.Now
                    };
                }
            }
        }
        private static string ComputeEmd5(FileInfo fileInfo, MD5 md5, RSACryptoServiceProvider dsa)
        {
            using (FileStream fs = fileInfo.OpenRead())
            {
                byte[] md5Bytes = md5.ComputeHash(fs);

                byte[] hash;
                using (SHA1 sha1 = SHA1.Create())
                {
                    hash = sha1.ComputeHash(md5Bytes);
                }
                RSAPKCS1SignatureFormatter DSAFormatter = new RSAPKCS1SignatureFormatter(dsa);
                DSAFormatter.SetHashAlgorithm("SHA1");
                byte[] encryptdata = DSAFormatter.CreateSignature(hash);

                return BytesToHexStr(encryptdata);
            }
        }

        /// <summary>
        /// 递归指定目录，针对每个符合扩展名的文件回调委托处理
        /// </summary>
        /// <param name="directoryString">要递归的目录</param>
        /// <param name="extensions">要求的文件扩展名数组，为空表示不限制扩展名,忽略大小写，需要包含“.”</param>
        /// <param name="doFileCallBack">回调委托</param>
        private static void RecursionFile(string directoryString, string[] extensions, Action<string> doFileCallBack)
        {
            if (extensions != null)
                extensions = extensions.Where(m => m != null).Select(m => m.ToLower()).ToArray();
            foreach (string fileName in Directory.GetFiles(directoryString))
            {
                if (extensions == null || extensions.Contains(Path.GetExtension(fileName).ToLower()))
                    doFileCallBack(fileName);
            }
            foreach (string item in Directory.GetDirectories(directoryString))
                RecursionFile(item, extensions, doFileCallBack);
        }

        private static string BytesToHexStr(byte[] data)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < data.Length; i++)
            {
                sb.Append(data[i].ToString("x2"));
            }
            return sb.ToString();
        }
    }

    [XmlRoot("versionInfo")]
    public class PackageVersionInfo
    {
        [XmlArray("files")]
        [XmlArrayItem("file")]
        public FileVersionInfo[] Files { get; set; }

        [XmlAttribute("totalLength")]
        public long TotalLength { get; set; }

        [XmlAttribute("packageDate")]
        public DateTime PackageDate { get; set; }
    }

    [XmlRoot("versionInfo")]
    public class FileVersionInfo
    {
        [XmlAttribute("file")]
        public string File { get; set; }
        [XmlAttribute("emd5")]
        public string Emd5 { get; set; }
        [XmlAttribute("length")]
        public long Length { get; set; }
    }
}
