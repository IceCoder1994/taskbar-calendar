using Microsoft.Win32;

namespace TaskbarCalendar.Services;

using Color = System.Windows.Media.Color;

/// <summary>
/// 系统主题与强调色检测
/// </summary>
public static class ThemeService
{
    /// <summary>当前应用主题是否为浅色（读取失败时按浅色处理）</summary>
    public static bool IsLightTheme()
    {
        try
        {
            object? value = Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Themes\Personalize",
                "AppsUseLightTheme",
                1);
            return value is not int i || i != 0;
        }
        catch
        {
            return true;
        }
    }

    /// <summary>读取系统强调色（AccentColor 为 ABGR 顺序），失败返回 null</summary>
    public static Color? GetAccentColor()
    {
        try
        {
            object? value = Registry.GetValue(@"HKEY_CURRENT_USER\Software\Microsoft\Windows\DWM", "AccentColor", null);
            if (value is int raw && raw != 0)
            {
                byte r = (byte)(raw & 0xFF);
                byte g = (byte)((raw >> 8) & 0xFF);
                byte b = (byte)((raw >> 16) & 0xFF);
                return Color.FromRgb(r, g, b);
            }
        }
        catch
        {
            // 忽略，使用默认强调色
        }

        return null;
    }
}
