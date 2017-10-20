using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Zl.AutoUpgrade.Core
{
    public interface IUpgradeService
    {
        /// <summary>
        /// 检测到新版本
        /// </summary>
        event EventHandler<NewVersionDetectedArgs> NewVersionDetected;

        event EventHandler UpgradeStarted;

        event EventHandler<UpgradeProgressArgs> UpgradeProgressChanged;

        event EventHandler<UpgradeEndedArgs> UpgradeEnded;

        /// <summary>
        /// 探测新版本时间间隔(毫秒)
        /// </summary>
        int DetectInterval { get; set; }

        /// <summary>
        /// 是否升级
        /// </summary>
        bool AutoUpgrade { get; set; }

        /// <summary>
        /// 开始自动检测
        /// </summary>
        void StartAutoDetect();

        /// <summary>
        /// 停止自动检测
        /// </summary>
        void StopAutoDetect();

        /// <summary>
        /// 探测是否有新版本
        /// </summary>
        /// <returns></returns>
        bool DetectNewVersion();
        /// <summary>
        /// 尝试立即升级，如果有新版本
        /// </summary>
        bool TryUpgradeNow();

    }

    public class NewVersionDetectedArgs : EventArgs
    {
        /// <summary>
        /// 随后是否需要进行升级
        /// </summary>
        public bool ThenUpgrade { get; set; }
    }

    public class UpgradeProgressArgs : EventArgs
    {
        /// <summary>
        /// 进度百分比
        /// </summary>
        public float ProgressPercent { get; internal set; }

    }
    public class UpgradeEndedArgs : EventArgs
    {
        /// <summary>
        /// 进度百分比
        /// </summary>
        public UpgradeEndedType EndedType { get; }

        /// <summary>
        /// 错误异常, 如果是错误终止有值
        /// </summary>
        public Exception ErrorException { get; }
        /// <summary>
        /// 错误消息, 如果是错误终止有值
        /// </summary>
        public string ErrorMessage { get; }

        public UpgradeEndedArgs(string errorMessage, Exception errorException)
        {
            this.EndedType = UpgradeEndedType.ErrorAborted;
            this.ErrorMessage = errorMessage;
            this.ErrorException = errorException;
        }

        public UpgradeEndedArgs()
        {
            this.EndedType = UpgradeEndedType.Completed;
        }
    }

    public enum UpgradeEndedType
    {
        /// <summary>
        /// 顺利完成
        /// </summary>
        Completed,
        /// <summary>
        /// 错误终止
        /// </summary>
        ErrorAborted
    }
}
