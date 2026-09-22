using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

// WinForms 与 WPF 同名类型消歧
using MessageBox = System.Windows.MessageBox;

namespace TaskbarCalendar.Services;

/// <summary>
/// 托盘图标与右键菜单服务
/// </summary>
public sealed class TrayService : IDisposable
{
    private NotifyIcon? _notifyIcon;
    private ContextMenuStrip? _menu;
    private ToolStripMenuItem? _autoStartItem;
    private Icon? _trayIcon;

    /// <summary>请求打开日历面板</summary>
    public event EventHandler? OpenCalendarRequested;

    /// <summary>请求打开设置窗口</summary>
    public event EventHandler? OpenSettingsRequested;

    /// <summary>请求重新定位时钟</summary>
    public event EventHandler? RelocateRequested;

    public void Initialize()
    {
        _menu = new ContextMenuStrip();
        _menu.Opening += (_, _) => UpdateAutoStartItem();

        var openItem = new ToolStripMenuItem("打开日历(&C)");
        openItem.Click += (_, _) => OpenCalendarRequested?.Invoke(this, EventArgs.Empty);
        _menu.Items.Add(openItem);

        _menu.Items.Add(new ToolStripSeparator());

        var settingsItem = new ToolStripMenuItem("设置(&S)...");
        settingsItem.Click += (_, _) => OpenSettingsRequested?.Invoke(this, EventArgs.Empty);
        _menu.Items.Add(settingsItem);

        _autoStartItem = new ToolStripMenuItem("开机自启");
        _autoStartItem.Click += (_, _) => ToggleAutoStart();
        _menu.Items.Add(_autoStartItem);

        var relocateItem = new ToolStripMenuItem("重新定位时钟(&R)");
        relocateItem.Click += (_, _) => RelocateRequested?.Invoke(this, EventArgs.Empty);
        _menu.Items.Add(relocateItem);

        _menu.Items.Add(new ToolStripSeparator());

        var logItem = new ToolStripMenuItem("打开日志目录(&L)");
        logItem.Click += (_, _) => OpenLogFolder();
        _menu.Items.Add(logItem);

        var aboutItem = new ToolStripMenuItem("关于(&A)");
        aboutItem.Click += (_, _) => ShowAbout();
        _menu.Items.Add(aboutItem);

        _menu.Items.Add(new ToolStripSeparator());

        var exitItem = new ToolStripMenuItem("退出(&X)");
        exitItem.Click += (_, _) => System.Windows.Application.Current.Shutdown();
        _menu.Items.Add(exitItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = LoadAppIcon(),
            Text = "任务栏日历",
            Visible = true,
            ContextMenuStrip = _menu,
        };
        _notifyIcon.DoubleClick += (_, _) => OpenCalendarRequested?.Invoke(this, EventArgs.Empty);

        UpdateAutoStartItem();
        Logger.Info("托盘图标已就绪");
    }

    /// <summary>在鼠标位置弹出托盘右键菜单</summary>
    public void ShowMenu()
    {
        _menu?.Show(Cursor.Position);
    }

    /// <summary>显示托盘气泡提示</summary>
    public void ShowBalloon(string title, string text)
    {
        _notifyIcon?.ShowBalloonTip(5000, title, text, ToolTipIcon.Info);
    }

    /// <summary>加载应用图标：优先从 exe 嵌入图标提取，失败退回系统默认图标</summary>
    private Icon LoadAppIcon()
    {
        try
        {
            // .NET Framework 下无 ProcessPath API，改用进程主模块路径；图标优先取 exe 嵌入图标
            string? exePath = System.Diagnostics.Process.GetCurrentProcess().MainModule?.FileName;
            if (!string.IsNullOrEmpty(exePath))
            {
                Icon? extracted = Icon.ExtractAssociatedIcon(exePath);
                if (extracted is not null)
                {
                    _trayIcon = extracted;
                    return extracted;
                }
            }
        }
        catch (Exception ex)
        {
            Logger.Error("从可执行文件提取图标失败", ex);
        }

        try
        {
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", "app.ico");
            if (File.Exists(path))
            {
                _trayIcon = new Icon(path, new Size(32, 32));
                return _trayIcon;
            }
        }
        catch (Exception ex)
        {
            Logger.Error("加载应用图标失败", ex);
        }

        _trayIcon = SystemIcons.Application;
        return _trayIcon;
    }

    private void ToggleAutoStart()
    {
        bool enable = !AutoStartService.IsEnabled();
        if (AutoStartService.SetEnabled(enable))
        {
            SettingsService.Instance.Current.AutoStart = enable;
            SettingsService.Instance.Save();
        }

        UpdateAutoStartItem();
    }

    private void UpdateAutoStartItem()
    {
        if (_autoStartItem is not null)
        {
            _autoStartItem.Text = AutoStartService.IsEnabled() ? "开机自启：已开启" : "开机自启：已关闭";
        }
    }

    private static void ShowAbout()
    {
        MessageBox.Show(
            "任务栏日历 v1.0.0\n\n点击任务栏时钟弹出本日历，替代系统原生日历面板。\n设置文件与日志可在托盘菜单中打开。",
            "关于 任务栏日历",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private static void OpenLogFolder()
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogDirectory);
            Process.Start(new ProcessStartInfo
            {
                FileName = AppPaths.LogDirectory,
                UseShellExecute = true,
            });
        }
        catch (Exception ex)
        {
            Logger.Error("打开日志目录失败", ex);
        }
    }

    public void Dispose()
    {
        if (_notifyIcon is not null)
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
            _notifyIcon = null;
        }

        _menu?.Dispose();
        _menu = null;

        // SystemIcons.Application 为共享资源，无需释放
        if (_trayIcon is not null && !ReferenceEquals(_trayIcon, SystemIcons.Application))
        {
            _trayIcon.Dispose();
        }

        _trayIcon = null;
    }
}
