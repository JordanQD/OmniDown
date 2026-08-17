# OmniDown

**原生 WinUI 3 Windows 下载管理器。**

OmniDown 是一款使用 Windows App SDK、WinUI 3、C# 与 XAML 构建的原生 Windows 桌面应用，通过本地 JSON-RPC 调用 aria2 或 aria2-next 完成下载。界面不是 Electron 或 WebView 套壳。

> [!WARNING]
> 项目仍处于早期开发阶段，界面和配置格式可能发生变化。目前暂无稳定发行版，请勿将其用于没有备份的重要下载任务。

## 原生 WinUI 3

OmniDown 的桌面界面直接使用 WinUI 3 控件和 Windows App SDK API 实现，遵循 Windows 的 Fluent Design，并原生适配浅色、深色及系统主题。系统通知、任务栏进度、托盘运行、协议关联和 Windows 小组件等功能也直接集成 Windows 平台能力，不依赖跨平台 Web UI 运行时。

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

发行包不附带下载引擎。请自行下载适合设备架构的 Windows 版本，然后在 OmniDown 中打开 `设置 → 高级 → aria2 引擎`，选择引擎类型并导入 `aria2c.exe` 或 `aria2-next.exe`。OmniDown 会把所选文件复制到 MSIX 管理的 `%LOCALAPPDATA%\Packages\<包系列名>\LocalState\engines\aria2\win-<架构>`，之后不再依赖原始下载位置；正常卸载应用时，这份内核也会被删除。也可以把相应程序加入 `PATH`。

OmniDown 只连接由自身启动、监听于 `127.0.0.1` 的 RPC 服务。RPC 端口和密钥可在高级设置中修改。

### 开发调试内核

aria2 可执行文件不纳入 Git。开发者可以把本机调试用内核放在 `OmniDown/Engines/aria2/win-<架构>`；Debug 构建会把它复制到输出目录，Release 构建和发布包始终忽略这些文件。这样重新编译调试时可继续使用本机内核，同时不会误发到正式包。

应用可以检查并更新已经导入的 aria2-next，但不会自动完成首次下载。配置、任务缓存和会话保存在稳定的 `%LOCALAPPDATA%\OmniDown`，不会因为重新生成或更换调试输出目录而被覆盖；导入的内核单独保存在 MSIX `LocalState`，同一包标识下重新编译/F5 会保留，正常卸载则会删除。升级后首次启动会把旧版 `%LOCALAPPDATA%\OmniDown\engines` 中的内核迁入 `LocalState`，成功后清除旧副本。

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
├─ Engines/aria2/   引擎配置及可选的本机 Debug 内核（不纳入 Git）
├─ Models/          下载任务和设置模型
├─ Pages/           下载与设置页面
├─ Services/        aria2 RPC、引擎、通知、托盘和小组件等服务
├─ Strings/         简体中文和英文资源
└─ ViewModels/      页面与控件视图模型
```

## 参与开发

欢迎通过 [Issues](https://github.com/JordanQD/OmniDown/issues) 报告问题或提出建议。提交代码前，请确保解决方案可以成功构建，并尽量让每次改动只处理一个明确问题。

## 许可证

OmniDown 源代码采用宽松的 [MIT License](LICENSE) 开源，允许使用、复制、修改和再分发，但需要保留原始版权及许可声明。

MIT License 仅适用于 OmniDown 自身代码。发行包不包含 aria2 或 aria2-next；用户自行导入的下载引擎是独立的第三方程序，采用 GNU GPL v2 授权，下载、使用或再分发时需要分别遵守对应项目的许可证及第三方依赖条款。

## 依赖与参考

OmniDown 的实现、界面设计和功能规划参考或使用了以下开源项目：

- [files-community/Files](https://github.com/files-community/Files)
- [AnInsomniacy/motrix-next](https://github.com/AnInsomniacy/motrix-next)
- [aria2/aria2](https://github.com/aria2/aria2)
- [AnInsomniacy/aria2-next](https://github.com/AnInsomniacy/aria2-next)
- [Devolutions/UniGetUI](https://github.com/Devolutions/UniGetUI)
- [microsoft/WinUI-Gallery](https://github.com/microsoft/WinUI-Gallery)
- [microsoft/WindowsAppSDK](https://github.com/microsoft/WindowsAppSDK)
- [microsoft/microsoft-ui-xaml](https://github.com/microsoft/microsoft-ui-xaml)
- [CommunityToolkit/Windows](https://github.com/CommunityToolkit/Windows)

## Tracker 来源

内置 Tracker 来源选项由以下公开项目提供：

- [XIU2/TrackersListCollection](https://github.com/XIU2/TrackersListCollection)
- [ngosang/trackerslist](https://github.com/ngosang/trackerslist)
