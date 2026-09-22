using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using TaskbarCalendar.Calendar;
using TaskbarCalendar.Interop;
using TaskbarCalendar.Services;

// WinForms 全局 using 与 WPF 同名类型消歧
using Button = System.Windows.Controls.Button;

namespace TaskbarCalendar.UI;

/// <summary>
/// 设置窗口：基本设置、时钟区域校准与关于信息（修改即保存）
/// </summary>
public partial class SettingsWindow : Window
{
    private readonly HolidayService _holidayService = new();
    private readonly UpdateService _updateService = new();

    private bool _loading;
    private DispatcherTimer? _calibrateTimer;
    private int _calibrateCountdown;

    public SettingsWindow()
    {
        InitializeComponent();
        _holidayService.Load();
        LoadCurrent();
    }

    /// <summary>从设置加载界面状态</summary>
    private void LoadCurrent()
    {
        _loading = true;

        AppSettings settings = SettingsService.Instance.Current;
        WeekStartCombo.SelectedIndex = settings.WeekStart == WeekStartMode.Sunday ? 1 : 0;
        ThemeCombo.SelectedIndex = settings.Theme switch
        {
            ThemeMode.Light => 1,
            ThemeMode.Dark => 2,
            _ => 0,
        };
        AutoStartCheck.IsChecked = AutoStartService.IsEnabled();
        ShowLunarCheck.IsChecked = settings.ShowLunar;
        ShowHolidayMarkCheck.IsChecked = settings.ShowHolidayMark;
        AutoSyncCheck.IsChecked = settings.AutoSyncHolidays;
        AutoCheckUpdateCheck.IsChecked = settings.AutoCheckUpdate;
        ForceManualClockCheck.IsChecked = settings.ForceManualClockRect;
        OffsetXBox.Text = settings.OffsetX.ToString();
        OffsetYBox.Text = settings.OffsetY.ToString();

        AboutText.Text =
            $"任务栏日历 {AppInfo.CurrentVersionTag}（.NET Framework 4.8 免安装版）\n" +
            $"官网：{AppInfo.WebsiteUrl}\n" +
            $"设置文件：{AppPaths.SettingsFilePath}\n" +
            $"日志目录：{AppPaths.LogDirectory}";

        UpdateHolidayVersionText();
        UpdateClockLocateStatus();
        _loading = false;
    }

    /// <summary>刷新节假日数据版本、覆盖年份与上次同步时间显示</summary>
    private void UpdateHolidayVersionText()
    {
        IReadOnlyList<int> years = _holidayService.GetCoveredYears();
        string yearText = years.Count > 0 ? string.Join("、", years) : "无";
        string lastSyncText = _holidayService.LastSyncUtc is { } lastSync
            ? lastSync.ToLocalTime().ToString("yyyy-MM-dd HH:mm")
            : "从未";
        HolidayVersionText.Text =
            $"当前数据版本：{_holidayService.Version}\n" +
            $"覆盖年份：{yearText}\n" +
            $"上次同步：{lastSyncText}";
    }

