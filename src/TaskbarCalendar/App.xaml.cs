using System.Net;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using TaskbarCalendar.Calendar;
using TaskbarCalendar.Interop;
using TaskbarCalendar.Overlay;
using TaskbarCalendar.Services;
using TaskbarCalendar.UI;

// WinForms 与 WPF 存在同名类型（UseWindowsForms 会注入全局 using），此处显式消歧
using Application = System.Windows.Application;
using MessageBox = System.Windows.MessageBox;

namespace TaskbarCalendar;

/// <summary>
/// 应用程序入口：负责单实例、全局异常、托盘与核心链路（定位 / 覆盖层 / 弹窗）的生命周期管理
/// </summary>
public partial class App : Application
{
    private const string MutexName = @"Global\TaskbarCalendar.SingleInstance";

    private Mutex? _singleInstanceMutex;
    private TrayService? _trayService;
    private ClockLocator? _clockLocator;
    private ClickOverlay? _clickOverlay;
    private CalendarPopupWindow? _popup;
    private SettingsWindow? _settingsWindow;
    private ClockRect? _currentClockRect;
    private DispatcherTimer? _topmostTimer;
    private DispatcherTimer? _autoSyncTimer;
    private HolidayService? _autoSyncService;
    private readonly UpdateService _updateService = new();
    private int _locateFailCount;
    private bool _locateWarned;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // .NET Framework 默认不启用 TLS 1.2，节假日同步走 HTTPS 必须显式开启（幂等，可重复调用）
        ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

        // 单实例检查：重复启动时提示并退出
        _singleInstanceMutex = new Mutex(true, MutexName, out bool createdNew);
        if (!createdNew)
        {
            MessageBox.Show("任务栏日历已在运行中。", "任务栏日历", MessageBoxButton.OK, MessageBoxImage.Information);
            Shutdown();
            return;
        }

        // 注册全局异常处理，避免崩溃静默退出
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += OnAppDomainUnhandledException;
        TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;

        SettingsService.Instance.Load();
        Logger.Info("应用启动");

        _trayService = new TrayService();
        _trayService.Initialize();
        _trayService.OpenCalendarRequested += (_, _) => Dispatcher.BeginInvoke(ShowCalendar);
        _trayService.OpenSettingsRequested += (_, _) => Dispatcher.BeginInvoke(ShowSettings);
        _trayService.CheckUpdateRequested += (_, _) => Dispatcher.BeginInvoke(ManualCheckUpdate);
        _trayService.RelocateRequested += (_, _) => Dispatcher.BeginInvoke(RelocateClock);

        StartClockOverlay();
        StartAutoSync();

