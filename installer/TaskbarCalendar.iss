; ============================================================================
; 任务栏日历（Taskbar Calendar）· Inno Setup 安装脚本
; 策略：每用户安装（PrivilegeRequired=lowest，免 UAC），
;       默认安装到 %LOCALAPPDATA%\Programs\TaskbarCalendar（用户可写，程序自更新继续可用）
;
; 编译方式（版本号由调用方从 csproj 读取后以 /D 注入，避免两处硬编码）：
;   iscc /Q "/DMyAppVersion=1.2.0" "/O<输出目录>" installer\TaskbarCalendar.iss
;
; 注意：本文件含中文注释，必须保持 UTF-8 with BOM，
;       否则 Inno Setup 编译器会按 GBK 误读导致语法错乱（参见 AGENTS.md 陷阱 1）
; ============================================================================

#ifndef MyAppVersion
  ; 未通过 /DMyAppVersion= 传入时给出占位版本，提醒构建脚本维护者
  #define MyAppVersion "0.0.0"
#endif

#define MyAppName "任务栏日历"
#define MyAppExeName "TaskbarCalendar.exe"
#define MyAppDirName "TaskbarCalendar"
#define MyAppPublisher "IceCoder1994"
#define MyAppURL "https://calendar.icewang.qzz.io/"

; 被安装的应用产物目录：由发布脚本以 /DSourceDir 注入（publish.ps1 传 publish/，
; sync-site.ps1 与 CI 传 src/... 发布输出目录），默认回退 dist/（兼容直接命令行编译）
#ifndef SourceDir
  #define SourceDir = "..\dist"
#endif

; 兼容旧变量名（历史注释与文档中的 DistDir 指向 SourceDir）
#define DistDir SourceDir

