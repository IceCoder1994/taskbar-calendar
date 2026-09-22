# 录制日历面板演示 GIF（全自动流程）
# 1) 重启程序保证面板处于关闭状态；2) 从日志解析时钟矩形并计算抓屏区域；
# 3) 调用 record_demo.py 模拟操作并抓屏；4) 调用 build_gif.py 合成 GIF。
#
# 用法: powershell -ExecutionPolicy Bypass -File tools\record-demo.ps1
# 注意: 录制期间脚本会真实控制鼠标（约 7 秒），请勿操作电脑。
param(
    [string]$ExePath = 'D:\TaskbarCalendar\TaskbarCalendar.exe',
    [int]$Fps = 10,
    [double]$Scale = 1.0,
    [int]$Colors = 128,
    [switch]$KeepFrames
)
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms

$root = Split-Path -Parent $PSScriptRoot
$framesDir = Join-Path $env:TEMP 'taskbar-calendar-frames'
$outGif = Join-Path $root 'site\assets\demo-dark.gif'
$logFile = Join-Path $env:LOCALAPPDATA "TaskbarCalendar\logs\app-$(Get-Date -Format 'yyyyMMdd').log"

if (-not (Test-Path $ExePath)) {
    throw "未找到程序: $ExePath（可用 -ExePath 指定）"
}

# ---------- 1. 重启程序，保证初始状态为"面板已关闭" ----------
Get-Process TaskbarCalendar -ErrorAction SilentlyContinue | Stop-Process -Force
Start-Sleep -Milliseconds 800
Write-Output '启动程序...'
Start-Process $ExePath | Out-Null

# 等待日志出现时钟定位记录（最多 12 秒）
$clockRect = $null
$deadline = (Get-Date).AddSeconds(12)
while ((Get-Date) -lt $deadline) {
    Start-Sleep -Milliseconds 500
    if (-not (Test-Path $logFile)) { continue }
    $lines = Get-Content $logFile -Encoding UTF8 -Tail 40
    $hit = $lines | Select-String -Pattern '时钟位置更新: \((\d+),(\d+)\)-\((\d+),(\d+)\)' | Select-Object -Last 1
    if ($hit) {
        $g = $hit.Matches[0].Groups
        $clockRect = @{
            Left = [int]$g[1].Value; Top = [int]$g[2].Value
            Right = [int]$g[3].Value; Bottom = [int]$g[4].Value
        }
        break
    }
}
if (-not $clockRect) { throw '等待时钟定位超时，请检查程序日志' }
Write-Output ("时钟矩形: ({0},{1})-({2},{3})" -f $clockRect.Left, $clockRect.Top, $clockRect.Right, $clockRect.Bottom)

# ---------- 2. 计算面板位置与抓屏区域 ----------
# 面板右对齐时钟右边缘、位于时钟上方 8px 处，尺寸固定 360x460
$panelW = 360
$panelH = 460
$panelRight = $clockRect.Right
$panelBottom = $clockRect.Top - 8
$panelLeft = $panelRight - $panelW
$panelTop = $panelBottom - $panelH
Write-Output ("面板位置: ({0},{1}) {2}x{3}" -f $panelLeft, $panelTop, $panelW, $panelH)

$screenW = [int]([System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Width)
$screenH = [int]([System.Windows.Forms.Screen]::PrimaryScreen.Bounds.Height)
$padX = 20
$padTop = 20
$grabLeft = [Math]::Max(0, $panelLeft - $padX)
$grabTop = [Math]::Max(0, $panelTop - $padTop)
$grabRight = [Math]::Min($screenW, $panelRight + $padX)
$grabBottom = $screenH
Write-Output ("抓屏区域: ({0},{1})-({2},{3})  {4}x{5}" -f $grabLeft, $grabTop, $grabRight, $grabBottom, ($grabRight - $grabLeft), ($grabBottom - $grabTop))

# ---------- 3. 录制（Python 模拟操作 + 抓屏） ----------
$recordPy = Join-Path $PSScriptRoot 'record_demo.py'
Write-Output '开始录制（请勿操作鼠标键盘）...'
python $recordPy --left $grabLeft --top $grabTop --right $grabRight --bottom $grabBottom `
    --panel-left $panelLeft --panel-top $panelTop --fps $Fps --outdir $framesDir `
    --clock-x ([int](($clockRect.Left + $clockRect.Right) / 2)) `
    --clock-y ([int](($clockRect.Top + $clockRect.Bottom) / 2))
if ($LASTEXITCODE -ne 0) { throw "录制失败，退出码 $LASTEXITCODE" }

# ---------- 4. 合成 GIF ----------
$buildPy = Join-Path $PSScriptRoot 'build_gif.py'
python $buildPy --indir $framesDir --out $outGif --fps $Fps --scale $Scale --colors $Colors `
    --cursor (Join-Path $framesDir 'cursor.txt')
if ($LASTEXITCODE -ne 0) { throw "GIF 合成失败，退出码 $LASTEXITCODE" }

if (-not $KeepFrames) {
    Remove-Item $framesDir -Recurse -Force -ErrorAction SilentlyContinue
    Write-Output '中间帧已清理'
}

Write-Output ''
Write-Output "完成！GIF: $outGif"
