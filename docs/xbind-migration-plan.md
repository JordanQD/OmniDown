# DownloadsPage `{x:Bind}` 迁移计划

> 分支: `app-restructure` | 前提: Phase 1–5 已完成，`DownloadsPageViewModel` 已就绪
>
> 目标：把 `DownloadsPage.xaml` 中硬编码的初始值和 code-behind 手动赋值，替换为 `{x:Bind}` 绑定 `DownloadsPageViewModel` 属性。

---

## 背景

当前 `MainWindow.TaskStatus.xaml.cs` 中有 ~200+ 行代码用于手动更新 UI 控件：

```csharp
GlobalDownloadSpeedText.Text = FormatSpeed(downloadSpeed);
StatusBarItemCountText.Text = $"{itemCount} 个项目";
TasksLoadingPanel.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
```

每处更新都需要 C# 代码操作控件，分散在多个 partial class 文件中。迁移到 `{x:Bind}` 后，XAML 直接绑定 ViewModel 属性，属性变化 UI 自动刷新。

| 对比 | 现在（code-behind） | 迁移后（{x:Bind}） |
|------|-------------------|-------------------|
| Code-behind 代码量 | ~200+ 行 UI 更新代码 | 删掉，只保留交互逻辑 |
| 数据流 | 散落在 5 个 partial class | 集中在 ViewModel 一处 |
| 可测试性 | 必须启动窗口 | 只测 ViewModel，不依赖 UI |
| 改动风险 | 改一处可能漏更新另一处 | 绑定就不会漏 |

---

## 0. 准备工作 — ViewModel 接入 XAML

- 在 `DownloadsPage.xaml.cs` 中添加属性 `public DownloadsPageViewModel ViewModel => ...`
- 在 `DownloadsPage` 的构造函数中接收 ViewModel（由 `MainWindow` 创建并传入）
- 在 `DownloadsPage.xaml` 根元素 `<Page>` 加 `x:DataType="viewmodels:DownloadsPageViewModel"`（或通过 `x:Bind` 指定本地类型）
- 在 `MainWindow` 构造函数中将 `_downloadsViewModel` 传给 `DownloadsPage`

**影响文件**：`DownloadsPage.xaml.cs`, `MainWindow.xaml.cs`

**风险**：低（纯新增，不删不改现有逻辑）

---

## 1. 速度显示区域

### 迁移的控件绑定

| 控件 | 属性 | 绑到 ViewModel |
|------|------|---------------|
| `_globalDownloadSpeedText` | `Text` | `GlobalDownloadSpeedText` |
| `_globalUploadSpeedText` | `Text` | `GlobalUploadSpeedText` |
| `_globalDownloadLimitText` | `Text` | `GlobalDownloadLimitText` |
| `_globalDownloadLimitText` | `Opacity` | `BoolToOpacity(IsGlobalDownloadLimitIconVisible)` |
| `_globalUploadLimitText` | `Text` | `GlobalUploadLimitText` |
| `_globalUploadLimitText` | `Opacity` | `BoolToOpacity(IsGlobalUploadLimitIconVisible)` |
| `_globalDownloadLimitIconPanel` | `Opacity` | `BoolToOpacity(IsGlobalDownloadLimitIconVisible)` |
| `_globalUploadLimitIconPanel` | `Opacity` | `BoolToOpacity(IsGlobalUploadLimitIconVisible)` |

### 删除的 code-behind

- `UpdateGlobalSpeeds` 中设置 `GlobalDownloadSpeedText.Text` / `GlobalUploadSpeedText.Text` 的行
- `UpdateGlobalSpeedLimitText` 中设置 Global Limit 相关 Text / Opacity 的行
- 保留 `StatusBar` 速度文本的 code-behind（后续步骤处理）

**影响文件**：`DownloadsPage.xaml`, `MainWindow.TaskStatus.xaml.cs`

**风险**：低（纯显示，无交互依赖）

---

## 2. 状态栏区域

### 迁移的控件绑定

| 控件 | 属性 | 绑到 ViewModel |
|------|------|---------------|
| `_statusBarItemCountText` | `Text` | `StatusBarItemCountText` |
| `_statusBarSelectedCountText` | `Text` | `StatusBarSelectedCountText` |
| `_statusBarSelectedCountText` | `Visibility` | `BoolToVisibility(IsStatusBarSelectedCountVisible)` |
| `_statusBarSelectedCountDivider` | `Visibility` | `BoolToVisibility(IsStatusBarSelectedCountVisible)` |
| `_statusBarTaskCountsDivider` | `Visibility` | `BoolToVisibility(IsStatusBarTaskCountsPanelVisible)` |
| `_statusBarTaskCountsPanel` | `Visibility` | `BoolToVisibility(IsStatusBarTaskCountsPanelVisible)` |
| `_statusBarActiveTasksText` | `Text` | `StatusBarActiveTasksText` |
| `_statusBarPausedTasksText` | `Text` | `StatusBarPausedTasksText` |
| `_statusBarIssueTasksText` | `Text` | `StatusBarIssueTasksText` |
| `_statusBarIssueTasksPanel` | `Visibility` | `BoolToVisibility(IsStatusBarIssueTasksPanelVisible)` |
| `_statusBarSpeedPanel` | `Visibility` | `BoolToVisibility(IsStatusBarSpeedPanelVisible)` |
| `_statusBarDownloadSpeedText` | `Text` | `StatusBarDownloadSpeedText` |
| `_statusBarUploadSpeedText` | `Text` | `StatusBarUploadSpeedText` |
| `_statusBarDownloadLimitPanel` | `Visibility` | `BoolToVisibility(IsStatusBarDownloadLimitVisible)` |
| `_statusBarUploadLimitPanel` | `Visibility` | `BoolToVisibility(IsStatusBarUploadLimitVisible)` |
| `_statusBarDownloadLimitText` | `Text` | `StatusBarDownloadLimitText` |
| `_statusBarUploadLimitText` | `Text` | `StatusBarUploadLimitText` |

