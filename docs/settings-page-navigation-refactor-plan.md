# 设置页真实页面导航最终方案

> ⚠️ **当前状态（2025-07）**：Phase 1–5 已完成，Frame + Page 导航架构已运行。**本文件末尾有遗留项清单。**

> 目标：设置页最终必须使用 WinUI `Frame` + `Page` 的真实页面导航，不再使用"常驻 UserControl + Visibility 切换 + 手动重播动画"的假导航。
>
> 范围：本方案只重构设置页内部导航和设置页与 MainWindow 的边界，不同时重构下载页、全局 Shell、aria2 引擎生命周期或下载列表 MVVM。

---

## 结论

当前设置页结构不够理想，建议重构。

问题不在某一个 `EntranceThemeTransition` 写法，而在导航模型本身：`SettingsPageControl` 一次性实例化首页和所有设置分区，通过 `Visibility` 切换显示内容，再用 `ReplayEntranceTransition` 清空并重新添加 children 来强制动画重播。这条路径绕过了 WinUI 页面导航机制，所以重复进入同一个设置界面时动画容易丢失，返回、焦点、滚动位置和状态边界也会越来越难维护。

最终形态应该是：

- 设置首页是 `Page`
- 每个设置分区是独立 `Page`
- `AppSettingsPage` 是设置模块的内部导航宿主
- `AppSettingsPage` 内部持有 `Frame`
- MainWindow 不直接操纵设置页里的具体控件
- 设置项读写集中到 `SettingsPageViewModel`
- 需要系统能力的操作通过宿主接口或事件回到 MainWindow

---

## 当前实现问题

现有设置页结构：

```text
MainWindow
└── ContentFrame.Content = AppSettingsPage
    └── SettingsPageControl
        ├── SettingsHomePageControl
        ├── GeneralSettingsSectionControl
        ├── DownloadSettingsSectionControl
        ├── BitTorrentSettingsSectionControl
        ├── NetworkSettingsSectionControl
        ├── AdvancedSettingsSectionControl
        ├── AboutSettingsSectionControl
        └── WinUIGallery.Pages.SettingsPage
```

核心问题：

- `SettingsPageControl` 是导航壳，但不是 `Frame`，只能靠 `Visibility` 切换模拟页面。
- 所有设置分区常驻，生命周期不清晰。
- `ReplayEntranceTransition` 依赖 children remove/add，动画触发条件脆弱。
- 首页卡片点击和 MainWindow 都参与导航，容易出现重复导航。
- MainWindow 通过 `SettingsPage.*Control` 直接读写大量设置控件，页面边界被打穿。
- `SettingsContentScrollViewer`、`CurrentSection`、返回按钮状态、搜索过滤都由外层拼接维护。
- `WinUIGallery.Pages.SettingsPage` 示例页混在真实设置页导航中，应从生产设置结构中移除。

---

## 最终目标结构

```text
MainWindow
└── ContentFrame
    └── AppSettingsPage
        ├── SettingsFrame
        │   ├── SettingsHomePage
        │   ├── GeneralSettingsPage
        │   ├── DownloadSettingsPage
        │   ├── BitTorrentSettingsPage
        │   ├── NetworkSettingsPage
        │   ├── AdvancedSettingsPage
        │   └── AboutSettingsPage
        ├── SettingsPageViewModel
        └── ISettingsHostActions
```

最终删除：

- `OmniDown/Controls/SettingsPageControl.xaml`
- `OmniDown/Controls/SettingsPageControl.xaml.cs`
- `OmniDown/Controls/SettingsHomePageControl.xaml`
- `OmniDown/Controls/SettingsHomePageControl.xaml.cs`
- `WinUIGallery.Pages.SettingsPage` 在 OmniDown 设置页里的入口和引用

最终保留但改造：

- `AppSettingsPage`：设置页内部导航宿主
- `SettingsPageViewModel`：设置数据和保存状态的唯一页面级状态源
- 各设置 section 的 XAML 内容：迁移到对应 `Page`，不是继续包在旧 UserControl 壳里

