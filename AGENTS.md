# AGENTS.md · 任务栏日历（Taskbar Calendar）

点击 Windows 11 任务栏时钟弹出自研日历的 WPF 应用。本文件面向在此仓库工作的 AI 代理与开发者。

## 项目概览

| 项 | 内容 |
| --- | --- |
| 目标系统 | Windows 11（24H2 实测），仅 x64 |
| 技术栈 | C# / .NET Framework 4.8 / WPF（`net48`，LangVersion 12），仅 System.Text.Json 一个 NuGet 依赖 |
| 发布形态 | 免安装（依赖 Win10/11 系统自带的 .NET Framework 4.8），zip 约 500KB / 解压约 1.7MB |
| 仓库 | https://github.com/IceCoder1994/taskbar-calendar（MIT） |
| 官网 | https://calendar.icewang.qzz.io/（`site/` 纯静态单页，Cloudflare Pages 托管，免备案） |
| 文档 | `docs/功能清单.md`（需求与变更记录）、`docs/使用说明.txt`、`README.md` |

## 常用命令

```powershell
dotnet build TaskbarCalendar.sln -c Debug                 # 构建
dotnet run --project src\TaskbarCalendar                  # 运行（托盘驻留）
dotnet run --project src\TaskbarCalendar -- --settings    # 直接打开设置窗口
powershell -ExecutionPolicy Bypass -File tools\publish.ps1      # 发布轻量版到 publish\
powershell -ExecutionPolicy Bypass -File tools\generate-icon.ps1 # 重新生成 app.ico
powershell -ExecutionPolicy Bypass -File tools\sync-site.ps1     # 同步官网下载包 + 页面版本号
powershell -ExecutionPolicy Bypass -File tools\generate-og-image.ps1 # 重新生成 OG 分享卡片
powershell -ExecutionPolicy Bypass -File tools\record-demo.ps1   # 录制官网演示 GIF（默认调用 D:\TaskbarCalendar 下的程序）
```

| 运行时文件 | 位置 |
| --- | --- |
| 日志 | `%LOCALAPPDATA%\TaskbarCalendar\logs\app-yyyyMMdd.log`（UTF-8） |
| 用户设置 | `%APPDATA%\TaskbarCalendar\settings.json` |
| 节假日数据 | 程序目录 `Data\holidays.json`（源文件在 `src\TaskbarCalendar\Data\`） |

## 项目结构

```
src/TaskbarCalendar/
├─ App.xaml(.cs)            # 入口：单实例、全局异常、托盘/覆盖层/自动同步生命周期
├─ Interop/
│  ├─ NativeMethods.cs      # 全部 Win32 P/Invoke 声明
│  └─ ClockLocator.cs       # UIA 定位任务栏时钟（后台线程 1 秒轮询 + 4 级兜底）
├─ Overlay/ClickOverlay.cs  # 纯 Win32 隐形点击层（拦截时钟左/右键）
├─ UI/
│  ├─ CalendarPopupWindow.* # 日历面板（月历/年月光选择器/详情栏/主题）
│  ├─ SettingsWindow.*      # 设置窗口
│  ├─ UpdateWindow.*        # 新版本检查与自动就地更新弹窗
│  └─ DayCell.cs            # 日期格数据模型（不可变，每次重建）
├─ Services/
│  ├─ AppInfo.cs            # 版本号反射读取与官网地址常量
│  ├─ UpdateService.cs      # 版本检查、流式下载、解压与后台批处理自覆盖重启
│  ├─ TrayService.cs        # 托盘图标与菜单
│  ├─ SettingsService.cs    # 设置持久化（单例）+ Changed 事件
│  ├─ AutoStartService.cs   # HKCU Run 开机自启
│  ├─ ThemeService.cs       # 系统深浅色 / 强调色读取
│  ├─ Logger.cs             # 按日归档文件日志
│  └─ AppPaths.cs           # 全部路径常量
├─ Calendar/
│  ├─ LunarService.cs       # 农历/节气/传统节日/干支（ChineseLunisolarCalendar）
│  └─ HolidayService.cs     # 节假日数据：本地 JSON + timor.tech 在线同步 + lastSync
├─ Compat/
│  └─ Net48Polyfills.cs     # 兼容层：IsExternalInit / required 特性 / MathCompat / KeyValuePair 解构
└─ GlobalUsings.cs          # 全局 using（导入兼容层命名空间）