### 删除的 code-behind

- `UpdateStatusBar` 中设置上述控件属性的大部分代码
- `UpdateGlobalSpeedLimitText` 中 StatusBar 限速相关代码
- `UpdateGlobalSpeeds` 中 StatusBar 速度相关代码

**影响文件**：`DownloadsPage.xaml`, `MainWindow.TaskStatus.xaml.cs`

**风险**：中（多处引用 `UpdateStatusBar`，需确认没有遗漏调用方）

---

## 3. 仪表盘指标区域

### 迁移的控件绑定

| 控件 | 属性 | 绑到 ViewModel |
|------|------|---------------|
| `_downloadsTitleText` | `Text` | `DownloadsTitleText` |
| `_totalTasksText` | `Text` | `TotalTasksText` |
| `_activeTasksText` | `Text` | `ActiveTasksText` |
| `_pausedTasksText` | `Text` | `PausedTasksText` |
| `_completedTasksText` | `Text` | `CompletedTasksText` |
| `_issueTasksText` | `Text` | `IssueTasksText` |
| `_statsPanel` | `Visibility` | `BoolToVisibility(IsStatsPanelVisible)` |
| `_completedMetricPanel` | `Visibility` | `BoolToVisibility(IsCompletedMetricVisible)` |
| `_issueMetricPanel` | `Visibility` | `BoolToVisibility(IsIssueMetricVisible)` |

### 删除的 code-behind

- `UpdateDashboard` 中手动设置 Text 的代码
- `UpdateStatsVisibility` 中手动设置 Visibility 的代码

**影响文件**：`DownloadsPage.xaml`, `MainWindow.TaskStatus.xaml.cs`, `MainWindow.SearchAndHelpers.xaml.cs`

**风险**：中

---

## 4. 加载状态 & 详情面板

### 迁移的控件绑定

| 控件 | 属性 | 绑到 ViewModel |
|------|------|---------------|
| `_tasksLoadingPanel` | `Visibility` | `BoolToVisibility(IsLoading)` |
| `_tasksLoadingRing` | `IsActive` | `IsLoading` |
| `_tasksListView` | `Visibility` | `InvertBoolToVisibility(IsLoading)` |
| `_taskDetailsPane` | `Visibility` | `BoolToVisibility(IsTaskDetailsPaneOpen)` |
| `_taskDetailsHostColumn` | `Width` | 需要 ViewModel 提供计算后的 GridLength |

### 删除的 code-behind

- `SetTaskListLoading` 中手动设置 Visibility 的代码
- `UpdateTaskDetailsPaneVisibility` 中手动设置 Visibility 的代码（`Width` 需要保留或通过函数绑定处理）

**影响文件**：`DownloadsPage.xaml`, `MainWindow.TaskStatus.xaml.cs`

**风险**：中（`TaskDetailsHostColumn.Width` 两档切换需要 ViewModel 属性 `0` / `380`）

---

## 5. 清理 PageRedirects

### 删除的条件

- 控件的所有属性绑定改为 `{x:Bind}` 后，MainWindow 不再需要直接访问该控件
- 该控件没有事件转发需求

### 预计可清理

- `TotalTasksText`、`ActiveTasksText` 等纯显示 TextBlock
- `StatsPanel`、`CompletedMetricPanel` 等纯显示容器
- `StatusBarItemCountText` 等状态栏纯文本
- 全局速度文本

### 保留

- `TasksListView`（需要 `ItemsSource`、`SelectedItems` 管理）
- `SelectAllTasksCheckBox`（事件）
- `TasksLoadingPanel`、`TasksLoadingRing`（可能有程序化触发场景）
- 所有工具栏按钮（事件）
- `NotificationHistoryButton`、`NotificationHistoryListView`（事件 + ItemsSource）
- `TaskDetailsPane`（事件 + 程序化调用）

**影响文件**：`MainWindow.PageRedirects.cs`

**风险**：低（只删属性，编译报错即回退）

---

## 辅助函数

需要在 `DownloadsPage.xaml.cs` 中添加静态辅助方法供 `x:Bind` 使用：

```csharp
// DownloadsPage.xaml.cs
public static Visibility BoolToVisibility(bool value) =>
    value ? Visibility.Visible : Visibility.Collapsed;

public static Visibility InvertBoolToVisibility(bool value) =>
    value ? Visibility.Collapsed : Visibility.Visible;

public static double BoolToOpacity(bool value) =>
    value ? 1.0 : 0.0;
```

---

## 执行顺序

| 批次 | 区域 | code-behind 受影响 | 风险 |
|------|------|-------------------|------|
| 0 | ViewModel 接入 XAML | 无（纯新增） | 低 |
| 1 | 速度显示 | `UpdateGlobalSpeeds`, `UpdateGlobalSpeedLimitText` | 低 |
| 2 | 状态栏 | `UpdateStatusBar`, `UpdateGlobalSpeedLimitText` | 中 |
| 3 | 仪表盘 | `UpdateDashboard`, `UpdateStatsVisibility` | 中 |
| 4 | 加载 + 详情面板 | `SetTaskListLoading`, `UpdateTaskDetailsPaneVisibility` | 中 |
| 5 | 清理 PageRedirects | `PageRedirects.cs` | 低 |

每批完成后执行 `dotnet build OmniDown.slnx` 验证，确认 0 错误后继续下一批。