---

## 页面职责

### AppSettingsPage

职责：

- 持有 `Frame x:Name="SettingsFrame"`
- 初始化进入 `SettingsHomePage`
- 根据 tag 导航到对应设置分区 Page
- 维护 `CurrentSection`
- 暴露 `CanGoBack`
- 处理设置页内部 `GoBack`
- 向 MainWindow 发出 `NavigationStateChanged`
- 持有 `SettingsPageViewModel`
- 持有 `ISettingsHostActions`

不负责：

- 不直接保存具体设置字段
- 不直接启动或停止 aria2
- 不直接打开 Picker、系统链接、文件夹
- 不暴露具体 TextBox、ToggleSwitch、NumberBox 给 MainWindow

### SettingsHomePage

职责：

- 显示设置分区入口卡片
- 点击卡片后请求导航到分区 tag
- 使用内置 `EntranceThemeTransition` 做页面内容入场动画

不负责：

- 不知道 MainWindow
- 不保存设置
- 不维护返回按钮

### 各 Settings Page

目标页面：

- `GeneralSettingsPage`
- `DownloadSettingsPage`
- `BitTorrentSettingsPage`
- `NetworkSettingsPage`
- `AdvancedSettingsPage`
- `AboutSettingsPage`

职责：

- 只渲染本分区设置
- 从 `SettingsPageViewModel` 读取状态
- 用户编辑后更新 ViewModel
- 触发 `HasPendingChanges`
- 需要系统能力时调用 `ISettingsHostActions`
- 页面加载后设置初始焦点和滚动位置

不负责：

- 不直接访问 MainWindow
- 不直接弹全局 InfoBar
- 不持有下载任务集合

---

## 导航规则

入口：

```csharp
SettingsFrame.Navigate(
    typeof(SettingsHomePage),
    null,
    new EntranceNavigationTransitionInfo());
```

从首页进入分区：

```csharp
SettingsFrame.Navigate(
    pageType,
    sectionTag,
    new DrillInNavigationTransitionInfo());
```

从分区返回首页：

```csharp
if (SettingsFrame.CanGoBack)
{
    SettingsFrame.GoBack(new SlideNavigationTransitionInfo
    {
        Effect = SlideNavigationTransitionEffect.FromLeft
    });
}
```

重复点击同一个分区：

- 如果当前已经在同一个分区，不重复导航。
- 如需重新播放动画，必须通过显式刷新命令或重新 Navigate 到新 Page 实例，不使用 children remove/add。

MainWindow 标题栏返回按钮：

- 显示条件：`_currentTaskFilter == "Settings" && _appSettingsPage.CanGoBack`
- 点击行为：调用 `_appSettingsPage.GoBack()`
- MainWindow 不直接判断 `SettingsPage.CurrentSection`

---

## 状态和保存模型

最终设置数据流：

```text
Settings Page controls
→ SettingsPageViewModel
→ AppSettingsStore
→ services/runtime sync
```

要求：

- `SettingsPageViewModel` 暴露各分区设置对象或属性。
- 页面通过绑定或明确方法更新 ViewModel。
- ViewModel 维护 `HasPendingChanges`。
- 保存提示由 `HasPendingChanges` 驱动。
- 保存按钮触发 ViewModel 统一保存。
- 保存成功后由 AppSettingsPage 请求 MainWindow 显示消息。

MainWindow 仍保留的跨应用职责：

- `FolderPicker`
- 打开链接、文件夹、日志目录、配置目录
- aria2 启动、停止、重启
- 手动检查 engine update
- 协议关联刷新
- 全局 InfoBar / TeachingTip 显示

这些能力通过接口进入设置页：

