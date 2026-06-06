# OmniDown 应用重构计划

> 分支: `app-restructure` | 基准: `aria2next`
>
> 目标：将 `MainWindow.xaml`（~1300 行 XAML + 8 个 partial class 文件）拆分为 Frame 驱动页面导航 + 轻量 Shell Window。

---

## 当前问题

- **单体巨石**：MainWindow 承担了导航、任务列表、设置、引擎管理、通知等全部职责
- **没有 Frame 导航**：`NavigationView` 不使用 `<Frame>`，通过 `Visibility` 切换内容区域——WinUI 反模式
- **假页面**：Home / Transfers / Complete / Issues 四个导航项是同一份 `ListView` + 不同过滤参数
- **设置嵌入在主窗口**：`SettingsPageControl` 是 `UserControl` 内嵌在 `MainWindow.xaml`
- **代码后置臃肿**：`MainWindow.xaml.cs` 混杂了引擎生命周期、更新检查、设置加载等数十个关注点

---

## Phase 1: 抽取 Shell Window — 轻量化 MainWindow

- 将 `MainWindow.xaml` 精简为：`TitleBar` + `NavigationView` + `<Frame>` + 全局 `InfoBar` + 状态栏
- 移除 `TasksContentHost`（任务列表区）、`SettingsPage`、`TaskDetailsPane` 的内嵌 XAML——它们将成为 Frame 中的独立 Page
- 引擎生命周期、通知处理等核心服务保持不变，仍由 MainWindow 持有和协调
- `NavigationView` 内容区改为 `<Frame x:Name="ContentFrame" />`

## Phase 2: 将内容区拆分为独立 Page

- **新建 `DownloadsPage`**：承载原 `TasksContentHost` 全部内容（统计面板 + `CommandBar` 工具栏 + `ListView` + 详情面板 `TaskDetailsPane`）
  - 页面内保留"详情面板"的展开/折叠，由 `DownloadsPage` 内部控制
- **重构 `SettingsPage`**：将 `SettingsPageControl` + `SettingsHomePageControl` + 各 Section `UserControl` 整合为 Frame 内的独立 `Page`
  - 设置子页面继续使用内部 `NavigateTo` 模式
  - 移除 `WinUIGallery.Pages.SettingsPage` 示例页依赖

## Phase 3: 引入 Frame 导航

- `NavigationView.SelectionChanged` → `ContentFrame.Navigate(typeof(XxxPage), parameter)`
- 导航参数传递过滤器标签（`"Home"` / `"Downloading"` / `"Completed"` / `"Issues"`），`DownloadsPage` 根据参数切换视图
- `TitleBar.BackRequested` → `ContentFrame.GoBack()`，设置页内部回退通过 `SettingsPage` 自身处理
- `NavigationView` 的 `SelectedItem` 与 `Frame.CurrentSourcePageType` 保持同步

## Phase 4: 侧栏导航项规划（选项 B — 保留原有分类）

| 导航项 | 图标 | 目标 Page | Tag 参数 |
|--------|------|-----------|----------|
| **Home** | `Home` | `DownloadsPage` | `"Home"` |
| **Transfers** | `Play` | `DownloadsPage` | `"Downloading"` |
| **Complete** | `Accept` | `DownloadsPage` | `"Completed"` |
| **Issues** | `Important` | `DownloadsPage` | `"Issues"` |
| **Settings** | `Setting` (Footer) | `SettingsPage` | — |

- 四个下载视图导航到同一个 `DownloadsPage`，通过 Tag 传参切换过滤器
- `DownloadsPage` 内部根据参数调用不同过滤逻辑，四种视图共享同一份 ListView / 工具栏 / 详情面板

## Phase 5: MVVM 分层（后续阶段）

**作用**：把逻辑从 View 里抽出来——当前 MainWindow code-behind 直接处理任务过滤、排序、选中状态、进度刷新，移到 ViewModel 后：

- **View（XAML）只负责渲染**：`{x:Bind ViewModel.Tasks}` 绑定列表
- **ViewModel 负责数据和逻辑**：过滤、排序、刷新、选中/全选，不依赖 UI 控件引用
- **可测试**：可对 ViewModel 写单元测试
- **Code-behind 变薄**：只留下纯 UI 操作（焦点切换、动画、右键菜单等）

具体步骤：
- 创建 `DownloadsPageViewModel`：管理 `Tasks` 集合、过滤/排序状态、选中/全选状态、刷新计时器
- 将 `DownloadTask` 相关字段和方法从 `MainWindow` 迁移到 ViewModel
- XAML 通过 `{x:Bind}` 绑定 ViewModel 属性
- Code-behind 只保留纯 UI 交互（右键菜单、拖放、焦点管理）
