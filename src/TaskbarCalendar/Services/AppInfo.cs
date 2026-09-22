using System.Reflection;

namespace TaskbarCalendar.Services;

/// <summary>
/// 应用程序全局信息与元数据常量
/// </summary>
public static class AppInfo
{
    /// <summary>程序中文显示名称</summary>
    public const string AppName = "任务栏日历";

    /// <summary>官方网站直链</summary>
    public const string WebsiteUrl = "https://calendar.icewang.qzz.io/";

    /// <summary>版本检查元数据 JSON 地址（托管于 Cloudflare Pages 边缘节点）</summary>
    public const string UpdateCheckUrl = "https://calendar.icewang.qzz.io/version.json";

    /// <summary>当前程序版本号（三段式，如 1.1.0）</summary>
    public static string CurrentVersion { get; } = GetCurrentVersion();

    /// <summary>当前版本 Tag 标识（如 v1.1.0）</summary>
    public static string CurrentVersionTag => "v" + CurrentVersion;

    private static string GetCurrentVersion()
    {
        Version? ver = Assembly.GetExecutingAssembly().GetName().Version;
        if (ver is null)
        {
            return "1.1.0";
        }

        // 去掉末尾为 0 的 revision，保留三段式版本号
        return $"{ver.Major}.{ver.Minor}.{Math.Max(0, ver.Build)}";
    }
}