        // 支持 --settings 参数直接打开设置窗口（便于创建快捷方式）
        if (e.Args.Any(arg => string.Equals(arg, "--settings", StringComparison.OrdinalIgnoreCase)))
        {
            Dispatcher.BeginInvoke(ShowSettings);
        }
    }

    /// <summary>启动时钟定位与透明点击层（核心链路）</summary>
    private void StartClockOverlay()
    {
        _clockLocator = new ClockLocator();
        _clockLocator.ClockRectChanged += OnClockRectChanged;
        _clockLocator.Start();

        _clickOverlay = new ClickOverlay();
        _clickOverlay.Create();
        _clickOverlay.LeftClicked += (_, _) => Dispatcher.BeginInvoke(ToggleCalendar);
        _clickOverlay.RightClicked += (_, _) => Dispatcher.BeginInvoke(() => _trayService?.ShowMenu());

        // 定期将覆盖层保持在任务栏之上，防御 z-order 被 explorer 重排
        _topmostTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(1),
            DispatcherPriority.Background,
            (_, _) => _clickOverlay?.BringToTop(),
            Dispatcher);
        _topmostTimer.Start();
    }

    /// <summary>启动节假日数据自动同步：启动 10 秒后首次检查，之后每 24 小时一次</summary>
    private void StartAutoSync()
    {
        _autoSyncService = new HolidayService();
        _autoSyncService.Load();

        _autoSyncTimer = new DispatcherTimer(
            TimeSpan.FromSeconds(10),
            DispatcherPriority.Background,
            OnAutoSyncTick,
            Dispatcher);
        _autoSyncTimer.Start();
    }

    private async void OnAutoSyncTick(object? sender, EventArgs e)
    {
        // 首次触发后改为每 24 小时检查一次
        if (_autoSyncTimer is not null)
        {
            _autoSyncTimer.Interval = TimeSpan.FromHours(24);
        }

        try
        {
            await _autoSyncService!.MaybeAutoSyncAsync(TimeSpan.FromHours(24));
            await CheckUpdateQuietlyAsync();
        }
        catch (Exception ex)
        {
            Logger.Error("后台自动同步或检查更新异常", ex);
        }
    }

    /// <summary>后台静默检查新版本，发现更新时弹出气泡通知</summary>
    private async Task CheckUpdateQuietlyAsync()
    {
        try
        {
            UpdateCheckResult? result = await _updateService.MaybeAutoCheckAsync(TimeSpan.FromHours(24));
            if (result is not null && result.HasUpdate && result.Info is not null)
            {
                _trayService?.NotifyUpdateAvailable(result.Info);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("后台自动检查更新失败", ex);
        }
    }

    /// <summary>托盘菜单手动点击检查更新</summary>
    private async void ManualCheckUpdate()
    {
        try
        {
            UpdateCheckResult result = await _updateService.CheckUpdateAsync(silent: false);
            if (result.HasUpdate && result.Info is not null)
            {
                var updateWin = new UpdateWindow(result.Info);
                updateWin.Show();
                updateWin.Activate();
            }
            else
            {
                MessageBox.Show(
                    result.Message,
                    "检查更新",
                    MessageBoxButton.OK,
                    result.Success ? MessageBoxImage.Information : MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("手动检查更新失败", ex);
            MessageBox.Show($"检查更新失败：{ex.Message}", "检查更新", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void OnClockRectChanged(object? sender, ClockRect? rect)
    {
        // 事件来自后台线程：应用偏移校准并切回 UI 线程更新覆盖层
        ClockRect? adjusted = ApplyOffset(rect);
        Dispatcher.BeginInvoke(() =>
        {
            _currentClockRect = adjusted;
            _clickOverlay?.Update(adjusted);
            UpdateLocateState(adjusted is not null);
        });
    }

    /// <summary>跟踪定位状态：持续失败时给出托盘提示</summary>
    private void UpdateLocateState(bool located)
    {
        if (located)
        {
            _locateFailCount = 0;
            _locateWarned = false;
            return;
        }

        _locateFailCount++;
        if (_locateFailCount >= 10 && !_locateWarned)
        {
            _locateWarned = true;
            Logger.Warn("连续 10 次未定位到任务栏时钟，显示托盘提示");
            _trayService?.ShowBalloon("未找到任务栏时钟", "点击托盘菜单中的「重新定位时钟」，或在设置中校准位置偏移。");
        }
    }

    /// <summary>应用设置中的位置校准偏移</summary>
    private static ClockRect? ApplyOffset(ClockRect? rect)
    {
        if (rect is not { } r)
        {
            return null;
        }

        AppSettings settings = SettingsService.Instance.Current;
        if (settings.OffsetX == 0 && settings.OffsetY == 0)
        {
            return r;
        }

        return r with
        {
            Left = r.Left + settings.OffsetX,
            Top = r.Top + settings.OffsetY,
            Right = r.Right + settings.OffsetX,
            Bottom = r.Bottom + settings.OffsetY,
        };
    }

    /// <summary>点击时钟：显示或收起日历面板</summary>
    private void ToggleCalendar()
    {
        if (_popup is { IsVisible: true })
        {
            _popup.Hide();
            return;
        }

        ShowCalendar();
    }

    private void ShowCalendar()
    {
        _popup ??= new CalendarPopupWindow();
        _popup.ShowNear(_currentClockRect);
    }

    /// <summary>重新定位时钟并立即刷新覆盖层</summary>
    private void RelocateClock()
    {
        _clockLocator?.RefreshNow();
        Logger.Info("已请求重新定位时钟");
    }

    /// <summary>打开设置窗口；已打开时仅激活</summary>
    private void ShowSettings()
    {
        if (_settingsWindow is { IsVisible: true })
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow();
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
        _settingsWindow.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Logger.Info("应用退出");
        _topmostTimer?.Stop();
        _autoSyncTimer?.Stop();
        _clickOverlay?.Dispose();
        _clockLocator?.Dispose();
        _popup?.Close();
        _settingsWindow?.Close();
        _trayService?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        Logger.Error("UI 线程未处理异常", e.Exception);
        e.Handled = true;
    }

    private void OnAppDomainUnhandledException(object sender, UnhandledExceptionEventArgs e)
    {
        Logger.Error("后台线程未处理异常", e.ExceptionObject as Exception);
    }

    private void OnUnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        Logger.Error("未观察的任务异常", e.Exception);
        e.SetObserved();
    }
}
