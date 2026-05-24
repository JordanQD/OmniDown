**不要让 widget 直接变成第二个下载管理器**。最稳的是做一个共享的“状态读取层”，主 App 负责维护状态，widget provider 只读这个状态；需要实时性时再轻量补一次 aria2 RPC。

你现在项目里已经有这些基础：

- [DownloadCoordinator.cs](</C:/Users/Q/Data/Visual Studio/OmniDown/OmniDown/Services/Downloads/DownloadCoordinator.cs>)：负责把 aria2 的 raw 状态归一化成 `DownloadTask`，还维护 `tasks.json` 缓存。
- [Aria2RpcClient.cs](</C:/Users/Q/Data/Visual Studio/OmniDown/OmniDown/Services/Rpc/Aria2RpcClient.cs>)：已有 aria2 RPC 封装。
- [Aria2EngineHost.cs](</C:/Users/Q/Data/Visual Studio/OmniDown/OmniDown/Services/Engine/Aria2EngineHost.cs>)：负责启动/停止 aria2。
- [MainWindow.TaskStatus.xaml.cs](</C:/Users/Q/Data/Visual Studio/OmniDown/OmniDown/MainWindow.TaskStatus.xaml.cs>)：当前 UI 刷新周期里调用 `DownloadCoordinator.RefreshAsync()`。

**数据来源建议**
我建议：**widget 主要读 App 生成的 compact snapshot，不直接以 aria2 RPC 为唯一数据源**。

原因很实际：

| 方案 | 优点 | 问题 |
|---|---|---|
| 直接读 aria2 RPC | 实时、简单、绕过 UI | widget 要知道 rpc port/secret；App 不运行时 aria2 可能也不在；会绕过 OmniDown 的任务名、缓存、状态归一化、完成任务处理逻辑 |
| 走主 App 状态 | 和 UI 一致、逻辑复用、可显示离线/上次状态 | 需要增加一个轻量 snapshot 文件或共享服务 |
| 混合方案 | 最稳 | 多一点结构设计 |

推荐混合方案：

1. 主 App 每次刷新下载状态后，写一个 `widget-snapshot.json`。
2. Widget provider 启动时先读 `widget-snapshot.json`，保证快、稳定、不依赖 aria2 当前是否活着。
3. 如果配置里 aria2 RPC 可达，widget provider 可以补充调用 `aria2.getGlobalStat` / `tellActive` 做一次轻量刷新。
4. Widget provider **不要启动 aria2**，至少第一版不要。启动下载引擎应该仍由主 App 负责。

**开发计划**

**第 1 步：定义第一版 widget 能力**
先只做一个低风险版本：

- 显示当前下载数
- 显示总下载速度/上传速度
- 显示最近 3 个活动任务
- 显示暂停/错误/完成计数
- 按钮：打开 OmniDown
- 可选按钮：暂停全部 / 继续全部，放到第二阶段

第一版不要做新增下载、选择文件、复杂任务操作。Windows Widgets 的交互能力有限，越像“仪表盘”越稳定。

**第 2 步：升级/确认 Windows App SDK 版本**
你现在是：

```xml
<PackageReference Include="Microsoft.WindowsAppSDK" Version="2.0.1" />
```

Widgets 可用，但我建议先评估升级到当前稳定版，例如 `2.1.x`。理由是 widgets、web widget、customization 这些 API 都在 Windows App SDK 后续版本里持续补强。

这一阶段只做：

- 升级包
- build
- 确认现有 WinUI 功能没炸
- 不同时改 widget 逻辑

**第 3 步：抽出 widget 状态模型**
新增类似：

```text
Services/Widgets/WidgetSnapshot.cs
Services/Widgets/WidgetSnapshotStore.cs
```

内容只保留 widget 需要的数据，不把整个 `DownloadTask` 暴露出去：

```csharp
public sealed record WidgetSnapshot(
    DateTimeOffset UpdatedAt,
    bool EngineRunning,
    long DownloadSpeed,
    long UploadSpeed,
    int ActiveCount,
    int WaitingCount,
    int PausedCount,
    int CompletedCount,
    int ErrorCount,
    IReadOnlyList<WidgetTaskSummary> Tasks);
```

`WidgetTaskSummary` 只放：

