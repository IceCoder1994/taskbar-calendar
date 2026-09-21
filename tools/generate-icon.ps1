# 生成任务栏日历应用图标（多尺寸 ICO，内嵌 PNG）
# 用法: powershell -ExecutionPolicy Bypass -File tools\generate-icon.ps1
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# 用矩形 + 圆形拼接绘制圆角矩形（规避 GraphicsPath.AddArc 在部分环境下的兼容问题）
function Fill-RoundedRect {
    param(
        [System.Drawing.Graphics]$Graphics,
        [System.Drawing.Brush]$Brush,
        [single]$X,
        [single]$Y,
        [single]$W,
        [single]$H,
        [single]$R
    )

    $d = [single]($R * 2)
    # 中间区域（十字）
    $Graphics.FillRectangle($Brush, ($X + $R), $Y, ($W - $d), $H)
    $Graphics.FillRectangle($Brush, $X, ($Y + $R), $W, ($H - $d))
    # 四个圆角
    $Graphics.FillEllipse($Brush, $X, $Y, $d, $d)
    $Graphics.FillEllipse($Brush, ($X + $W - $d), $Y, $d, $d)
    $Graphics.FillEllipse($Brush, $X, ($Y + $H - $d), $d, $d)
    $Graphics.FillEllipse($Brush, ($X + $W - $d), ($Y + $H - $d), $d, $d)
}

function New-IconBitmap {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = [single]$Size
    $blueBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 0, 103, 192))
    $blueLightBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 40, 130, 210))
    $whiteBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

    # 背景圆角方块
    Fill-RoundedRect -Graphics $g -Brush $blueBrush -X 0 -Y 0 -W $s -H $s -R ($s * 0.22)

    # 日历白色页面
    $px = $s * 0.17
    $py = $s * 0.21
    $pw = $s * 0.66
    $ph = $s * 0.60
    $pageR = $s * 0.08
    Fill-RoundedRect -Graphics $g -Brush $whiteBrush -X $px -Y $py -W $pw -H $ph -R $pageR

    # 页面顶部标题条（上圆角，下直角）
    $headH = $s * 0.12
    Fill-RoundedRect -Graphics $g -Brush $blueLightBrush -X $px -Y $py -W $pw -H $headH -R $pageR
    $g.FillRectangle($blueLightBrush, $px, ($py + $headH - $pageR), $pw, $pageR)

    # 装订环
    $ringW = $s * 0.07
    $ringH = $s * 0.15
    $ringY = $s * 0.11
    Fill-RoundedRect -Graphics $g -Brush $whiteBrush -X ($s * 0.33) -Y $ringY -W $ringW -H $ringH -R ($ringW / 2)
    Fill-RoundedRect -Graphics $g -Brush $whiteBrush -X ($s * 0.60) -Y $ringY -W $ringW -H $ringH -R ($ringW / 2)

    # 日期圆点（3 列 x 2 行）
    $dotR = $s * 0.042
    $startX = $s * 0.32
    $startY = $s * 0.47
    $gapX = $s * 0.18
    $gapY = $s * 0.16
    for ($row = 0; $row -lt 2; $row++) {
        for ($col = 0; $col -lt 3; $col++) {
            $cx = $startX + ($col * $gapX)
            $cy = $startY + ($row * $gapY)
            $g.FillEllipse($blueBrush, ($cx - $dotR), ($cy - $dotR), ($dotR * 2), ($dotR * 2))
        }
    }

    $g.Dispose()
    return $bmp
}

$root = Split-Path -Parent $PSScriptRoot
$outPath = Join-Path $root 'src\TaskbarCalendar\Assets\app.ico'
New-Item -ItemType Directory -Path (Split-Path $outPath) -Force | Out-Null

# 从位图构造标准 32bpp BMP 帧：BITMAPINFOHEADER + XOR 位图 + AND 掩码
function ConvertTo-IconFrame {
    param([System.Drawing.Bitmap]$Bitmap)

    $w = $Bitmap.Width
    $h = $Bitmap.Height
    $rect = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
    $data = $Bitmap.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    try {
        $stride = $data.Stride
        if ($stride -lt 0) { throw "不支持的负 stride" }
        $pixels = New-Object byte[] ($stride * $h)
        [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)
    } finally {
        $Bitmap.UnlockBits($data)
    }

    $andStride = [int]([math]::Floor(($w + 31) / 32) * 4)
    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    $bw.Write([UInt32]40)            # biSize
    $bw.Write([Int32]$w)             # biWidth
    $bw.Write([Int32]($h * 2))       # biHeight（XOR + AND 两倍高度）
    $bw.Write([UInt16]1)             # biPlanes
    $bw.Write([UInt16]32)            # biBitCount
    $bw.Write([UInt32]0)             # biCompression（BI_RGB）
    $bw.Write([UInt32]0)             # biSizeImage
    $bw.Write([Int32]0)              # biXPelsPerMeter
    $bw.Write([Int32]0)              # biYPelsPerMeter
    $bw.Write([UInt32]0)             # biClrUsed
    $bw.Write([UInt32]0)             # biClrImportant

    # XOR 位图（自下而上）
    for ($y = $h - 1; $y -ge 0; $y--) {
        $bw.Write($pixels, ($y * $stride), ($w * 4))
    }

    # AND 掩码（32bpp 全 0，透明由 alpha 通道决定）
    $andMask = New-Object byte[] ($andStride * $h)
    $bw.Write($andMask)

    $bw.Flush()
    $result = $ms.ToArray()
    $bw.Dispose()
    $ms.Dispose()
    return , $result
}

$sizes = @(256, 48, 32, 16)
$frames = @()
foreach ($size in $sizes) {
    $bmp = New-IconBitmap -Size $size
    $frameData = ConvertTo-IconFrame -Bitmap $bmp
    $frames += , @($size, $frameData)
    $bmp.Dispose()
}

# 打包多尺寸 ICO 容器
$fs = [System.IO.File]::Create($outPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([UInt16]0)               # reserved
$bw.Write([UInt16]1)               # type: icon
$bw.Write([UInt16]$frames.Count)   # image count

$offset = 6 + (16 * $frames.Count)
foreach ($frame in $frames) {
    $size = $frame[0]
    $data = $frame[1]
    $dim = if ($size -ge 256) { 0 } else { $size }
    $bw.Write([byte]$dim)          # width
    $bw.Write([byte]$dim)          # height
    $bw.Write([byte]0)             # color count
    $bw.Write([byte]0)             # reserved
    $bw.Write([UInt16]1)           # planes
    $bw.Write([UInt16]32)          # bpp
    $bw.Write([UInt32]$data.Length)
    $bw.Write([UInt32]$offset)
    $offset += $data.Length
}

foreach ($frame in $frames) {
    $bw.Write($frame[1])
}

$bw.Flush()
$bw.Dispose()
$fs.Dispose()

# 输出一张预览图便于核对
$previewBmp = New-IconBitmap -Size 256
$previewPath = Join-Path $env:TEMP 'taskbar-calendar-icon-preview.png'
$previewBmp.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$previewBmp.Dispose()

Write-Output "Icon generated: $outPath ($((Get-Item $outPath).Length) bytes)"
Write-Output "Preview: $previewPath"
