# 发布任务栏日历（.NET Framework 4.8 版本：Win10/11 系统自带，用户无需安装任何运行时）
# 用法: powershell -ExecutionPolicy Bypass -File tools\publish.ps1
# 说明: 发布产物为 Inno Setup 安装器（每用户安装，免 UAC）；
#       本机需安装 Inno Setup 6（http://www.jrsoftware.org/isdinfo.php），
#       未安装时自动退化为仅输出发布目录，不影响 CI（CI 由 choco 安装）。
# 注意: 本脚本含中文，必须保持 UTF-8 with BOM（见 AGENTS.md 陷阱 1）
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\TaskbarCalendar\TaskbarCalendar.csproj'
$issFile = Join-Path $root 'installer\TaskbarCalendar.iss'
$output = Join-Path $root 'publish'

if (Test-Path $output) { Remove-Item $output -Recurse -Force }
dotnet publish $project -c Release -o $output --nologo
if ($LASTEXITCODE -ne 0) {
    throw "发布失败，退出码 $LASTEXITCODE"
}

# 附带使用说明到发布目录（安装器一并打包）
$manualDoc = Join-Path $root 'docs\使用说明.txt'
if (Test-Path $manualDoc) {
    Copy-Item $manualDoc (Join-Path $output '使用说明.txt') -Force
}

# 从 csproj 读取版本号，注入 Inno Setup 编译（避免两处硬编码）
[xml]$proj = Get-Content $project
$version = $proj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) {
    throw "未能从 $project 读取 Version 节点"
}
$tag = "v$version"

# 查找 Inno Setup 编译器（优先进 PATH，其次常见安装路径）
$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    foreach ($candidate in @(
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe")) {
        if (Test-Path $candidate) { $iscc = Get-Command $candidate; break }
    }
}

if ($iscc) {
    # 编译安装器到 publish 目录（注入产物目录，避免 ISS 内硬编码相对路径）
    & $iscc.Source "/Q" "/DMyAppVersion=$version" "/DSourceDir=$output" "/O$output" $issFile | Out-Null
    if ($LASTEXITCODE -ne 0) {
        throw "Inno Setup 编译失败，退出码 $LASTEXITCODE"
    }
    $setup = Join-Path $output "TaskbarCalendar-$tag-win-x64-Setup.exe"
    if (Test-Path $setup) {
        Write-Output ("安装器已生成: {0}（{1:N0} KB）" -f $setup, ((Get-Item $setup).Length / 1KB))
    }
} else {
    Write-Warning '未检测到 Inno Setup，已跳过安装器编译（仅输出发布目录）。安装 Inno Setup 6 后重试即可。'
}

$size = (Get-ChildItem $output -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Output ("发布完成: {0}（{1:N2} MB）" -f $output, ($size / 1MB))
