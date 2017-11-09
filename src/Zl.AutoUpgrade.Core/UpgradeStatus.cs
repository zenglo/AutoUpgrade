using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zl.AutoUpgrade.Core
{
    public enum UpgradeStatus
    {
        /// <summary>
        /// 未检测到新版本
        /// </summary>
        NoNewVersion,
        /// <summary>
        /// 升级开始
        /// </summary>
        Started,
        /// <summary>
        /// 升级结束
        /// </summary>
        Ended,
        /// <summary>
        /// 正在升级
        /// </summary>
        Upgrading
    }
}