```csharp
public interface ISettingsHostActions
{
    Task<string?> PickDownloadDirectoryAsync();
    Task OpenUriAsync(Uri uri);
    Task OpenFolderAsync(string path);
    Task RestartAriaAsync();
    Task StartOrStopAriaAsync();
    Task CheckEngineUpdateAsync();
    void ShowMessage(string message, InfoBarSeverity severity);
    void DismissSettingsTeachingTips();
}
```

---

## 搜索行为

最终搜索不再通过 `SettingsPageControl.ApplySearchFilter` 遍历所有常驻控件。

推荐实现：

- `SettingsPageViewModel` 建立所有设置项的搜索索引。
- 搜索框输入时更新 `SettingsSearchQuery`。
- 首页显示匹配到的分区和设置项摘要。
- 如果当前在分区页，只过滤当前页可见设置项。
- 点击搜索结果导航到对应 Page，并滚动到目标设置项。

这避免为了搜索而常驻所有分区控件。

---

## 文件迁移清单

- [x] 新增：`OmniDown/Pages/SettingsHomePage.xaml/.cs`
- [x] 新增：`OmniDown/Pages/GeneralSettingsPage.xaml/.cs`
- [x] 新增：`OmniDown/Pages/DownloadSettingsPage.xaml/.cs`
- [x] 新增：`OmniDown/Pages/BitTorrentSettingsPage.xaml/.cs`
- [x] 新增：`OmniDown/Pages/NetworkSettingsPage.xaml/.cs`
- [x] 新增：`OmniDown/Pages/AdvancedSettingsPage.xaml/.cs`
- [x] 新增：`OmniDown/Pages/AboutSettingsPage.xaml/.cs`
- [x] 新增：`OmniDown/Services/Settings/ISettingsHostActions.cs`
- [x] 新增：`OmniDown/MainWindow.HostActions.cs`
- [x] 重写：`OmniDown/Pages/AppSettingsPage.xaml/.cs`
- [x] 重写：`OmniDown/ViewModels/SettingsPageViewModel.cs`
- [x] 重写：`OmniDown/MainWindow.Shell.xaml.cs`
- [x] 重写：`OmniDown/MainWindow.PageRedirects.cs`
- [x] 删除：`OmniDown/Controls/SettingsPageControl.xaml/.cs`
- [x] 删除：`OmniDown/Controls/SettingsHomePageControl.xaml/.cs`
- [x] 移除：WinUIGallery Example 卡片
- [ ] 待删除（内容迁移后）：6 个 `*SettingsSectionControl.xaml/.cs`

---

## 落地顺序（Phase 进度）

1. ✅ **Phase 1** — 新建 `ISettingsHostActions`，改造 `SettingsPageViewModel`（HasPendingChanges / SaveAll / 分区更新）
2. ✅ **Phase 2** — 重写 `AppSettingsPage` 为 Frame 导航宿主，新建 `SettingsHomePage`
3. ✅ **Phase 3** — 新建 6 个分区 Page（空壳 + SetSectionContent 注入模式）
4. ✅ **Phase 4** — MainWindow 实现 ISettingsHostActions，Shell 改用 Frame 导航 API
5. ✅ **Phase 5** — 删除旧 SettingsPageControl / SettingsHomePageControl，PageRedirects 重连，Build 0 error
6. ⏳ **Phase 6** — 见下方"遗留项"

---

## 验证清单

构建：

```powershell
dotnet build OmniDown.slnx -p:Platform=x64
```

导航和动画：

- [ ] 第一次进入设置首页有页面动画。
- [ ] 从首页进入每个分区都有 `DrillInNavigationTransitionInfo` 动画。
- [ ] 从分区返回首页有反向动画。
- [ ] 多次进入同一个分区不会丢动画。
- [ ] 当前已在某分区时重复点击同一分区不会重复堆栈。
- [ ] 标题栏返回按钮只在设置分区页显示。
- [ ] 返回按钮不会影响下载页导航状态。

设置行为：

