using Microsoft.Win32;

namespace TaskbarCalendar.Services;

/// <summary>
/// 开机自启服务：读写 HKCU 的 Run 键
/// </summary>
public static class AutoStartService
{
    private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string ValueName = "TaskbarCalendar";

    /// <summary>是否已启用开机自启</summary>
    public static bool IsEnabled()
    {
        try
        {
            using RegistryKey? key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
            return key?.GetValue(ValueName) is string value && value.Length > 0;
        }
        catch (Exception ex)
        {
            Logger.Error("读取自启状态失败", ex);
            return false;
        }
    }

    /// <summary>设置开机自启</summary>
    public static bool SetEnabled(bool enabled)
    {
        try
        {
            using RegistryKey key = Registry.CurrentUser.CreateSubKey(RunKeyPath, true);
            if (enabled)
            {
                string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(exePath))
                {
                    Logger.Warn("无法获取程序路径，未能设置自启");
                    return false;
                }

                key.SetValue(ValueName, $"\"{exePath}\"");
                Logger.Info($"已启用开机自启: {exePath}");
            }
            else
            {
                key.DeleteValue(ValueName, false);
                Logger.Info("已关闭开机自启");
            }

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error("设置开机自启失败", ex);
            return false;
        }
    }
}
