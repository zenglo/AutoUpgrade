using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zl.AutoUpgrade.Core
{
    public class RollbackException : UpgradeException
    {
        public RollbackException(string message, Exception inner) : base(message, inner)
        {
        }
    }
    public class DeleteTempFileException : UpgradeException
    {
        public DeleteTempFileException(string message, Exception inner) : base(message, inner)
        {
        }
    }
    public class ReplaceNewVersionException : UpgradeException
    {
        public ReplaceNewVersionException(string message, Exception inner) : base(message, inner)
        {
        }
    }

    /// <summary>
    /// 非法文件
    /// </summary>
    public class UnlawfulException : UpgradeException
    {
        public UnlawfulException(string message, Exception inner) : base(message, inner)
        {
        }
    }
    public class DownFileException : UpgradeException
    {
        public DownFileException(string message, Exception inner) : base(message, inner)
        {
        }
    }
    public class BackupFileException : UpgradeException
    {
        public BackupFileException(string message, Exception inner) : base(message, inner)
        {
        }
    }
    public class CreateVersionInfoException : UpgradeException
    {
        public CreateVersionInfoException(string message, Exception inner) : base(message, inner)
        {
        }
    }
    public class UpgradeException : Exception
    {
        public UpgradeException(string message, Exception inner) : base(message, inner)
        {
        }
    }
}
