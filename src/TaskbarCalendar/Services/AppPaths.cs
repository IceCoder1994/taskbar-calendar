using System.IO;

namespace TaskbarCalendar.Services;

/// <summary>
/// 应用相关路径管理
/// </summary>
public static class AppPaths
{
    /// <summary>本地应用数据目录（日志等）</summary>
    public static string LocalAppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "TaskbarCalendar");

    /// <summary>日志目录</summary>
    public static string LogDirectory { get; } = Path.Combine(LocalAppDataDirectory, "logs");

    /// <summary>漫游应用数据目录（设置等）</summary>
    public static string RoamingAppDataDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "TaskbarCalendar");

    /// <summary>设置文件路径</summary>
    public static string SettingsFilePath { get; } = Path.Combine(RoamingAppDataDirectory, "settings.json");

    /// <summary>内置节假日数据文件路径（随程序输出）</summary>
    public static string HolidaysFilePath { get; } = Path.Combine(AppContext.BaseDirectory, "Data", "holidays.json");
}