site/                       # 官网静态单页（Cloudflare Pages 构建输出目录，无构建步骤）
├─ index.html               #   首页：下载入口 / 安装引导 / FAQ
├─ 404.html                 #   自定义错误页
├─ robots.txt、sitemap.xml  #   SEO 文件
├─ _headers                 #   Cloudflare Pages 缓存策略
├─ css/、js/、assets/       #   样式 / 脚本 / 图片（favicon、OG 卡片、界面截图）
└─ download/                #   官网直链下载包（由 tools\sync-site.ps1 生成）
```

## 核心链路（改动前务必理解）

1. `ClockLocator` 后台线程每秒用 UIA 读取任务栏时钟按钮矩形（多级兜底：类名候选 + 名称/位置特征 + 类名兜底，失败回退手动校准位置），变化才发事件；
2. `App` 应用设置中的偏移校准后交给 `ClickOverlay`，同时用 1 秒定时器反复置顶（防 explorer 重排 z-order）；
3. 用户点击 → 覆盖层收到 `WM_LBUTTONUP` → `ToggleCalendar()` 弹出/收起 `CalendarPopupWindow`；
4. 面板定位：时钟上方右对齐，物理像素计算（`SetWindowPos`），并 clamp 到工作区。

**设计红线**：

- 不使用全局鼠标钩子（杀软误报风险）；
- 点击层必须是纯 Win32 窗口（不得用 `HwndSource`，WPF 渲染会接管 layered 属性导致黑块）；
- 只覆盖时钟按钮矩形，不影响通知铃铛与原生时钟显示。

## 代码约定

- **所有注释必须用中文**（全局规则）；
- 提交信息遵循 Angular 规范 + 中文（`feat:` / `fix:` / `chore:` / `ci:`）；
- 新增 P/Invoke 一律放入 `Interop/NativeMethods.cs`，不在业务文件中声明；
- 设置项新增：`SettingsService.AppSettings` 加属性 + 设置窗口 UI + `ApplyFromUi`/`LoadCurrent` 两处接线；
- 面板数据（`DayCell`）采用"每次重建 42 个不可变对象"的模式，不要引入可变状态与 INotifyPropertyChanged。

## 已知陷阱（踩坑记录，改代码前必读）

1. **PowerShell 5.1 中文脚本必须 UTF-8 with BOM**：`tools/*.ps1` 修改后若无 BOM，中文注释会按 GBK 误读导致语法错乱。修复：
   ```powershell
   $p = (Resolve-Path 'tools\xxx.ps1').Path
   $c = [System.IO.File]::ReadAllText($p, [System.Text.Encoding]::UTF8)
   [System.IO.File]::WriteAllText($p, $c, (New-Object System.Text.UTF8Encoding $true))
   ```
2. **.NET Framework 下 `System.Net.Http` 必须显式引用**：csproj 已加 `<Reference Include="System.Net.Http" />`（net8 由框架自带）；`System.IO` 由 ImplicitUsings 提供，但 UIAutomation 相关类型需见陷阱 5；
3. **`UseWindowsForms` 注入全局 using 导致同名类型冲突**，本项目已用别名消歧：`Application`、`MessageBox`、`Button`、`Brush`、`Brushes`、`Color`、`KeyEventArgs`、`MouseWheelEventArgs`、`KeyboardFocusChangedEventArgs`、`Cursor`。新增文件遇到 `CS0104` 先加别名；
4. **点击层的透明与光标**：`SetLayeredWindowAttributes(alpha=1)`（alpha=0 会穿透点击）；窗口类必须 `hCursor=IDC_ARROW` 且处理 `WM_SETCURSOR`，否则光标会"冻结"在进入前的状态（如转圈）；
5. **UIAutomation 引用方式随 TFM 不同**：`net48` 必须在 csproj 显式 `<Reference Include="UIAutomationClient" />` 与 `UIAutomationTypes`（缺失报 CS0234）；`net8.0-windows` 下由 WPF 框架自带，裸 `<Reference>` 会 MSB3245；
6. **节假日 JSON 序列化**：读取大小写不敏感；写出用 camelCase + `UnsafeRelaxedJsonEscaping`；设置文件枚举用 `JsonStringEnumConverter`（否则字符串枚举解析失败回落默认值）；
7. **托盘图标优先从 exe 提取**：`Icon.ExtractAssociatedIcon(Process.GetCurrentProcess().MainModule?.FileName)`（net48 无 `Environment.ProcessPath` 可用），失败时才回退 `Assets/app.ico`；
8. **发布被文件锁定**：本地覆盖 `publish\` 前必须退出正在运行的实例（`Get-Process TaskbarCalendar` 确认）；CI（Actions）不受影响；
9. **单实例 Mutex** 为 `Global\TaskbarCalendar.SingleInstance`：自动化测试启动新实例前先杀掉旧实例，否则弹"已在运行"对话框阻塞脚本；
10. **节气算法**为寿星公式（21 世纪），个别年份可能 ±1 天；发现偏差需查证后修正 `LunarService.SolarTermConstants` 或加例外表。
11. **任务栏时钟类名随系统版本变化**（24H2 = `SystemTray.OmniButtonLeft`、25H2 = `SystemTray.OmniButton`，且类名可能被多个按钮共用）：通用兜底是"名称含时间/日期特征 + 最靠右位置"（与类名、语言无关），类名匹配必须逐候选校验名称；适配新系统时更新 `ClockClassNames`，并利用用户日志中的"任务栏结构诊断快照"定位问题；无法自动适配时可引导用户使用设置中的手动校准（5 秒取点）。
12. **官网（`site/`）发版必须同步**：`site/download/` 内的 zip 与 `index.html` / `404.html` 中的版本号不会自动更新，发版前必须运行 `tools\sync-site.ps1`，否则官网下载到的仍是旧版本；`tools\sync-site.ps1` 与 `tools\generate-og-image.ps1` 均为含中文的 UTF-8 with BOM 脚本（见陷阱 1）。
13. **官网下载包依赖 .gitignore 例外**：根 `.gitignore` 有 `*.zip` 规则，`site/download/` 通过 `!site/download/*.zip` 例外交付；新增站点二进制资源时注意同样处理。
14. **Cloudflare 上 `*.pages.dev` 共享域在国内不稳定**：对外一律使用自定义域 `calendar.icewang.qzz.io`；页面内资源用相对路径（`assets/...`），404 页用绝对路径（`/assets/...`）以兼容任意深度路径。
15. **演示 GIF 录制有隐式依赖**：`tools\record-demo.ps1` 会强制重启 `D:\TaskbarCalendar\TaskbarCalendar.exe`（可用 `-ExePath` 覆盖）并真实控制鼠标约 7 秒；面板内元素坐标基于 360×460 面板与 UIA 实测（`record_demo.py` 顶部注释），时钟坐标依赖日志格式 `时钟位置更新: (左,上)-(右,下)`；改动面板布局、尺寸或日志格式后需同步更新脚本，否则会点到错误位置。
16. **net48 兼容层与 API 红线**：`Compat/Net48Polyfills.cs` 提供 `IsExternalInit`、`RequiredMemberAttribute` 等编译器标记类型（支撑 record / init / required），以及 `MathCompat.Clamp` 与 `KeyValuePair` 解构扩展；业务代码不得直接使用 .NET Core 专有 API —— `Environment.ProcessPath` → `Process.GetCurrentProcess().MainModule?.FileName`、`str.AsSpan(n, len)` → `Substring(n, len)`、`name[..n]` → `Substring(0, n)`、`Math.Clamp` → `MathCompat.Clamp`、`KeyValuePair` 解构依赖兼容层扩展。
17. **net48 下节假日同步必须显式开启 TLS 1.2**：`App.OnStartup` 中的 `ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12` 不可删除，否则 HTTPS 请求全部失败（表现为节假日自动同步报错）。
18. **发布包名自 v1.1.0 起不含 `-lite`**：下载包为 `TaskbarCalendar-v{版本}-win-x64.zip`；CI 与本地脚本均不使用 `--self-contained` / `PublishSingleFile`（net48 不支持），产物为 exe + 若干依赖 dll 的目录（约 1.7MB，zip 约 500KB）。

## 测试方式（无单元测试）

验证靠"构建 + 运行 + 日志断言 + UIA 模拟点击 + 截图"：

1. 启动 → 读日志确认 `托盘图标已就绪` / `隐形点击层已创建` / `时钟位置更新: (左,上)-(右,下)`；
2. 用 PowerShell `UIAutomationClient` 获取时钟矩形中心 → `SetCursorPos` + `mouse_event` 点击 → 日志出现 `日历面板定位` 即链路正常；
3. 截图用 `System.Drawing` + `CopyFromScreen`（面板尺寸 360×460 物理像素，100% 缩放）；
4. 验证设置生效：写 `%APPDATA%\TaskbarCalendar\settings.json` 后重启程序（如周日起始/浅色主题）；
5. 测试结束：杀进程并恢复被改动的数据/设置文件。

## 发布流程

```powershell
# 1. 改版本号与变更记录
#    src\TaskbarCalendar\TaskbarCalendar.csproj 的 <Version>
#    docs\功能清单.md 变更记录追加一行
# 2. 同步官网站点（下载包 + 页面版本号 + sitemap 日期）
powershell -ExecutionPolicy Bypass -File tools\sync-site.ps1
# 3. 提交推送（site/ 改动推送后由 Cloudflare Pages 自动部署）
git add -A; git commit -m "feat: ..."; git push
# 4. 打标签触发自动发版（GitHub Actions: .github/workflows/release.yml）
git tag v1.0.2; git push origin v1.0.2
# 5. 验证（3-5 分钟后）
gh release view v1.0.2
```

工作流会构建 .NET Framework 4.8 版本（用户免安装）、打包 `TaskbarCalendar-v*-win-x64.zip` 并自动发布 Release。

## 外部依赖与数据

- 节假日数据接口：`https://timor.tech/api/holiday/year/{year}`（数据与国务院公告核对一致）；
- 数据更新策略：启动 10 秒后首查 + 每 24 小时复检（`MaybeAutoSyncAsync`），`lastSync` 记录在数据文件中；未公布年份视为"已检查"不重复请求；
- 农历支持范围 1901-2100（`ChineseLunisolarCalendar` 限制）。
