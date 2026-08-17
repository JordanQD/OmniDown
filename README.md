# OmniDown

OmniDown 是一款面向 Windows 的现代下载管理器，使用 WinUI 3 构建，并通过本地 JSON-RPC 调用 aria2 或 aria2-next 完成下载。

> [!WARNING]
> 项目仍处于早期开发阶段，界面和配置格式可能发生变化。目前暂无稳定发行版，请勿将其用于没有备份的重要下载任务。

## 功能

- 管理 HTTP、HTTPS、FTP、SFTP、磁力链接和 `.torrent` 下载任务
- 新建、暂停、继续、删除、筛选、排序和搜索任务
- 查看下载进度、上下行速度、剩余时间及任务详情
- 设置全局或单任务限速、并发数、分段数和重试策略
- 配置 BitTorrent Tracker、做种、加密、代理及监听端口
- 支持断点续传、会话恢复和任务记录清理
- 支持系统通知、任务栏进度、托盘运行及下载时阻止休眠
- 支持浅色、深色和跟随系统主题
- 可选剪贴板链接检测、协议关联、本地浏览器扩展 API 和 Windows 小组件
- 支持官方 aria2、aria2-next 以及用户指定的兼容可执行文件

## 系统要求

- Windows 10 1809（版本 17763）或更高版本
- x86、x64 或 ARM64 设备
- aria2 兼容下载引擎，参见下一节

## 下载引擎

OmniDown 本身是下载管理界面，实际传输由独立的 aria2 兼容进程完成。支持以下选择：

- [aria2](https://github.com/aria2/aria2)：成熟稳定，支持 HTTP(S)、FTP、SFTP、BitTorrent 和 Metalink
- [aria2-next](https://github.com/AnInsomniacy/aria2-next)：与 aria2 RPC 兼容的维护分支，也是 OmniDown 当前的默认引擎类型
- 自定义引擎：选择一个与 aria2 JSON-RPC 兼容的可执行文件

如果发行包没有附带引擎，请自行下载适合设备架构的 Windows 版本，然后在 OmniDown 中打开 `设置 → 高级 → aria2 引擎`，选择引擎类型并指定 `aria2c.exe` 或 `aria2-next.exe` 的路径。也可以把相应程序加入 `PATH`。

OmniDown 只连接由自身启动、监听于 `127.0.0.1` 的 RPC 服务。RPC 端口和密钥可在高级设置中修改。

### 当前仓库的引擎状态

当前源码树包含用于开发的 x64 `aria2c.exe` 和 `aria2-next.exe`。项目文件会把它们复制到构建输出和发布目录，因此按当前配置生成的 x64 发行包会包含下载引擎，并非“仅提供前端、由用户自行安装引擎”。x86 和 ARM64 目录目前没有对应的内置程序。

应用可以检查并更新已有的 aria2-next，但在完全没有内核时不会自动完成首次下载。若正式发行采用“用户自行下载引擎”的策略，应在发布前移除这些二进制文件及其发布复制规则，并保留自定义路径和 `PATH` 查找方式。

## 从源码构建

### 准备环境

- Visual Studio 2022，并安装“使用 C++ 的桌面开发”和“通用 Windows 平台开发”相关组件
- .NET 8 SDK
- Windows 10/11 SDK

### 构建

克隆仓库：

```powershell
git clone https://github.com/JordanQD/OmniDown.git
cd OmniDown
```

还原依赖并构建解决方案：

```powershell
dotnet restore OmniDown.slnx
dotnet build OmniDown.slnx
```

也可以使用 Visual Studio 打开 `OmniDown.slnx`，选择目标架构后运行或打包项目。

## 项目结构

```text
OmniDown/
├─ Controls/        可复用 WinUI 控件
├─ Dialogs/         新建下载等对话框
├─ Engines/aria2/   引擎配置及当前开发用二进制文件
├─ Models/          下载任务和设置模型
├─ Pages/           下载与设置页面
├─ Services/        aria2 RPC、引擎、通知、托盘和小组件等服务
├─ Strings/         简体中文和英文资源
└─ ViewModels/      页面与控件视图模型
```

## 参与开发

欢迎通过 [Issues](https://github.com/JordanQD/OmniDown/issues) 报告问题或提出建议。提交代码前，请确保解决方案可以成功构建，并尽量让每次改动只处理一个明确问题。

## 许可证

OmniDown 当前尚未添加正式的开源许可证。在许可证确定前，公开仓库并不自动代表允许复制、修改或再分发代码。

aria2 与 aria2-next 是独立的第三方项目，使用 GNU GPL v2 或更高版本授权。无论 OmniDown 最终采用何种许可证，下载、使用或再分发这些引擎时都需要分别遵守其许可证及第三方依赖条款。

## 致谢

- [aria2](https://github.com/aria2/aria2)
- [aria2-next](https://github.com/AnInsomniacy/aria2-next)
- [Windows App SDK](https://github.com/microsoft/WindowsAppSDK)
- [Windows Community Toolkit](https://github.com/CommunityToolkit/Windows)
- [WinUI Gallery](https://github.com/microsoft/WinUI-Gallery)
