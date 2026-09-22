# 发布任务栏日历（.NET Framework 4.8 版本：Win10/11 系统自带，用户无需安装任何运行时）
# 用法: powershell -ExecutionPolicy Bypass -File tools\publish.ps1
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\TaskbarCalendar\TaskbarCalendar.csproj'
$output = Join-Path $root 'publish'

if (Test-Path $output) { Remove-Item $output -Recurse -Force }
dotnet publish $project -c Release -o $output --nologo
if ($LASTEXITCODE -ne 0) {
    throw "发布失败，退出码 $LASTEXITCODE"
}

$size = (Get-ChildItem $output -Recurse -File | Measure-Object -Property Length -Sum).Sum
Write-Output ("发布完成: {0}（{1:N2} MB）" -f $output, ($size / 1MB))
