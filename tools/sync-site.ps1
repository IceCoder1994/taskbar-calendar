# 同步官网站点资源：按当前版本构建安装器并更新 site/ 内所有版本号与日期
# 用法: powershell -ExecutionPolicy Bypass -File tools\sync-site.ps1
# 说明: 发布新版本前执行本脚本，然后把 site/ 的改动一并提交推送，
#       Cloudflare Pages 会自动重新部署，官网下载包与版本号即保持最新。
# 依赖: 本机需安装 Inno Setup 6（未安装时跳过安装器编译并给出告警）。
# 注意: 本脚本含中文，必须保持 UTF-8 with BOM（见 AGENTS.md 陷阱 1）
$ErrorActionPreference = 'Stop'

$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\TaskbarCalendar\TaskbarCalendar.csproj'
$distDir = Join-Path $root 'dist'
$siteDownload = Join-Path $root 'site\download'
$issFile = Join-Path $root 'installer\TaskbarCalendar.iss'
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

# 2. 按与 CI 一致的参数发布（.NET Framework 4.8，无需运行时）
if (Test-Path $distDir) { Remove-Item $distDir -Recurse -Force }
dotnet publish $project -c Release -p:Version=$version -o $distDir --nologo
if ($LASTEXITCODE -ne 0) {
    throw "发布失败，退出码 $LASTEXITCODE"
}

# 3. 编译安装器（每用户安装，免 UAC）并生成站点下载包
Copy-Item $manualDoc (Join-Path $distDir '使用说明.txt') -Force
New-Item -ItemType Directory -Path $siteDownload -Force | Out-Null

$iscc = Get-Command iscc -ErrorAction SilentlyContinue
if (-not $iscc) {
    foreach ($candidate in @(
        "$env:ProgramFiles(x86)\Inno Setup 6\ISCC.exe",
        "$env:ProgramFiles\Inno Setup 6\ISCC.exe")) {
        if (Test-Path $candidate) { $iscc = Get-Command $candidate; break }
    }
}
if (-not $iscc) {
    throw '未检测到 Inno Setup 编译器，无法生成安装器。请先安装 Inno Setup 6（https://jrsoftware.org/isdl.php）后重试。'
}

& $iscc.Source "/Q" "/DMyAppVersion=$version" "/DSourceDir=$distDir" "/O$siteDownload" $issFile | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "Inno Setup 编译失败，退出码 $LASTEXITCODE"
}

$setupName = "TaskbarCalendar-$tag-win-x64-Setup.exe"
$setupPath = Join-Path $siteDownload $setupName
if (-not (Test-Path $setupPath)) {
    throw "安装器编译后未找到预期产物: $setupPath"
}
$setupKB = [math]::Round((Get-Item $setupPath).Length / 1KB)
Write-Output "下载包已更新: $setupPath ($setupKB KB)"

# 4. 清理历史遗留下载包（旧 zip 与旧版本安装器），避免用户下载到旧文件
Get-ChildItem $siteDownload -Include '*.zip', '*.exe' -File | Where-Object { $_.Name -ne $setupName } | Remove-Item -Force

# 5. 更新页面中的版本号与下载文件名
foreach ($file in @($indexHtml, $errorHtml)) {
    $text = [System.IO.File]::ReadAllText($file, [System.Text.Encoding]::UTF8)
    $text = [regex]::Replace($text, 'TaskbarCalendar-v[\d.]+(?:-lite)?-win-x64\.(?:zip|exe)', $setupName)
    $text = [regex]::Replace($text, 'v\d+\.\d+\.\d+', $tag)
    [System.IO.File]::WriteAllText($file, $text, $utf8NoBom)
    Write-Output "已更新版本号: $(Split-Path -Leaf $file)"
}

# 6. 更新 sitemap 的 lastmod
$today = Get-Date -Format 'yyyy-MM-dd'
$text = [System.IO.File]::ReadAllText($sitemap, [System.Text.Encoding]::UTF8)
$text = [regex]::Replace($text, '<lastmod>[\d-]+</lastmod>', "<lastmod>$today</lastmod>")
[System.IO.File]::WriteAllText($sitemap, $text, $utf8NoBom)
Write-Output "已更新 sitemap lastmod: $today"

# 7. 同步 site/version.json（客户端更新检查元数据）
$versionJsonPath = Join-Path $root 'site\version.json'
$verMeta = [ordered]@{
    version = $version
    releaseDate = $today
    downloadUrl = "https://calendar.icewang.qzz.io/download/$setupName"
    changelog = "修复已知问题并提升系统稳定性。"
}
if (Test-Path $versionJsonPath) {
    try {
        $oldMeta = Get-Content $versionJsonPath -Encoding UTF8 | ConvertFrom-Json
        if ($oldMeta.changelog) {
            $verMeta.changelog = $oldMeta.changelog
        }
    } catch { }
}
$jsonOutput = ($verMeta | ConvertTo-Json -Depth 4)
[System.IO.File]::WriteAllText($versionJsonPath, $jsonOutput, $utf8NoBom)
Write-Output "已更新客户端版本元数据: site\version.json"

Write-Output ''
Write-Output '完成。请提交并推送 site/ 的改动，Cloudflare Pages 将自动重新部署。'
Write-Output '若页面截图或 OG 分享图需要更新，请重新生成 site\assets\ 下的图片。'
