# 从应用图标 app.ico 提取并生成网页图标（favicon.ico / favicon-32.png / apple-touch-icon.png）
# 用法: powershell -ExecutionPolicy Bypass -File tools\generate-favicon.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName PresentationCore

$root = Split-Path -Parent $PSScriptRoot
$srcIco = Join-Path $root 'src\TaskbarCalendar\Assets\app.ico'
$assetsDir = Join-Path $root 'site\assets'

$decoder = New-Object System.Windows.Media.Imaging.IconBitmapDecoder (
    $srcIco,
    [System.Windows.Media.Imaging.BitmapCreateOptions]::PreservePixelFormat,
    [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad)

# 收集原始各尺寸帧
$frames = @{}
foreach ($f in $decoder.Frames) { $frames[[int]$f.PixelWidth] = $f }
Write-Output ('源图标帧: ' + (($frames.Keys | Sort-Object) -join ', '))

function Save-PngFrame($frame, $path) {
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($frame))
    $fs = [System.IO.File]::Create($path)
    try { $encoder.Save($fs) } finally { $fs.Close() }
}

function Get-PngBytes($frame) {
    $encoder = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($frame))
    $ms = New-Object System.IO.MemoryStream
    $encoder.Save($ms)
    $bytes = $ms.ToArray()
    $ms.Dispose()
    return $bytes
}

# 1) 32x32 PNG（现代浏览器优先使用）
Save-PngFrame $frames[32] (Join-Path $assetsDir 'favicon-32.png')

# 2) iOS 主屏图标 180x180（由 256 帧等比缩小）
$scale = 180.0 / 256.0
$transformed = New-Object System.Windows.Media.Imaging.TransformedBitmap(
    $frames[256],
    (New-Object System.Windows.Media.ScaleTransform($scale, $scale)))
Save-PngFrame $transformed (Join-Path $assetsDir 'apple-touch-icon.png')

# 3) favicon.ico：16/32/48 三帧，采用 PNG 压缩帧（Vista+ 与现代浏览器均支持）
$sizes = @(16, 32, 48)
$pngs = New-Object 'System.Collections.Generic.List[byte[]]'
foreach ($s in $sizes) { $pngs.Add((Get-PngBytes $frames[$s])) }

$outIco = Join-Path $assetsDir 'favicon.ico'
$fs = [System.IO.File]::Create($outIco)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)
$bw.Write([UInt16]1)
$bw.Write([UInt16]$sizes.Count)
$offset = 6 + 16 * $sizes.Count
for ($i = 0; $i -lt $sizes.Count; $i++) {
    $data = $pngs[$i]
    $bw.Write([Byte]$sizes[$i])
    $bw.Write([Byte]$sizes[$i])
    $bw.Write([Byte]0)
    $bw.Write([Byte]0)
    $bw.Write([UInt16]1)
    $bw.Write([UInt16]32)
    $bw.Write([UInt32]$data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $data.Length
}
foreach ($d in $pngs) { $bw.Write([Byte[]]$d) }
$bw.Close()
$fs.Close()

Write-Output ('favicon.ico        : ' + (Get-Item $outIco).Length + ' bytes')
Write-Output ('favicon-32.png     : ' + (Get-Item (Join-Path $assetsDir 'favicon-32.png')).Length + ' bytes')
Write-Output ('apple-touch-icon   : ' + (Get-Item (Join-Path $assetsDir 'apple-touch-icon.png')).Length + ' bytes')
