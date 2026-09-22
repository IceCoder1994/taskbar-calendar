using System.Diagnostics;
using System.Windows;
using TaskbarCalendar.Services;

// WinForms 全局 using 与 WPF 同名类型消歧
using MessageBox = System.Windows.MessageBox;

namespace TaskbarCalendar.UI;

/// <summary>
/// 新版本更新确认与自动就地替换交互窗口
/// </summary>
public partial class UpdateWindow : Window
{
    private readonly UpdateInfo _info;
    private readonly UpdateService _updateService = new();
    private bool _isUpdating;

    public UpdateWindow(UpdateInfo info)
    {
        _info = info;
        InitializeComponent();

        TitleText.Text = $"发现新版本 v{_info.Version}";
        string dateStr = string.IsNullOrWhiteSpace(_info.ReleaseDate) ? "最新发布" : _info.ReleaseDate;
        SubTitleText.Text = $"当前版本: {AppInfo.CurrentVersionTag} · 发布日期: {dateStr}";

        ChangelogText.Text = string.IsNullOrWhiteSpace(_info.Changelog)
            ? "修复已知问题并提升稳定性。"
            : _info.Changelog.Replace("\\n", "\n");
    }

    private async void OnUpdateClick(object sender, RoutedEventArgs e)
    {
        if (_isUpdating)
        {
            return;
        }

        _isUpdating = true;
        UpdateButton.IsEnabled = false;
        CancelButton.IsEnabled = false;
        ProgressPanel.Visibility = Visibility.Visible;
        DownloadBar.Value = 0;
        StatusText.Text = "正在连接并下载更新包...";

        var progress = new Progress<int>(percent =>
        {
            DownloadBar.Value = percent;
            StatusText.Text = percent < 100
                ? $"正在下载更新包... {percent}%"
                : "下载完成，正在解压并准备重启...";
        });

        try
        {
            await _updateService.DownloadAndApplyUpdateAsync(_info, progress);
            // 正常情况下批处理拉起后主程序已执行 Shutdown()
        }
        catch (Exception ex)
        {
            Logger.Error("自动更新执行失败", ex);
            _isUpdating = false;
            UpdateButton.IsEnabled = true;
            CancelButton.IsEnabled = true;
            ProgressPanel.Visibility = Visibility.Collapsed;

            MessageBox.Show(
                $"自动更新遇到问题：{ex.Message}\n\n建议您直接前往官网下载最新版本解压覆盖。",
                "更新失败",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void OnOpenWebsiteClick(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo
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

    private void OnCancelClick(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
