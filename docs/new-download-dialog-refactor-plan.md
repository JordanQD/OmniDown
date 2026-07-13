# 新建下载对话框修复与重构计划

## 目标

在不改变下载引擎、设置模型和现有下载任务行为的前提下，将当前由 `MainWindow` 动态构建的新建下载界面重构为符合 WinUI 3 常规模式的独立 `ContentDialog`，并修复提交校验、键盘交互、响应式布局、可访问性和大 Torrent 文件列表性能问题。

## 范围边界

### 本次包含

- 修复 Enter 键绕过标准提交流程的问题。
- 将提交校验移到对话框关闭前，并显示内联错误。
- 将动态 C# 控件树迁移到独立 XAML `ContentDialog`。
- 为链接与 Torrent 模式建立明确的选择状态和键盘语义。
- 引入对话框专用 ViewModel 和结构化返回结果。
- 使用虚拟化列表显示 Torrent 文件。
- 补齐可访问名称、自动化标识、主题资源和响应式布局。
- 保留剪贴板识别、拖放 Torrent、文件/目录选择、多链接下载及 Torrent 文件选择能力。

### 本次不包含

- 不重构 aria2 RPC、引擎生命周期或 `DownloadCoordinator` 内部实现。
- 不全面迁移 `MainWindow` 其他 partial 文件。
- 不升级 .NET、Windows App SDK 或 CommunityToolkit 包。
- 不重新设计下载列表页、设置页或主导航。
- 不改变下载参数默认值及现有本地化文案含义，除非修复校验提示所必需。

## 目标结构

```text
Dialogs/
  NewDownloadDialog.xaml
  NewDownloadDialog.xaml.cs
  NewDownloadDialogResult.cs
ViewModels/
  NewDownloadDialogViewModel.cs
Services/Downloads/
  TorrentSelectionService.cs       # 仅在文件读取逻辑确实需要独立复用时创建
MainWindow.NewDownloadDialog.xaml.cs
  # 只保留入口、显示对话框、启动引擎和提交 DownloadCoordinator
```

职责约束：

- `NewDownloadDialog.xaml`：布局、视觉状态、绑定和可访问性属性。
- `NewDownloadDialogViewModel`：输入状态、模式、Torrent 文件选择状态和同步校验。
- `NewDownloadDialog.xaml.cs`：文件/目录 Picker、拖放、焦点协调和异步校验。
- `NewDownloadDialogResult`：对话框成功提交后返回的不可变请求数据。
- `MainWindow`：显示对话框，确保 aria2 已启动，并调用 `DownloadCoordinator`。

## 阶段 1：先修复现有行为缺陷

目标是在大规模迁移前建立正确行为基线，避免把缺陷原样搬入新对话框。

1. 删除根容器对所有 Enter 键执行 `dialog.Hide()` 的处理。
2. 保留 `DefaultButton="Primary"` 的标准行为。
3. 在 PrimaryButton 点击阶段执行校验；校验失败时取消关闭。
4. 链接模式至少校验一个有效下载源。
5. Torrent 模式校验已选择 Torrent 文件且至少选择一个内部文件。
6. 校验错误显示在对应字段附近，并将焦点移动到第一个错误控件。
7. 异步读取本地 Torrent 时使用按钮 deferral，避免对话框提前关闭或重复提交。

验收标准：

- 多行 URL 输入框中 Enter 能正常换行。
- Ctrl+Enter 不隐式提交，除非后续明确将其设计为快捷键并显示对应语义。
- 空链接、空 Torrent、未选 Torrent 子文件时对话框保持打开。
- 修正输入后错误提示消失，可以正常提交。
- 双击或快速连续点击“添加”不会创建重复任务。

## 阶段 2：迁移为独立 XAML ContentDialog

1. 创建继承 `ContentDialog` 的 `NewDownloadDialog.xaml` 和 code-behind。
2. 将标题放入 `ContentDialog.Title`，不在内容区重复绘制页面级标题。
3. 使用 `SelectorBar`；若目标版本或键盘行为不合适，则使用同组 `RadioButton` 表示“链接 / Torrent”。
4. 用 VisualState 或绑定控制两种模式内容的显示，不在事件处理器中逐个修改前景色和指示条。
5. 使用 `TextBox`、`NumberBox`、`Button`、`ListView` 等标准控件及主题资源。
6. 删除 `Width=664/680` 等刚性内容宽度，改为 `MinWidth`、`MaxWidth` 和可伸缩列。
7. 保留必要的垂直滚动，但不在 `ListView` 外再套纵向 `ScrollViewer`。
8. 将硬编码圆角替换为 `ControlCornerRadius` 或 `OverlayCornerRadius`。
9. 将常规文本字号替换为 WinUI Typography 样式；仅为图标保留必要尺寸。
10. 删除动态 UI 工厂方法，保留纯数据解析帮助方法。

验收标准：

