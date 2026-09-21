# 发布任务栏日历（框架依赖模式，目标机器需安装 .NET 8 Desktop Runtime）
# 用法: powershell -ExecutionPolicy Bypass -File tools\publish.ps1
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\TaskbarCalendar\TaskbarCalendar.csproj'
$output = Join-Path $root 'publish'

dotnet publish $project -c Release -r win-x64 --self-contained false -o $output --nologo
if ($LASTEXITCODE -ne 0) {
    throw "发布失败，退出码 $LASTEXITCODE"
}

Write-Output "发布完成: $output"
