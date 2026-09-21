using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TaskbarCalendar.Services;

/// <summary>主题模式</summary>
public enum ThemeMode
{
    /// <summary>跟随系统</summary>
    System,

    /// <summary>强制浅色</summary>
    Light,

    /// <summary>强制深色</summary>
    Dark,
}

/// <summary>周起始日</summary>
public enum WeekStartMode
{
    /// <summary>周一</summary>
    Monday,

    /// <summary>周日</summary>
    Sunday,
}

/// <summary>应用设置</summary>
public sealed class AppSettings
{
    public WeekStartMode WeekStart { get; set; } = WeekStartMode.Monday;

    public ThemeMode Theme { get; set; } = ThemeMode.System;

    public bool AutoStart { get; set; }

    public bool ShowLunar { get; set; } = true;

    public bool ShowHolidayMark { get; set; } = true;

    /// <summary>是否每天自动同步节假日数据（静默执行）</summary>
    public bool AutoSyncHolidays { get; set; } = true;

    /// <summary>时钟区域水平偏移校准（像素）</summary>
    public int OffsetX { get; set; }

    /// <summary>时钟区域垂直偏移校准（像素）</summary>
    public int OffsetY { get; set; }
}

/// <summary>
/// 设置持久化服务（单例）：读写 %APPDATA%\TaskbarCalendar\settings.json
/// </summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        // 枚举以字符串形式读写（提升设置文件可读性与兼容性）
        Converters = { new JsonStringEnumConverter() },
    };

    public static SettingsService Instance { get; } = new();

    private SettingsService()
    {
    }

    /// <summary>当前设置</summary>
    public AppSettings Current { get; private set; } = new();

    /// <summary>设置变更事件</summary>
    public event EventHandler? Changed;

    public void Load()
    {
        try
        {
            if (!File.Exists(AppPaths.SettingsFilePath))
            {
                Logger.Info("设置文件不存在，使用默认设置");
                return;
            }

            string json = File.ReadAllText(AppPaths.SettingsFilePath);
            Current = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
            Logger.Info("设置已加载");
        }
        catch (Exception ex)
        {
            Logger.Error("加载设置失败，使用默认设置", ex);
            Current = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.RoamingAppDataDirectory);
            string json = JsonSerializer.Serialize(Current, JsonOptions);
            File.WriteAllText(AppPaths.SettingsFilePath, json);
            Logger.Info("设置已保存");
            Changed?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            Logger.Error("保存设置失败", ex);
        }
    }
}
