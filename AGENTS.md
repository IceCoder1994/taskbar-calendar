# AGENTS.md · 任务栏日历（Taskbar Calendar）

点击 Windows 11 任务栏时钟弹出自研日历的 WPF 应用。本文件面向在此仓库工作的 AI 代理与开发者。

## 项目概览

| 项 | 内容 |
| --- | --- |
| 目标系统 | Windows 11（24H2 实测），仅 x64 |
| 技术栈 | C# / .NET 8 / WPF（`net8.0-windows`），零第三方 NuGet 依赖 |
| 发布形态 | 轻量版（框架依赖 .NET 8 Desktop Runtime），约 130KB zip |
| 仓库 | https://github.com/IceCoder1994/taskbar-calendar（MIT） |
| 文档 | `docs/功能清单.md`（需求与变更记录）、`docs/使用说明.txt`、`README.md` |

## 常用命令

```powershell
dotnet build TaskbarCalendar.sln -c Debug                 # 构建
dotnet run --project src\TaskbarCalendar                  # 运行（托盘驻留）
dotnet run --project src\TaskbarCalendar -- --settings    # 直接打开设置窗口
powershell -ExecutionPolicy Bypass -File tools\publish.ps1      # 发布轻量版到 publish\
powershell -ExecutionPolicy Bypass -File tools\generate-icon.ps1 # 重新生成 app.ico
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
│  └─ DayCell.cs            # 日期格数据模型（不可变，每次重建）
├─ Services/
│  ├─ TrayService.cs        # 托盘图标与菜单
│  ├─ SettingsService.cs    # 设置持久化（单例）+ Changed 事件
│  ├─ AutoStartService.cs   # HKCU Run 开机自启
│  ├─ ThemeService.cs       # 系统深浅色 / 强调色读取
│  ├─ Logger.cs             # 按日归档文件日志
│  └─ AppPaths.cs           # 全部路径常量
└─ Calendar/
   ├─ LunarService.cs       # 农历/节气/传统节日/干支（ChineseLunisolarCalendar）
   └─ HolidayService.cs     # 节假日数据：本地 JSON + timor.tech 在线同步 + lastSync
```

## 核心链路（改动前务必理解）

1. `ClockLocator` 后台线程每秒用 UIA 读取任务栏时钟按钮矩形（`SystemTray.OmniButtonLeft`），变化才发事件；
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
2. **WPF 项目的 ImplicitUsings 不含 `System.IO` 与 `System.Net.Http`**，使用需显式 `using`；
3. **`UseWindowsForms` 注入全局 using 导致同名类型冲突**，本项目已用别名消歧：`Application`、`MessageBox`、`Button`、`Brush`、`Brushes`、`Color`、`KeyEventArgs`、`MouseWheelEventArgs`、`KeyboardFocusChangedEventArgs`、`Cursor`。新增文件遇到 `CS0104` 先加别名；
4. **点击层的透明与光标**：`SetLayeredWindowAttributes(alpha=1)`（alpha=0 会穿透点击）；窗口类必须 `hCursor=IDC_ARROW` 且处理 `WM_SETCURSOR`，否则光标会"冻结"在进入前的状态（如转圈）；
5. **UIAutomationClient 由 WPF 框架自带**，不要添加 NuGet 或裸 `<Reference>`（会 MSB3245）；
6. **节假日 JSON 序列化**：读取大小写不敏感；写出用 camelCase + `UnsafeRelaxedJsonEscaping`；设置文件枚举用 `JsonStringEnumConverter`（否则字符串枚举解析失败回落默认值）；
7. **单文件发布下 `Assets/app.ico` 可能不外置**：托盘图标优先用 `Icon.ExtractAssociatedIcon(ProcessPath)` 从 exe 提取；
8. **发布被文件锁定**：本地覆盖 `publish\` 前必须退出正在运行的实例（`Get-Process TaskbarCalendar` 确认）；CI（Actions）不受影响；
9. **单实例 Mutex** 为 `Global\TaskbarCalendar.SingleInstance`：自动化测试启动新实例前先杀掉旧实例，否则弹"已在运行"对话框阻塞脚本；
10. **节气算法**为寿星公式（21 世纪），个别年份可能 ±1 天；发现偏差需查证后修正 `LunarService.SolarTermConstants` 或加例外表。

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
# 2. 提交推送
git add -A; git commit -m "feat: ..."; git push
# 3. 打标签触发自动发版（GitHub Actions: .github/workflows/release.yml）
git tag v1.0.2; git push origin v1.0.2
# 4. 验证（3-5 分钟后）
gh release view v1.0.2
```

工作流会构建轻量版（框架依赖 + 单文件）、打包 `TaskbarCalendar-v*-lite-win-x64.zip` 并自动发布 Release。

## 外部依赖与数据

- 节假日数据接口：`https://timor.tech/api/holiday/year/{year}`（数据与国务院公告核对一致）；
- 数据更新策略：启动 10 秒后首查 + 每 24 小时复检（`MaybeAutoSyncAsync`），`lastSync` 记录在数据文件中；未公布年份视为"已检查"不重复请求；
- 农历支持范围 1901-2100（`ChineseLunisolarCalendar` 限制）。