    /// <summary>从网络更新节假日数据（今年与明年）</summary>
    private async void OnUpdateHolidayClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button button)
        {
            return;
        }

        button.IsEnabled = false;
        UpdateStatusText.Text = "正在从网络获取…";

        try
        {
            int year = DateTime.Today.Year;
            HolidayUpdateResult result = await _holidayService.UpdateYearsAsync(year, year + 1);
            UpdateStatusText.Text = result.Success
                ? $"更新完成：{result.Message}"
                : $"未能更新：{result.Message}";
            UpdateHolidayVersionText();
        }
        catch (Exception ex)
        {
            Logger.Error("更新节假日数据异常", ex);
            UpdateStatusText.Text = $"更新失败：{ex.Message}";
        }
        finally
        {
            button.IsEnabled = true;
        }
    }

    /// <summary>界面变更后统一保存并广播</summary>
    private void ApplyFromUi()
    {
        if (_loading)
        {
            return;
        }

        AppSettings settings = SettingsService.Instance.Current;

        settings.WeekStart = WeekStartCombo.SelectedIndex == 1 ? WeekStartMode.Sunday : WeekStartMode.Monday;
        settings.Theme = ThemeCombo.SelectedIndex switch
        {
            1 => ThemeMode.Light,
            2 => ThemeMode.Dark,
            _ => ThemeMode.System,
        };
        settings.ShowLunar = ShowLunarCheck.IsChecked == true;
        settings.ShowHolidayMark = ShowHolidayMarkCheck.IsChecked == true;
        settings.AutoSyncHolidays = AutoSyncCheck.IsChecked == true;
        settings.AutoCheckUpdate = AutoCheckUpdateCheck.IsChecked == true;
        settings.ForceManualClockRect = ForceManualClockCheck.IsChecked == true;

        if (int.TryParse(OffsetXBox.Text, out int offsetX))
        {
            settings.OffsetX = MathCompat.Clamp(offsetX, -200, 200);
        }

        if (int.TryParse(OffsetYBox.Text, out int offsetY))
        {
            settings.OffsetY = MathCompat.Clamp(offsetY, -200, 200);
        }

        // 仅在状态不一致时写注册表
        bool autoStartWanted = AutoStartCheck.IsChecked == true;
        if (autoStartWanted != AutoStartService.IsEnabled())
        {
            AutoStartService.SetEnabled(autoStartWanted);
        }

        settings.AutoStart = autoStartWanted;

        SettingsService.Instance.Save();
    }

    private void OnSettingChanged(object sender, RoutedEventArgs e) => ApplyFromUi();

    private void OnSelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFromUi();

    private void OnOffsetChanged(object sender, RoutedEventArgs e) => ApplyFromUi();

    /// <summary>开始校准时钟位置：给用户 5 秒把鼠标移到时钟上，随后记录鼠标所在点</summary>
    private void OnCalibrateClockClick(object sender, RoutedEventArgs e)
    {
        if (_calibrateTimer is not null)
        {
            return;
        }

        _calibrateCountdown = 5;
        CalibrateClockButton.IsEnabled = false;
        ClockLocateStatusText.Text = $"请在 {_calibrateCountdown} 秒内把鼠标移到任务栏时钟上停住…";

        _calibrateTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(1),
            DispatcherPriority.Normal,
            OnCalibrateTick,
            Dispatcher);
        _calibrateTimer.Start();
    }

    private void OnCalibrateTick(object? sender, EventArgs e)
    {
        _calibrateCountdown--;
        if (_calibrateCountdown > 0)
        {
            ClockLocateStatusText.Text = $"请在 {_calibrateCountdown} 秒内把鼠标移到任务栏时钟上停住…";
            return;
        }

        _calibrateTimer?.Stop();
        _calibrateTimer = null;
        CalibrateClockButton.IsEnabled = true;

        if (!NativeMethods.GetCursorPos(out NativeMethods.POINT point))
        {
            ClockLocateStatusText.Text = "校准失败：无法获取鼠标位置，请重试。";
            return;
        }

        AppSettings settings = SettingsService.Instance.Current;
        settings.HasManualClockRect = true;
        settings.ManualClockRectX = point.X;
        settings.ManualClockRectY = point.Y;
        SettingsService.Instance.Save();

        Logger.Info($"手动校准时钟位置完成：中心 ({point.X},{point.Y})");
        UpdateClockLocateStatus();
    }

    /// <summary>清除手动校准位置</summary>
    private void OnClearCalibrationClick(object sender, RoutedEventArgs e)
    {
        AppSettings settings = SettingsService.Instance.Current;
        settings.HasManualClockRect = false;
        SettingsService.Instance.Save();
        UpdateClockLocateStatus();
        Logger.Info("已清除手动时钟位置校准");
    }

    /// <summary>刷新时钟定位状态显示</summary>
    private void UpdateClockLocateStatus()
    {
        AppSettings settings = SettingsService.Instance.Current;
        if (settings.HasManualClockRect)
        {
            ClearCalibrationButton.IsEnabled = true;
            ClockLocateStatusText.Text =
                $"已校准手动位置：中心点 ({settings.ManualClockRectX},{settings.ManualClockRectY})，" +
                $"区域 {settings.ManualClockRectWidth}×{settings.ManualClockRectHeight}。" +
                "自动定位不可用时会自动使用该位置；如与时钟有偏差，可重新校准或用下方偏移微调。";
        }
        else
        {
            ClearCalibrationButton.IsEnabled = false;
            ClockLocateStatusText.Text = "未校准手动位置（当前完全依赖自动定位）。";
        }
    }

    /// <summary>手动触发版本更新检查</summary>
    private async void OnCheckUpdateClick(object sender, RoutedEventArgs e)
    {
        CheckUpdateButton.IsEnabled = false;
        UpdateCheckStatusText.Text = "正在检查更新...";

        try
        {
            UpdateCheckResult result = await _updateService.CheckUpdateAsync(silent: false);
            if (result.HasUpdate && result.Info is not null)
            {
                UpdateCheckStatusText.Text = $"发现新版本 v{result.Info.Version}";
                var updateWin = new UpdateWindow(result.Info) { Owner = this };
                updateWin.ShowDialog();
            }
            else
            {
                UpdateCheckStatusText.Text = result.Message;
            }
        }
        catch (Exception ex)
        {
            UpdateCheckStatusText.Text = $"检查更新失败: {ex.Message}";
        }
        finally
        {
            CheckUpdateButton.IsEnabled = true;
        }
    }

    /// <summary>浏览器打开官网</summary>
    private void OnVisitWebsiteClick(object sender, RoutedEventArgs e)
    {
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = AppInfo.WebsiteUrl,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Logger.Error("打开官网失败", ex);
        }
    }

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
