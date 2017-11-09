using Args.Help.Formatters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Zl.AutoUpgrade.Shared;

namespace Zl.AutoUpgrade.VersionInfoBuilder
{
    class Program
    {
        private const string versionFileName = "versionInfo.xml";
        static void Main(string[] args)
        {
            try
            {
                var definition = Args.Configuration.Configure<CommandObject>();
                var command = definition.CreateAndBind(args);
                if (command.Help != null)
                {
                    var help = new Args.Help.HelpProvider().GenerateModelHelp(definition);
                    var f = new ConsoleHelpFormatter(80, 1, 5);
                    Console.WriteLine(f.GetHelp(help));
                    return;
                }
                Console.WriteLine($"正在生成...");
                Console.WriteLine($"目标文件夹：{command.TargetFolder}");
                Console.WriteLine($"秘钥：{command.SecretKey}");
                Console.WriteLine($"忽略文件：{ ((command.Ignore == null || command.Ignore.Count == 0) ? "无" : string.Join(", ", command.Ignore))}");
                VersionService versionService = new VersionService(command.SecretKey);
                if (command.Ignore == null)
                    command.Ignore = new List<string>(2);
                command.Ignore.Add(Path.GetFileNameWithoutExtension(typeof(Program).Assembly.Location));
                command.Ignore.Add(versionFileName);
                command.Ignore.Add("Zl.AutoUpgrade.Core");
                PackageVersionInfo info = versionService.ComputeVersionInfo(command.TargetFolder,
                  command.Ignore.ToArray());
                XmlSerializer.SaveToFile(info, System.IO.Path.Combine(command.TargetFolder, versionFileName));
                Console.WriteLine($"生成完毕.");
            }
            catch (Exception exc)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(exc);
                Console.ResetColor();
            }
        }

        [System.ComponentModel.Description("升级包版本文件生成器工具.")]
        class CommandObject
        {
            [Description("要生成版本文件的目标文件夹，生成版本文件后的文件夹才可作为升级包供升级")]
            public string TargetFolder { get; set; } = AppDomain.CurrentDomain.BaseDirectory;
            [Description("版本信息秘钥，客户端只有与本生成器使用的秘钥一致时才可正常升级本生成器生成的版本升级包")]
            public string SecretKey { get; set; } = "Zl.AutoUpgrade.SecretKey";

            [Description("目标文件夹中要忽略的文件，多个用空格间隔")]
            public List<string> Ignore { get; set; }

            [Description("查看帮助")]
            public string Help { get; set; }
        }
    }
}