- [ ] 通用设置读写正常。
- [ ] 下载设置读写正常。
- [ ] BitTorrent 设置读写正常。
- [ ] 网络设置读写正常。
- [ ] 高级设置读写正常。
- [ ] 关于页版本、克隆命令、链接正常。
- [ ] 修改任意设置后保存提示出现。
- [ ] 保存后提示消失，设置持久化。
- [ ] 需要重启 aria 的设置仍显示对应提示。
- [ ] aria 启停、重启、手动更新仍可从高级页触发。

搜索：

- [ ] 在设置首页搜索能找到所有分区的设置项。
- [ ] 点击搜索结果进入正确分区。
- [ ] 进入分区后能定位到目标设置项。
- [ ] 切换回下载页后搜索框占位和行为恢复下载页语义。

回归：

- [ ] 下载页不受设置页重构影响。
- [ ] 任务列表筛选、状态栏、详情面板不变。
- [ ] 应用启动默认仍进入下载页。
- [ ] 从通知唤起应用仍进入下载页。
- [ ] 切换 Settings / Home / Transfers / Complete / Issues 不串状态。

---

## 不做的事

本方案不做：

- 不重构下载页 MVVM。
- 不改 aria2 engine host。
- 不改下载任务列表 UI。
- 不升级 WinUI、Windows App SDK、.NET 或 CommunityToolkit。
- 不保留旧 `SettingsPageControl` 作为长期兼容层。
- 不再使用手动 children remove/add 来重播页面进入动画。

---

## 遗留项

以下工作尚未完成，按依赖关系排序：

### L1 — SectionControl 内容迁移到 Page

当前 6 个 Page 是"空壳"：XAML 为空，内容由 `AppSettingsPage` 在 `Frame.Navigated` 时通过 `SetSectionContent()` 注入共享的 `*SectionControl` 实例。

**需要做**：将每个 `*SectionControl` 的 XAML 和 code-behind 逻辑直接搬进对应的 Page，然后删除 6 个 SectionControl 文件。

文件：
- `GeneralSettingsSectionControl.xaml/.cs` → `GeneralSettingsPage.xaml/.cs`
- `DownloadSettingsSectionControl.xaml/.cs` → `DownloadSettingsPage.xaml/.cs`
- `BitTorrentSettingsSectionControl.xaml/.cs` → `BitTorrentSettingsPage.xaml/.cs`
- `NetworkSettingsSectionControl.xaml/.cs` → `NetworkSettingsPage.xaml/.cs`
- `AdvancedSettingsSectionControl.xaml/.cs` → `AdvancedSettingsPage.xaml/.cs`
- `AboutSettingsSectionControl.xaml/.cs` → `AboutSettingsPage.xaml/.cs`

迁移后 `AppSettingsPage` 不再需要创建和持有这 6 个 SectionControl 实例。

### L2 — MainWindow.Settings.xaml.cs 完全解耦

当前 `MainWindow.Settings.xaml.cs` 仍通过 `AppSettingsPage` 暴露的 130+ 个控件属性直接读写设置控件（和旧 `SettingsPageControl` 模式完全一样）。

**需要做**：将所有"从控件取值 → 写入 ViewModel"的逻辑改为 ViewModel 单向数据流，将"Picker / 打开链接 / aria 操作"改为通过 `ISettingsHostActions`。完成后可删除 `AppSettingsPage` 上 130+ 个控件属性。

### L3 — 搜索重构

当前搜索仍通过 `AppSettingsPage.ApplySearchFilter()` 遍历 6 个 SectionControl 的 `SearchEntries` 做 Visibility 切换。

**需要做**：在 `SettingsPageViewModel` 建立搜索索引，首页显示匹配摘要，分区页过滤当前页设置项，点击结果导航到目标页并滚动到位。完成后可删除 `AppSettingsPage.ApplySearchFilter()` 和 `GetSearchEntries()`。

### L4 — 手动验证

完成 L1–L3 后按上方"验证清单"逐项手动验证。
