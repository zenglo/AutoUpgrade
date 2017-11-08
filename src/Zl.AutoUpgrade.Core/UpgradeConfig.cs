using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Authentication;
using System.Text;

namespace Zl.AutoUpgrade.Core
{
    public class UpgradeConfig
    {
        /// <summary>
        /// 要升级的目标目录，缺省程序所在目录
        /// </summary>
        public string TargetFolder { get; set; }
        /// <summary>
        /// 升级服务器ftp主机域名或ip
        /// </summary>
        public string FtpHost { get; set; }
        /// <summary>
        /// 升级服务器ftp用户名
        /// </summary>
        public string FtpUser { get; set; }
        /// <summary>
        /// 升级服务器ftp密码
        /// </summary>
        public string FtpPassword { get; set; }
        /// <summary>
        /// 是否使用TLS协议连接ftp
        /// </summary>
        public bool FtpOverTLS { get; set; }
        /// <summary>
        /// 当检测到新版后是否自动升级
        /// </summary>
        public bool AutoUpgrade { get; set; }

        /// <summary>
        /// 版本信息秘钥，只有与生成升级版本信息的生成器使用的秘钥一致时才可生成，缺省为"Zl.AutoUpgrade.SecretKey"
        /// </summary>
        public string VersionInfoSecretKey { get; set; }
    }
}
