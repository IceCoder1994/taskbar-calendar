using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace TaskbarCalendar.Services;

/// <summary>新版本元数据描述</summary>
public sealed class UpdateInfo
{
    public string Version { get; set; } = string.Empty;
    public string ReleaseDate { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;
    public string Changelog { get; set; } = string.Empty;
}

/// <summary>版本检查结果</summary>
public sealed class UpdateCheckResult
{
    public bool Success { get; }
    public bool HasUpdate { get; }
    public UpdateInfo? Info { get; }
    public string Message { get; }

    public UpdateCheckResult(bool success, bool hasUpdate, UpdateInfo? info, string message)
    {
        Success = success;
        HasUpdate = hasUpdate;
        Info = info;
        Message = message;
    }
}

/// <summary>
/// 客户端更新检查与就地下载替换自更新服务
/// </summary>
public sealed class UpdateService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly SemaphoreSlim CheckLock = new(1, 1);

    /// <summary>检查远端是否有新版本</summary>
    public async Task<UpdateCheckResult> CheckUpdateAsync(bool silent = false)
    {
        await CheckLock.WaitAsync();
        try
        {
            if (!silent)
            {
                Logger.Info("正在手动检查版本更新...");
            }

            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(6) };
            // 添加防缓存时间戳
            string requestUrl = $"{AppInfo.UpdateCheckUrl}?_t={DateTime.UtcNow.Ticks}";
            using HttpResponseMessage response = await client.GetAsync(requestUrl);
            if (!response.IsSuccessStatusCode)
            {
                string errMsg = $"请求版本信息失败，HTTP 状态码: {response.StatusCode}";
                Logger.Warn(errMsg);
                return new UpdateCheckResult(false, false, null, errMsg);
            }

            string json = await response.Content.ReadAsStringAsync();
            UpdateInfo? info = JsonSerializer.Deserialize<UpdateInfo>(json, JsonOptions);
            if (info is null || string.IsNullOrWhiteSpace(info.Version))
            {
                return new UpdateCheckResult(false, false, null, "未能解析有效的版本元数据");
            }

            // 更新上次检查时间
            SettingsService.Instance.Current.LastUpdateCheckTime = DateTime.UtcNow;
            SettingsService.Instance.Save();

            // 语义化比对版本号
            bool hasUpdate = IsNewerVersion(info.Version, AppInfo.CurrentVersion);
            if (hasUpdate)
            {
                string msg = $"发现新版本 v{info.Version}（当前版本 v{AppInfo.CurrentVersion}）";
                Logger.Info(msg);
                return new UpdateCheckResult(true, true, info, msg);
            }

            string latestMsg = $"当前已是最新版本 (v{AppInfo.CurrentVersion})";
            if (!silent)
            {
                Logger.Info(latestMsg);
            }
            return new UpdateCheckResult(true, false, info, latestMsg);
        }
        catch (Exception ex)
        {
            string err = $"检查更新异常: {ex.Message}";
            Logger.Error(err, ex);
            return new UpdateCheckResult(false, false, null, err);
        }
        finally
        {
            CheckLock.Release();
        }
    }

    /// <summary>后台按最小时间间隔静默检查更新</summary>
    public async Task<UpdateCheckResult?> MaybeAutoCheckAsync(TimeSpan minInterval)
    {
        AppSettings settings = SettingsService.Instance.Current;
        if (!settings.AutoCheckUpdate)
        {
            return null;
        }

        DateTime? last = settings.LastUpdateCheckTime;
        if (last.HasValue && DateTime.UtcNow - last.Value < minInterval)
        {
            return null;
        }

        return await CheckUpdateAsync(silent: true);
    }

    /// <summary>
    /// 一键下载新版本压缩包、解压并在后台拉起批处理脚本覆盖自身并自动重启
    /// </summary>
    public async Task DownloadAndApplyUpdateAsync(UpdateInfo info, IProgress<int>? progress = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(info.DownloadUrl))
        {
            throw new InvalidOperationException("未提供下载链接地址");
        }

        string tempRoot = Path.Combine(Path.GetTempPath(), "TaskbarCalendar_Update");
        if (Directory.Exists(tempRoot))
        {
            try { Directory.Delete(tempRoot, true); } catch { }
        }
        Directory.CreateDirectory(tempRoot);

        string zipPath = Path.Combine(tempRoot, "update.zip");
        string extractDir = Path.Combine(tempRoot, "extracted");

        Logger.Info($"开始下载新版本安装包: {info.DownloadUrl}");

        // 1. 带进度汇报流式下载
        using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) })
        using (HttpResponseMessage response = await client.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            long totalBytes = response.Content.Headers.ContentLength ?? -1;

            using Stream source = await response.Content.ReadAsStreamAsync();
            using FileStream destination = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);

            byte[] buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;

            while ((bytesRead = await source.ReadAsync(buffer, 0, buffer.Length, cancellationToken)) > 0)
            {
                await destination.WriteAsync(buffer, 0, bytesRead, cancellationToken);
                totalRead += bytesRead;
                if (totalBytes > 0 && progress is not null)
                {
                    int pct = (int)((totalRead * 100) / totalBytes);
                    progress.Report(Math.Min(100, Math.Max(0, pct)));
                }
            }
        }

        Logger.Info("更新包下载完成，开始解压...");
        progress?.Report(100);

        // 2. 解压安装包
        ZipFile.ExtractToDirectory(zipPath, extractDir);

        // 寻找实际包含 TaskbarCalendar.exe 的源目录（防打包时外层包裹文件夹）
        string sourceDir = extractDir;
        if (!File.Exists(Path.Combine(sourceDir, "TaskbarCalendar.exe")))
        {
            string[] subDirs = Directory.GetDirectories(extractDir);
            foreach (string sub in subDirs)
            {
                if (File.Exists(Path.Combine(sub, "TaskbarCalendar.exe")))
                {
                    sourceDir = sub;
                    break;
                }
            }
        }

        if (!File.Exists(Path.Combine(sourceDir, "TaskbarCalendar.exe")))
        {
            throw new FileNotFoundException("更新包中未找到 TaskbarCalendar.exe 可执行文件");
        }

        // 3. 构建就地替换与重启的批处理脚本
        int currentPid = Process.GetCurrentProcess().Id;
        string? targetExePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(targetExePath) || !File.Exists(targetExePath) || Path.GetFileName(targetExePath).IndexOf("TaskbarCalendar", StringComparison.OrdinalIgnoreCase) < 0)
        {
            targetExePath = Path.Combine(AppContext.BaseDirectory, "TaskbarCalendar.exe");
        }
        string appDir = Path.GetDirectoryName(targetExePath) ?? AppContext.BaseDirectory;
        // 批处理文件置于外层临时目录，避免删除解压临时包时被占用
        string cmdScriptPath = Path.Combine(Path.GetTempPath(), $"taskbar_update_{currentPid}.cmd");

        var sb = new StringBuilder();
        sb.AppendLine("@echo off");
        sb.AppendLine("chcp 65001 >nul");
        sb.AppendLine($"set \"TARGET_PID={currentPid}\"");
        sb.AppendLine($"set \"SRC_DIR={sourceDir}\"");
        sb.AppendLine($"set \"APP_DIR={appDir}\"");
        sb.AppendLine($"set \"EXE_PATH={targetExePath}\"");
        sb.AppendLine();
        sb.AppendLine(":: 循环等待主进程完全退出以释放文件句柄锁定");
        sb.AppendLine(":WAIT_LOOP");
        sb.AppendLine("tasklist /fi \"PID eq %TARGET_PID%\" 2>nul | find \"%TARGET_PID%\" >nul");
        sb.AppendLine("if not errorlevel 1 (");
        sb.AppendLine("    timeout /t 1 /nobreak >nul");
        sb.AppendLine("    goto WAIT_LOOP");
        sb.AppendLine(")");
        sb.AppendLine();
        sb.AppendLine(":: 额外等待 500ms 确保所有 dll 句柄释放干净");
        sb.AppendLine("timeout /t 1 /nobreak >nul");
        sb.AppendLine();
        sb.AppendLine(":: 覆盖拷贝新文件到程序目录");
        sb.AppendLine("xcopy \"%SRC_DIR%\\*\" \"%APP_DIR%\\\" /s /e /y >nul");
        sb.AppendLine();
        sb.AppendLine(":: 重新拉起主程序");
        sb.AppendLine("start \"\" \"%EXE_PATH%\"");
        sb.AppendLine();
        sb.AppendLine(":: 清理解压临时包与批处理脚本自身");
        sb.AppendLine("timeout /t 1 /nobreak >nul");
        sb.AppendLine($"rd /s /q \"{tempRoot}\" 2>nul");
        sb.AppendLine("del \"%~f0\" 2>nul");

        File.WriteAllText(cmdScriptPath, sb.ToString(), new UTF8Encoding(false));

        Logger.Info($"更新就绪，拉起更新脚本并退出主程序 (PID: {currentPid})...");

        // 4. 启动后台批处理并优雅退出
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c \"{cmdScriptPath}\"",
            CreateNoWindow = true,
            UseShellExecute = false,
            WorkingDirectory = Path.GetTempPath(),
        };

        Process.Start(startInfo);

        // UI 线程退出
        if (System.Windows.Application.Current is not null)
        {
            _ = System.Windows.Application.Current.Dispatcher.BeginInvoke(new Action(() =>
            {
                System.Windows.Application.Current.Shutdown();
            }));
        }
        else
        {
            Environment.Exit(0);
        }
    }

    /// <summary>比较远端版本与本地版本号</summary>
    public static bool IsNewerVersion(string remoteVersionStr, string localVersionStr)
    {
        string cleanRemote = remoteVersionStr.Trim().TrimStart('v', 'V');
        string cleanLocal = localVersionStr.Trim().TrimStart('v', 'V');

        if (Version.TryParse(NormalizeVersion(cleanRemote), out Version? remote) &&
            Version.TryParse(NormalizeVersion(cleanLocal), out Version? local))
        {
            return remote > local;
        }

        return false;
    }

    private static string NormalizeVersion(string v)
    {
        string[] parts = v.Split('.');
        if (parts.Length == 1) return $"{parts[0]}.0.0";
        if (parts.Length == 2) return $"{parts[0]}.{parts[1]}.0";
        return v;
    }
}
