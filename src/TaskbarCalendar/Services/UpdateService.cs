using System.Diagnostics;
using System.IO;
using System.Net.Http;
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
/// 客户端更新检查与静默升级服务：下载安装器（Setup.exe）并以 Inno Setup 静默参数完成升级
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
    /// 一键下载新版本安装器并以静默参数拉起升级。
    /// 安装位置为每用户可写目录（%LOCALAPPDATA%\Programs\TaskbarCalendar），
    /// 静默安装无需管理员权限，且会通过 /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS
    /// 自动接管本进程占用的文件并在装完后自动重启。
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

        // 安装器命名与站点下载包规则一致（TaskbarCalendar-v{版本}-win-x64-Setup.exe）
        string setupPath = Path.Combine(tempRoot, $"TaskbarCalendar-v{info.Version}-win-x64-Setup.exe");

        Logger.Info($"开始下载新版本安装包: {info.DownloadUrl}");

        // 1. 带进度汇报流式下载安装器
        using (var client = new HttpClient { Timeout = TimeSpan.FromMinutes(3) })
        using (HttpResponseMessage response = await client.GetAsync(info.DownloadUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken))
        {
            response.EnsureSuccessStatusCode();
            long totalBytes = response.Content.Headers.ContentLength ?? -1;

            using Stream source = await response.Content.ReadAsStreamAsync();
            using FileStream destination = new FileStream(setupPath, FileMode.Create, FileAccess.Write, FileShare.None);

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

        Logger.Info("安装包下载完成，正在启动静默安装...");
        progress?.Report(100);

        // 2. 静默拉起安装器完成升级
        //    /SILENT                 静默安装（不弹交互向导）
        //    /SUPPRESSMSGBOXES       抑制安装器消息框，避免托盘场景下无窗口提示被阻塞
        //    /NORESTART              禁止安装器自行重启系统
        //    /FORCECLOSEAPPLICATIONS 强制结束占用安装目录的本程序进程（托盘程序收到
        //                            WM_CLOSE 只会隐藏窗体不会退出，必须走强制路径；
        //                            同时本方法末尾会主动 Shutdown，双保险）
        //    /RESTARTAPPLICATIONS    安装完成后自动重启被关闭的应用程序
        var startInfo = new ProcessStartInfo
        {
            FileName = setupPath,
            Arguments = "/SILENT /SUPPRESSMSGBOXES /NORESTART /FORCECLOSEAPPLICATIONS /RESTARTAPPLICATIONS",
            UseShellExecute = true,
            WorkingDirectory = tempRoot,
        };

        Process.Start(startInfo);

        Logger.Info("静默安装器已启动，准备退出主程序...");

        // 3. UI 线程退出：安装器接管后续升级与重启
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
