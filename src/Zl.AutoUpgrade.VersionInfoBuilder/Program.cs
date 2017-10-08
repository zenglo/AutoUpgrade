using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Zl.AutoUpgrade.Shared;

namespace Zl.AutoUpgrade.VersionInfoBuilder
{
    class Program
    {
        private const string versionFileName = "versionInfo.xml";
        static void Main(string[] args)
        {
            string targetFolder = string.Empty;
            if (args == null || args.Length == 0)
            {
                targetFolder = AppDomain.CurrentDomain.BaseDirectory;
            }
            else
            {
                targetFolder = new System.IO.DirectoryInfo(args[0]).FullName;
            }
            PackageVersionInfo info = VersionService.ComputeVersionInfo(targetFolder, Path.GetFileName(typeof(Program).Assembly.Location), versionFileName);
            XmlSerializer.SaveToFile(info, System.IO.Path.Combine(targetFolder, versionFileName));
        }
    }
}
