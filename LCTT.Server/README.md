# 使用说明

默认阁下是 Linux 玩家。该项目在 Windows 与 macOS 上均可运行。

> 注意：该项目需要 x86_64 架构的系统。macOS 用户可尝试使用 Rosetta 转译模式下的终端进行操作。

## 准备工作

0. [创建 GitHub 个人访问令牌](https://docs.github.com/zh/authentication/keeping-your-account-and-data-secure/managing-your-personal-access-tokens)
1. [在 Linux 上安装 .NET](https://learn.microsoft.com/zh-cn/dotnet/core/install/linux)
2. [在 Linux 上安装 PowerShell](https://learn.microsoft.com/zh-cn/powershell/scripting/install/installing-powershell-on-linux?view=powershell-7.3)
3. [安装 Visual Studio Code](https://code.visualstudio.com/Download)
4. 为 VSCode 安装插件 [.NET Extension Pack](https://marketplace.visualstudio.com/items?itemName=ms-dotnettools.vscode-dotnet-pack) 和 [YAML](https://marketplace.visualstudio.com/items?itemName=redhat.vscode-yaml)

## 初始化

0. 修改 [`LCTT.Server/Scripts/lci.ps1`](LCTT.Server/Scripts/lci.ps1) 中的 `$TranslateProject`
1. 搜索替换项目中所有的 `GITHUB_ID` 和 `GITHUB_PERSONAL_ACCESS_TOKEN` 为实际值
2. 切换到 [`LCTT.Server/LCTT.Server`](LCTT.Server/LCTT.Server) 目录，运行 `dotnet tool restore`
3. 在 PowerShell 环境下运行 `./Scripts/lci.ps1` 以初始化客户端脚本

## 日常使用

0. 使用 VSCode 打开 [`LCTT.Server/LCTT.Server`](LCTT.Server/LCTT.Server) 目录，按 <kbd>F5</kdb> 启动调试
1. 在 PowerShell 环境中使用 [`Scripts`](LCTT.Server/LCTT.Server/Scripts) 中的客户端脚本

## 脚本介绍

按照字母排序。

### lcb.ps1

清空仓库分支，仅保留 `main` 分支。

```
.\lcb.ps1 clean
```

### lcc.ps1

提供文章的 难易程度、类别、URL 和 是否编辑（可选），生成 MD 格式文章上传 GitHub 并发起 PR。

| 参数 | 值 1 | 值 2 | 值 3 |
| :- | :- | :- | :- |
| 难度 | easy | medium | hard |
| 类别 | news | talk | tech |

URL 即为原文的 URL，通常从 `lcf.ps1` 脚本的输出中获得。

是否编辑：在 URL 后输入任意非空字符，程序会在生成 MD 格式文章后将其在 VSCode 中打开以供编辑，此时程序暂停执行。编辑完毕后关闭该文件，程序继续执行。

### lcf.ps1

获取网站最新订阅列表。

可接受参数以获取某个范围日期内的订阅，也可直接运行获得参数输入提示。

示例：

```
# 快捷获取今天的订阅列表
> lcf.ps1 -0

# 快捷获取昨天之后的订阅列表
> lcf.ps1 -1

# 快捷获取最近一周的订阅列表
> lcf.ps1 -7

# 指定日期范围
> lcf.ps1
Since: 20230520
Until: 20230601
```

更多功能请参阅脚本内容。

### lci.ps1

初始化客户端脚本。

### lcp.ps1

接受一个 URL 参数，预览某篇文章转成 MD 格式的效果，不上传。

## 进阶使用

服务端是 ASP.NET Core 编写的 webapi，支持 Swagger，支持 OpenAPI。

本仓库的主要目标是移除对本地 Git 操作的依赖，转而使用 GitHub API，进一步降低新手译者的上手门槛。

很可惜的是，由于本人的时间/精力有限，以及兴趣点的转移，目前它仍然只是一个选题工具，只开发了选题相关的 API，没有成为真正意义上的 LCTT Server。

如有后来者，欢迎结合 VSCode 插件等技术为其开发更多的功能：申领文章、翻译文章、提交译文、修改译文等。
