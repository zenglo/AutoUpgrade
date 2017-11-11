# AutoUpgrade
AutoUpgrade 是一个非常简单、轻量级的自动升级组件，以Ftp Server作为升级服务器，可自动升级托管的用户软件。  
AutoUpgrade 是用C#语言开发，基于 .Net Framework4.0 实现的，支持 Windows xp sp3 及以上版本的Windows系统。  

## 组件组成
AutoUpgrade 组件包含两个子组件。  

子组件 | 对应的.Net项目 | 编译输出 | 说明
---------|----------|---------|---------
升级组件 | Zl.AutoUpgrade.Core | Zl.AutoUpgrade.Core.dll | 升级功能类库，供升级程序调用来完成升级任务。
版本信息生成工具 | Zl.AutoUpgrade.VersionInfoBuilder | VersionInfoBuilder.exe | 命令行工具，用于给升级补丁文件生成可被升级组件识别的补丁版本信息，只有包含该工具生成的版本信息文件的升级补丁包才可被升级组件下载升级。

## 开发说明
要集成AutoUpgrade需要基于`Zl.AutoUpgrade.Core`实现一个升级程序，用户软件需与升级程序相互配合功能完成用户软件升级。  
一般我们需要在用户软件启动时自动检测补丁及尝试升级，我们可以在用户软件启动时先启动升级程序尝试升级，升级程序升级结束后再重新启动用户软件。
### 1. 创建升级程序项目
首先，需要创建一个exe项目作为升级程序项目，该项目通过调用升级组件实现升级控制逻辑，该项目需要引用`Zl.AutoUpgrade.Core`，而不应该依赖任何用户软件项目(用户软件自身功能的相关项目)，以免升级时会因正在运行而无法覆盖更新。  
源码中`Demo.Updater`项目为升级程序示例项目，其中包含了升级进度监听和提示功能。  
如下为比较简单的参考实现：
``` CSharp
[STAThread]
public static void Main(string[] args)
{
    //如果已有升级程序正在运行则退出，避免多个进程同时升级
    if (AutoUpdater.IsUpdaterRunning())
    {
        return;
    }
    //等待小会，避免用户软件尚未关闭而无法覆盖升级
    Thread.Sleep(1000);
    IUpgradeService upgradeService;
    //获取升级服务，该种方式的服务获取仅针对由用户软件内通过AutoUpdater.TryUpgrade启动的升级程序有效
    if (AutoUpdater.TryResolveUpgradeService(out upgradeService))
    {
        //尝试升级
        upgradeService.TryUpgradeNow();
    }
    //尝试升级结束，重新启动托管的用户软件
    AutoUpdater.RunManagedExe();
}
```
### 2. 用户软件启动时自动升级
一般我们会在用户软件启动时进行一次自动升级，在用户软件的入口项目增加`Zl.AutoUpgrade.Core`引用，在入口的其实代码中调用尝试升级。  
源码中`Demo.Client`项目为升级程序示例项目。  
如下为参考实现：
``` CSharp
[STAThread]
public static void Main(string[] args)
{
    try
    {
        var upgradeStatus = AutoUpdater.TryUpgrade(new UpgradeConfig()
        {
            FtpHost = "127.0.0.1", //ftp server 地址
            FtpUser = "zl",// ftp 用户名
            FtpPassword = "temp",// ftp 密码
            FtpOverTLS = true //是否基于TLS连接
        }, typeof(Demo.Updater.MainWindow).Assembly //升级程序exe位置或所在的程序集
        );
        switch (upgradeStatus)
        {
            case UpgradeStatus.Started:
            case UpgradeStatus.Upgrading:
                //升级程序已启动，待升级程序升级结束后会再次启动当前exe，这里直接先结束当前exe
                return;
            case UpgradeStatus.NoNewVersion:
                MessageBox.Show("没有新版不需要升级，可继续运行了！");
                break;
            case UpgradeStatus.Ended:
                MessageBox.Show("升级过程已结束，可继续运行了！");
                break;
            default:
                break;
        }
    }
    catch (Exception exc)
    {
        MessageBox.Show("启动升级程序时遇到错误，跳过本次升级，继续运行！");
    }
    //继续用户软件代码
}
```

## 升级补丁发布说明
升级补丁是只指某一新版本相对上一版本需要更新的或需要增加的程序文件和资源文件的集合。将升级补丁的所有文件放入升级服务器(ftp server)中即可发布。
### 1. 自动升级服务器搭建
自动升级功能将ftp服务器作为自动升级服务器，需搭建一个或使用已有的ftp server用于存储托管软件的升级补丁，作为升级组件的升级补丁包源。  
创建一个只读ftp账号，后续如果有升级补丁，将补丁文件及`VersionInfoBuilder.exe`生成的版本信息文件放置在该ftp账根目录中即完成了补丁发布。

### 2. 生成版本信息文件
升级补丁文件必须通过`VersionInfoBuilder.exe`工具生成对应的版本信息文件后才可被升级组件正常升级。  
`VersionInfoBuilder.exe`是一个命令行工具，最简单生成版本信息文件的方式是直接将VersionInfoBuilder.exe放入升级补丁文件所在文件夹，双击运行VersionInfoBuilder.exe即可自动生成。  
VersionInfoBuilder.exe包含的参数如下：  

参数 | 说明
------- | -------
[/TargetFolder\|/T] | 要生成版本文件的目标文件夹，生成版本文件后的文件夹才可作为升级包供升级
[/SecretKey\|/S] | 版本信息秘钥，客户端只有与本生成器使用的秘钥一致时才可正常升级本生成器生成的版本升级包
[/Ignore\|/I] | 目标文件夹中要忽略的文件，多个用空格间隔
[/Help\|/H] | 查看帮助

也可在命令行中运行`VersionInfoBuilder.exe /H`查看参数帮助。  
**注意：不应该将`Zl.AutoUpgrade.Core.dll`和自建的升级程序项目作为升级补丁的一部分，否则会因这两个dll被升级过程运行时依赖而导致总是无法被覆盖升级，从而导致升级失败。**

## Here's the AD
<a target='_blank' rel='nofollow' href='https://app.codesponsor.io/link/q6NFtNujicHJPWrvRTPNrD5i/zenglo/AutoUpgrade'>
  <img alt='Sponsor' width='888' height='68' src='https://app.codesponsor.io/embed/q6NFtNujicHJPWrvRTPNrD5i/zenglo/AutoUpgrade.svg' />
</a>
