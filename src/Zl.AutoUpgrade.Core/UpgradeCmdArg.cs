using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Zl.AutoUpgrade.Core
{
    public class UpgradeCmdArg
    {
        public UpgradeConfig Config { get; set; }

        public string ManagedExeFileName { get; set; }

        public string ManagedExeArguments { get; set; }
    }
}