- 对话框在浅色、深色和高对比度主题下内容可读。
- 100%、150%、200% DPI 和系统文本放大时没有裁切或重叠。
- 主窗口较窄时对话框内容能收缩或滚动，不超出可用区域。
- 链接/Torrent 模式可通过鼠标、Tab、方向键和屏幕阅读器识别与切换。

## 阶段 3：建立 ViewModel 和提交边界

1. 创建 `NewDownloadDialogViewModel`，实现属性变更通知。
2. ViewModel 至少管理：
   - 当前任务类型。
   - URL/磁力链接文本。
   - 可选任务名称。
   - 下载目录。
   - 分片数。
   - Torrent 文件信息和子文件选择。
   - 校验错误与提交中状态。
3. XAML 对动态属性使用显式 `x:Bind Mode=OneWay/TwoWay`。
4. TextBox 的 TwoWay 绑定使用 `UpdateSourceTrigger=PropertyChanged`。
5. 创建 `NewDownloadDialogResult`，统一返回链接或 Torrent 请求，避免 `MainWindow` 读取对话框内部控件。
6. 将 URI 拆分、剪贴板文本规范化等纯逻辑迁移为无 UI 依赖的方法并增加单元测试。
7. `MainWindow.ShowNewDownloadDialogAsync` 缩减为：准备初始值、显示对话框、启动引擎、提交请求、显示最终状态。
8. 保持 `DownloadCoordinator` 为任务创建入口，不在对话框内直接操作 RPC。

验收标准：

- `MainWindow` 不再持有或操作新建下载对话框的 TextBox、Button、ListView 等具体控件。
- ViewModel 不引用 `Brush`、`Visibility`、`StorageFile`、Picker 或其他 UI 类型。
- 链接和 Torrent 请求都能由 `NewDownloadDialogResult` 完整表达。
- 纯校验和 URI 解析可以在不启动 WinUI 的情况下测试。

## 阶段 4：Torrent 列表、可访问性和自动化验证

1. 使用 `ListView` 和带 `x:DataType` 的 `DataTemplate` 显示 Torrent 文件。
2. 选择状态使用 TwoWay `x:Bind`，全选框正确处理选中、未选和不确定三态。
3. 为所有交互控件添加稳定的 `AutomationProperties.AutomationId`。
4. 为粘贴、浏览、打开 Torrent、清除 Torrent 等图标按钮添加 `AutomationProperties.Name` 和 Tooltip。
5. 确保错误提示可被屏幕阅读器发现，不仅依赖颜色表达。
6. 检查 Tab 顺序、初始焦点、Escape 取消、PrimaryButton 默认行为和拖放反馈。
7. 为以下核心流程添加 UI 自动化或批量 UI 测试：
   - 打开和取消对话框。
   - 空输入提交被阻止。
   - 单链接和多链接提交。
   - 粘贴剪贴板下载链接。
   - 选择、拖放和清除 Torrent。
   - Torrent 子文件全选、部分选择和无选择校验。
   - 修改目录和分片数。

验收标准：

- Accessibility Insights 基础扫描无新增严重错误。
- 所有图标按钮都有可读名称。
- 具有大量文件的 Torrent 不再通过清空并重建 `StackPanel` 刷新。
- 键盘能够完成整个新建下载流程。

## 实施顺序与提交建议

建议按以下顺序拆成独立提交，每一步都保持可构建：

1. `fix: keep new-download dialog open on validation errors`
2. `refactor: move new-download UI into XAML content dialog`
3. `refactor: add new-download view model and result contract`
4. `fix: virtualize torrent file list and complete accessibility metadata`
5. `test: cover new-download validation and keyboard flows`

每个提交完成后执行：

```powershell
dotnet build OmniDown.slnx
```

若 NuGet 因本机 Schannel `SEC_E_NO_CREDENTIALS` 失败，先按仓库 `AGENTS.md` 的网络诊断流程确认；不要通过修改项目依赖或永久关闭 NuGet Audit 规避。

## 回归检查清单

- Ctrl+N、工具栏按钮、协议激活、浏览器扩展和剪贴板监控仍进入同一新建下载流程。
- HTTP/HTTPS、磁力链接、ED2K、本地 Torrent 路径和 Torrent 文件选择行为保持一致。
- 自定义任务名、下载目录、分片数和 Torrent 子文件选择正确传递给 `DownloadCoordinator`。
- aria2 启动失败时不创建任务，并保留可理解的错误反馈。
- 添加多个链接时不会错误复用单链接任务名称。
- 对话框取消后不修改设置、不启动下载、不遗留事件订阅。
- 拖放非 Torrent 文件不会改变当前输入状态。
- 浅色、深色、高对比度和文本缩放下布局可用。

## 完成定义

- 解决方案构建为 0 错误。
- 新建下载 UI 不再由 `MainWindow` 动态创建。
- 所有提交前校验均在对话框关闭前完成。
- `MainWindow.NewDownloadDialog.xaml.cs` 只保留宿主协调职责，原动态控件工厂代码被删除。
- 核心校验具备单元测试，关键键盘和 Torrent 流程具备运行时验证记录。
- `git status --short` 中仅包含本次计划范围内的预期文件。
