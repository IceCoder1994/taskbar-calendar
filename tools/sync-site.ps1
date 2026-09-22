# 同步官网站点资源：按当前版本重新打包 zip 并更新 site/ 内所有版本号与日期
# 用法: powershell -ExecutionPolicy Bypass -File tools\sync-site.ps1
# 说明: 发布新版本前执行本脚本，然后把 site/ 的改动一并提交推送，
#       Cloudflare Pages 会自动重新部署，官网下载包与版本号即保持最新。
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\TaskbarCalendar\TaskbarCalendar.csproj'
$distDir = Join-Path $root 'dist'
$siteDownload = Join-Path $root 'site\download'
$manualDoc = Join-Path $root 'docs\使用说明.txt'
$indexHtml = Join-Path $root 'site\index.html'
$errorHtml = Join-Path $root 'site\404.html'
$sitemap = Join-Path $root 'site\sitemap.xml'
$utf8NoBom = New-Object System.Text.UTF8Encoding $false

# 1. 从 csproj 读取当前版本号
[xml]$proj = Get-Content $project
$version = $proj.Project.PropertyGroup.Version | Where-Object { $_ } | Select-Object -First 1
if (-not $version) {
    throw "未能从 $project 读取 Version 节点"
}
$tag = "v$version"
Write-Output "当前版本: $tag"

# 2. 按与 CI 一致的参数发布单文件版
if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
dotnet publish $project -c Release -r win-x64 --self-contained false `
    -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false `
    -p:Version=$version -o $distDir --nologo
if ($LASTEXITCODE -ne 0) {
    throw "发布失败，退出码 $LASTEXITCODE"
}

# 3. 生成站点下载包（与 Release 同为 lite 命名，附带使用说明）
Copy-Item $manualDoc (Join-Path $distDir '使用说明.txt') -Force
New-Item -ItemType Directory -Path $siteDownload -Force | Out-Null
Get-ChildItem $siteDownload -Filter '*.zip' | Remove-Item -Force
$zipPath = Join-Path $siteDownload "TaskbarCalendar-$tag-lite-win-x64.zip"
Compress-Archive -Path (Join-Path $distDir '*') -DestinationPath $zipPath -Force
$zipKB = [math]::Round((Get-Item $zipPath).Length / 1KB)
Write-Output "下载包已更新: site\download\TaskbarCalendar-$tag-lite-win-x64.zip ($zipKB KB)"

# 4. 更新页面中的版本号（zip 文件名 + 展示版本号）
foreach ($file in @($indexHtml, $errorHtml)) {
    $text = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)
    $text = [regex]::Replace($text, 'TaskbarCalendar-v[\d.]+-lite-win-x64\.zip', "TaskbarCalendar-$tag-lite-win-x64.zip")
    $text = [regex]::Replace($text, 'v\d+\.\d+\.\d+', $tag)
    [System.IO.File]::WriteAllText($file, $text, $utf8NoBom)
    Write-Output "已更新版本号: $(Split-Path -Leaf $file)"
}

# 5. 更新 sitemap 的 lastmod
$today = Get-Date -Format 'yyyy-MM-dd'
$text = [System.IO.File]::ReadAllText($sitemap, [System.Text.Encoding]::UTF8)
$text = [regex]::Replace($text, '<lastmod>[\d-]+</lastmod>', "<lastmod>$today</lastmod>")
[System.IO.File]::WriteAllText($sitemap, $text, $utf8NoBom)
Write-Output "已更新 sitemap lastmod: $today"

Write-Output ''
Write-Output '完成。请提交并推送 site/ 的改动，Cloudflare Pages 将自动重新部署。'
Write-Output '若页面截图或 OG 分享图需要更新，请重新生成 site\assets\ 下的图片。'
