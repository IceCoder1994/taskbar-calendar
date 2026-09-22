$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent $PSScriptRoot
$assets = Join-Path $root 'site\assets'
$out = Join-Path $assets 'og-image.png'
$shotPath = Join-Path $assets 'calendar-dark.png'

$W = 1200
$H = 630
$bmp = New-Object System.Drawing.Bitmap($W, $H)
$g = [System.Drawing.Graphics]::FromImage($bmp)
$g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$g.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::ClearTypeGridFit
$g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic

function New-RoundRectPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $r * 2
    $path.AddArc($x, $y, $d, $d, 180, 90)
    $path.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $path.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $path.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

# --- background ---
$rect = New-Object System.Drawing.Rectangle(0, 0, $W, $H)
$bg = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    [System.Drawing.Color]::FromArgb(255, 13, 15, 21),
    [System.Drawing.Color]::FromArgb(255, 24, 30, 44),
    30.0)
$g.FillRectangle($bg, $rect)

# soft blue glow (layered translucent circles)
for ($i = 10; $i -ge 1; $i--) {
    $r = 60 * $i
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(5, 0, 120, 212))
    $g.FillEllipse($brush, (270 - $r), (120 - $r), ($r * 2), ($r * 2))
    $brush.Dispose()
}
for ($i = 8; $i -ge 1; $i--) {
    $r = 50 * $i
    $brush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(4, 79, 172, 254))
    $g.FillEllipse($brush, (1080 - $r), (560 - $r), ($r * 2), ($r * 2))
    $brush.Dispose()
}

# --- fonts ---
$fontName = 'Microsoft YaHei'
try { $null = New-Object System.Drawing.Font($fontName, 12) } catch { $fontName = 'Arial' }
$fTitle = New-Object System.Drawing.Font($fontName, 46, [System.Drawing.FontStyle]::Bold)
$fLatin = New-Object System.Drawing.Font('Segoe UI', 14, [System.Drawing.FontStyle]::Bold)
$fSlogan = New-Object System.Drawing.Font($fontName, 25, [System.Drawing.FontStyle]::Bold)
$fDesc = New-Object System.Drawing.Font($fontName, 16)
$fBullet = New-Object System.Drawing.Font($fontName, 16)
$fBtn = New-Object System.Drawing.Font($fontName, 17, [System.Drawing.FontStyle]::Bold)
$fTag = New-Object System.Drawing.Font('Segoe UI', 12, [System.Drawing.FontStyle]::Bold)

$white = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 240, 242, 248))
$grey = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 154, 161, 181))
$blue = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 79, 172, 254))
$btnText = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)

# --- left column text ---
$g.DrawString('任务栏日历', $fTitle, $white, 80, 84)
$g.DrawString('TASKBAR CALENDAR', $fLatin, $blue, 84, 168)

$g.DrawString('点击任务栏时钟，弹出农历与调休', $fSlogan, $white, 80, 226)
$g.DrawString('零侵入替换 Windows 11 原生日历 · 不影响通知中心', $fDesc, $grey, 82, 282)

$bullets = @(
    '农历日期 / 二十四节气 / 传统节日',
    '法定节假日「休 / 班」自动同步',
    '开源免费 · 132 KB 绿色免安装'
)
$by = 344
foreach ($b in $bullets) {
    $mark = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 16, 185, 129))
    $g.FillEllipse($mark, 84, ($by + 9), 9, 9)
    $mark.Dispose()
    $g.DrawString($b, $fBullet, $grey, 106, $by)
    $by += 42
}

# --- download button ---
$btnX = 80
$btnY = 500
$btnW = 300
$btnH = 62
$btnPath = New-RoundRectPath $btnX $btnY $btnW $btnH 15
$btnBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    (New-Object System.Drawing.Rectangle($btnX, $btnY, $btnW, $btnH)),
    [System.Drawing.Color]::FromArgb(255, 0, 120, 212),
    [System.Drawing.Color]::FromArgb(255, 79, 172, 254),
    0.0)
$g.FillPath($btnBrush, $btnPath)
$btnLabel = '立即下载 绿色版'
$labelSize = $g.MeasureString($btnLabel, $fBtn)
$g.DrawString($btnLabel, $fBtn, $btnText, ($btnX + ($btnW - $labelSize.Width) / 2), ($btnY + ($btnH - $labelSize.Height) / 2))
$btnPath.Dispose()

# version tag next to button
$g.DrawString('Windows 11 x64 · MIT 开源', $fTag, $grey, ($btnX + 320), ($btnY + 24))

# --- right column screenshot ---
$shot = [System.Drawing.Image]::FromFile($shotPath)
$maxH = 470.0
$ratio = $maxH / $shot.Height
$sw = [int]($shot.Width * $ratio)
$sh = [int]$maxH
$sx = 1200 - 80 - $sw
$sy = [int]((630 - $sh) / 2)

# drop shadow
$shadowPath = New-RoundRectPath ($sx + 8) ($sy + 12) $sw $sh 14
$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(90, 0, 0, 0))
$g.FillPath($shadowBrush, $shadowPath)
$shadowPath.Dispose()
$shadowBrush.Dispose()

# rounded screenshot
$shotPathRounded = New-RoundRectPath $sx $sy $sw $sh 14
$oldClip = $g.Clip
$g.SetClip($shotPathRounded)
$g.DrawImage($shot, $sx, $sy, $sw, $sh)
$g.Clip = $oldClip
$shotPathRounded.Dispose()
$shot.Dispose()

# thin border around screenshot
$borderPath = New-RoundRectPath $sx $sy $sw $sh 14
$borderPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(70, 255, 255, 255), 1.5)
$g.DrawPath($borderPen, $borderPath)
$borderPath.Dispose()
$borderPen.Dispose()

# --- footer line ---
$fFooter = New-Object System.Drawing.Font('Segoe UI', 11)
$g.DrawString('calendar.icewang.qzz.io  ·  github.com/IceCoder1994/taskbar-calendar', $fFooter, $grey, 80, 588)
$fFooter.Dispose()

$g.Dispose()
$bg.Dispose()
$bmp.Save($out, [System.Drawing.Imaging.ImageFormat]::Png)
$bmp.Dispose()

Write-Output ('og-image.png: ' + (Get-Item $out).Length + ' bytes')