- `Gid`
- `Name`
- `Status`
- `Progress`
- `DownloadSpeed`
- `CompletedLength`
- `TotalLength`

**第 4 步：让主 App 写 snapshot**
在 `RefreshDownloadsAsync()` 成功后写 snapshot。也就是现在这里：

```csharp
DownloadSnapshot snapshot = await _downloadCoordinator.RefreshAsync();
```

后面增加：

```csharp
await _widgetSnapshotStore.SaveAsync(...);
```

这一步的目标是：不管 widget 有没有开发完成，App 本身已经能产出稳定状态文件。

**第 5 步：增加 widget provider 项目**
建议不要把 provider 强塞进 `MainWindow` 进程逻辑里。更清晰的结构是：

```text
OmniDown.WidgetProvider/
OmniDown.Shared/    可选
```

如果不想一开始拆太大，可以先在主项目里加 provider 代码，但长期我建议至少把 snapshot model/store 放到可共享层。

Provider 需要：

- 实现 `IWidgetProvider`
- 实现 COM class factory
- 处理 `CreateWidget`
- 处理 `DeleteWidget`
- 处理 `Activate`
- 处理 `Deactivate`
- 处理 `OnWidgetContextChanged`
- 用 `WidgetManager.GetDefault().UpdateWidget(...)` 推送 Adaptive Card JSON

**第 6 步：改 package manifest 注册 widget**
在 [Package.appxmanifest](</C:/Users/Q/Data/Visual Studio/OmniDown/OmniDown/Package.appxmanifest>) 里增加：

- `uap3` namespace
- `com` namespace
- `windows.comServer`
- `windows.appExtension`
- `com.microsoft.windows.widgets`
- widget definition：small / medium / large
- provider icons / screenshots

这一步最容易出错，建议直接按官方 sample 改，不自己发明结构。

**第 7 步：做 Adaptive Card 模板**
第一版准备三套：

- small：速度 + 活跃任务数
- medium：速度 + 计数 + 2 个任务
- large：速度 + 计数 + 3-5 个任务

模板 JSON 放文件里，不硬编码在 C# 大字符串里更好：

```text
Assets/Widgets/omnidown-small.json
Assets/Widgets/omnidown-medium.json
Assets/Widgets/omnidown-large.json
```

Provider 根据 widget size 选择模板，data 从 `widget-snapshot.json` 来。

**第 8 步：打开 App 的动作**
第一版只做 `Action.OpenUrl`：

```text
omnidown://open
```

你现有 manifest 已经注册了 `omnidown` protocol，所以这条路是自然的。

后续可以加：

```text
omnidown://new
omnidown://tasks
```

**第 9 步：动作能力第二阶段再加**
暂停全部 / 继续全部有两种做法：

- widget action 触发 provider，provider 直接调用 aria2 RPC
- widget action 触发 protocol，把主 App 拉起来执行命令

我建议第二阶段用 **主 App 命令入口**。原因是状态、通知、缓存、错误处理都能走现有逻辑。直接 RPC 可以作为 fallback，但不应该成为主要业务通道。

**第 10 步：测试矩阵**
最少测这些：

- App 正在运行，widget 能实时更新
- App 关闭，widget 显示上次状态，而不是空白
- aria2 未运行，widget 显示“未运行/无活动任务”
- 下载中、暂停、完成、错误状态显示正确
- small / medium / large 三种尺寸
- 深色/浅色主题
- 安装/卸载/重新部署后 widget 是否还出现在 Widgets Board
- 点击 widget 打开 OmniDown

**第 11 步：发布前处理**
最后再做：

- provider 改成无控制台窗口
- 补 widget icon/screenshot
- 检查 MSIX manifest
- 检查 Store 需要的 widget metadata
- 确认没有把 aria2 RPC secret 显示到日志或 widget data

**我的推荐落地顺序**
先做这四件事：

1. 升级或确认 Windows App SDK 版本。
2. 加 `WidgetSnapshot` / `WidgetSnapshotStore`。
3. 让主 App 每次刷新后写 snapshot。
4. 建一个最小 widget provider，只显示速度和任务数。

等这个跑通，再加任务列表、三尺寸布局、暂停/继续按钮。这样风险最低，也不会把 widget 和 aria2 生命周期搅在一起。