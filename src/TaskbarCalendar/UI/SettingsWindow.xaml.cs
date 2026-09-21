using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using TaskbarCalendar.Calendar;
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

    private bool _loading;

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
        OffsetXBox.Text = settings.OffsetX.ToString();
        OffsetYBox.Text = settings.OffsetY.ToString();

        AboutText.Text =
            $"任务栏日历 v1.0.0\n" +
            $"设置文件：{AppPaths.SettingsFilePath}\n" +
            $"日志目录：{AppPaths.LogDirectory}";

        UpdateHolidayVersionText();
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

        if (int.TryParse(OffsetXBox.Text, out int offsetX))
        {
            settings.OffsetX = Math.Clamp(offsetX, -200, 200);
        }

        if (int.TryParse(OffsetYBox.Text, out int offsetY))
        {
            settings.OffsetY = Math.Clamp(offsetY, -200, 200);
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

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();
}