[Setup]
AppId=TaskbarCalendar
AppName={#MyAppName}
AppVersion={#MyAppVersion}
AppVerName={#MyAppName} v{#MyAppVersion}
VersionInfoVersion={#MyAppVersion}
VersionInfoCompany={#MyAppPublisher}
VersionInfoDescription={#MyAppName} 安装程序
VersionInfoTextVersion={#MyAppVersion}
VersionInfoCopyright=Copyright (C) 2026 {#MyAppPublisher}
AppPublisher={#MyAppPublisher}
AppPublisherURL={#MyAppURL}
AppSupportURL={#MyAppURL}
AppUpdatesURL={#MyAppURL}
; 每用户安装：无需管理员权限，安装/卸载全程无 UAC 弹窗
DefaultDirName={localappdata}\Programs\{#MyAppDirName}
DefaultGroupName={#MyAppName}
DisableProgramGroupPage=yes
AllowNoIcons=yes
; 不使用 AppMutex：它会在静默安装遇到程序在运行时直接 Abort（消息框被抑制后取消），
; 关闭占用进程的职责交给下面 CloseApplications=force
LicenseFile=license.rtf
; 输出目录由命令行 /O 覆盖
OutputDir=.
; 产物名：TaskbarCalendar-v{版本}-win-x64-Setup.exe（与官网下载规则一致）
OutputBaseFilename=TaskbarCalendar-v{#MyAppVersion}-win-x64-Setup
SetupIconFile=..\src\TaskbarCalendar\Assets\app.ico
Compression=lzma2/max
SolidCompression=yes
WizardStyle=modern
PrivilegesRequired=lowest
ArchitecturesAllowed=x64compatible
UninstallDisplayName={#MyAppName}
UninstallDisplayIcon={app}\{#MyAppExeName}
UninstallDisplaySize=1800
; 安装/升级时强制关闭占用安装目录的进程（交互界面提示，静默时自动完成）。
; force 是必需的：默认 yes 依赖 Windows Restart Manager 发 WM_CLOSE，
; 托盘程序收到关闭消息只会隐藏窗体而不退出，导致后续删除文件失败。
; Filter 精确匹配本程序可执行文件名，避免误匹配系统进程
CloseApplications=force
CloseApplicationsFilter=TaskbarCalendar.exe
; 不自动重启被关闭的应用：由程序的 UpdateService 在安装器退出后负责重启
RestartApplications=no

[Languages]
; 简体中文语言文件：Inno Setup 6.4+ 不再随主分发包附带中文翻译，
; 从 issrc 官方仓库获取后随本 install 目录提交（CI 与本地编译一致）
Name: chinesesimplified; MessagesFile: "Languages\ChineseSimplified.isl"

[Tasks]
Name: desktopicon; Description: "创建桌面快捷方式"; GroupDescription: "附加图标:"; Flags: unchecked

[Files]
; 打包 dist/ 全部产物（含 Data\holidays.json、使用说明.txt）
Source: "{#DistDir}\*"; DestDir: "{app}"; Flags: ignoreversion recursesubdirs createallsubdirs; \
  Excludes: "*.pdb,*.xml"

[Icons]
; 开始菜单
Name: "{group}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Comment: "点击任务栏时钟弹出日历"
Name: "{group}\{#MyAppName}设置"; Filename: "{app}\{#MyAppExeName}"; Parameters: "--settings"
Name: "{group}\使用说明"; Filename: "{app}\使用说明.txt"
Name: "{group}\卸载 {#MyAppName}"; Filename: "{uninstallexe}"
; 桌面（可选任务）
Name: "{autodesktop}\{#MyAppName}"; Filename: "{app}\{#MyAppExeName}"; Tasks: desktopicon

[Run]
; 安装完成页的「立即运行」复选框：postinstall 标志使 Inno 在完成页生成勾选项，默认勾选
Filename: "{app}\{#MyAppExeName}"; Description: "立即运行{#MyAppName}"; Flags: nowait skipifsilent postinstall

[UninstallDelete]
; 卸载时移除整个安装目录（用户数据在 %APPDATA%/%LOCALAPPDATA%，不受影响）
Type: filesandordirs; Name: "{app}"

[Registry]
; 每用户卸载信息（写入 HKCU，设置→应用 中可见，uninsdeletekey 使卸载时自动清理）
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaskbarCalendar_is1"; \
  ValueType: string; ValueName: "DisplayName"; ValueData: "{#MyAppName}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaskbarCalendar_is1"; \
  ValueType: string; ValueName: "DisplayVersion"; ValueData: "{#MyAppVersion}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaskbarCalendar_is1"; \
  ValueType: string; ValueName: "Publisher"; ValueData: "{#MyAppPublisher}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaskbarCalendar_is1"; \
  ValueType: string; ValueName: "DisplayIcon"; ValueData: "{app}\{#MyAppExeName},0"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaskbarCalendar_is1"; \
  ValueType: string; ValueName: "URLInfoAbout"; ValueData: "{#MyAppURL}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaskbarCalendar_is1"; \
  ValueType: string; ValueName: "InstallLocation"; ValueData: "{app}"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaskbarCalendar_is1"; \
  ValueType: string; ValueName: "UninstallString"; ValueData: """{uninstallexe}"""; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaskbarCalendar_is1"; \
  ValueType: string; ValueName: "QuietUninstallString"; ValueData: """{uninstallexe}"" /SILENT"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaskbarCalendar_is1"; \
  ValueType: dword; ValueName: "NoModify"; ValueData: "1"; Flags: uninsdeletekey
Root: HKCU; Subkey: "Software\Microsoft\Windows\CurrentVersion\Uninstall\TaskbarCalendar_is1"; \
  ValueType: dword; ValueName: "NoRepair"; ValueData: "1"; Flags: uninsdeletekey

[Code]

const
  { 开机自启注册表位置（与 AutoStartService 常量保持一致，卸载时必须清理） }
  RunRegKeyPath = 'Software\Microsoft\Windows\CurrentVersion\Run';
  RunValueName = 'TaskbarCalendar';
  { 本程序进程名（卸载时若仍在运行需先结束，否则安装目录文件被占用删不掉） }
  AppProcessName = 'TaskbarCalendar.exe';

{ 卸载前结束仍在运行的实例。
  Inno 的 CloseApplications=force 只覆盖安装器主流程，静默卸载不会自动关闭进程，
  因此卸载器在真正删除文件前主动 taskkill /f，并轮询等待进程消失。
  taskkill 由系统按进程名匹配，无需 Pascal Script 自行枚举进程
  （枚举方案对 Unicode 进程名兼容性差，实测漏判）。 }
function InitializeUninstall(): Boolean;
var
  ResultCode: Integer;
begin
  Result := True;
  Exec(ExpandConstant('{sys}\taskkill.exe'), '/f /im ' + AppProcessName,
    ExpandConstant('{sys}'), SW_HIDE, ewWaitUntilTerminated, ResultCode);

  { 等待进程真正退出：WPF 走 OnExit 优雅退出并释放句柄需要数秒，
     若不等就删目录会残留文件（表现为卸载后目录里还有 exe/dll） }
  while CheckForMutexes('Global\TaskbarCalendar.SingleInstance') do
  begin
    Exec(ExpandConstant('{sys}\timeout.exe'), '/t 1 /nobreak',
      ExpandConstant('{sys}'), SW_HIDE, ewWaitUntilTerminated, ResultCode);
  end;
end;

{ 卸载后清理：删除开机自启注册表值（用户可能在设置里开启过自启，
  不清会导致卸载后开机仍尝试启动一个已不存在的路径）。 }
procedure CurUninstallStepChanged(CurUninstallStep: TUninstallStep);
begin
  if CurUninstallStep = usPostUninstall then
  begin
    RegDeleteValue(HKCU, RunRegKeyPath, RunValueName);
  end;
end;
