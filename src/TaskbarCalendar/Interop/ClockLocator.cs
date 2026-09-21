using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Automation;
using TaskbarCalendar.Services;

namespace TaskbarCalendar.Interop;

/// <summary>时钟区域矩形（物理像素）</summary>
public readonly record struct ClockRect(int Left, int Top, int Right, int Bottom)
{
    public int Width => Right - Left;

    public int Height => Bottom - Top;
}

/// <summary>
/// 通过 UI Automation 定位任务栏时钟按钮，并在后台线程周期跟踪其位置变化。
/// 多级兜底：类名匹配 → 内部时间文本父级 → 时间格式文本按钮。
/// </summary>
public sealed class ClockLocator : IDisposable
{
    /// <summary>不同 Windows 版本下时钟按钮的类名候选</summary>
    private static readonly string[] ClockClassNames =
    {
        "SystemTray.OmniButtonLeft",
        "SystemTray.ClockButton",
    };

    private const string TrayClassName = "Shell_TrayWnd";
    private const string TimeInnerTextId = "TimeInnerTextBlock";

    private static readonly Regex TimeTextRegex = new(@"^\d{1,2}[:：]\d{2}", RegexOptions.Compiled);

    private readonly object _sync = new();
    private readonly CancellationTokenSource _cts = new();

    private Thread? _thread;
    private AutomationElement? _cachedClock;
    private volatile bool _refreshRequested;

    /// <summary>当前时钟矩形（物理像素），未找到时为 null</summary>
    public ClockRect? CurrentRect { get; private set; }

    /// <summary>时钟矩形变化时触发（首次定位成功也触发；定位失败时参数为 null）</summary>
    public event EventHandler<ClockRect?>? ClockRectChanged;

    /// <summary>请求立即重新定位（清除缓存元素，最迟 1 秒内生效）</summary>
    public void RefreshNow() => _refreshRequested = true;

    public void Start()
    {
        _thread = new Thread(PollLoop)
        {
            IsBackground = true,
            Name = "ClockLocator",
        };
        _thread.Start();
        Logger.Info("时钟定位线程已启动");
    }

    private void PollLoop()
    {
        while (!_cts.IsCancellationRequested)
        {
            if (_refreshRequested)
            {
                _refreshRequested = false;
                _cachedClock = null;
            }

            ClockRect? rect = TryGetClockRect();

            bool changed;
            lock (_sync)
            {
                changed = rect != CurrentRect;
                if (changed)
                {
                    CurrentRect = rect;
                }
            }

            if (changed)
            {
                Logger.Info(rect is { } r
                    ? $"时钟位置更新: ({r.Left},{r.Top})-({r.Right},{r.Bottom})"
                    : "未找到任务栏时钟");
                ClockRectChanged?.Invoke(this, rect);
            }

            // 等待 1 秒或收到取消信号
            _cts.Token.WaitHandle.WaitOne(1000);
        }
    }

    private ClockRect? TryGetClockRect()
    {
        try
        {
            AutomationElement? clock = _cachedClock;
            if (clock is null)
            {
                clock = FindClockElement();
                if (clock is null)
                {
                    return null;
                }

                _cachedClock = clock;
            }

            // 一次读取属性快照，避免多次跨进程查询
            AutomationElement.AutomationElementInformation info = clock.Current;
            if (info.IsOffscreen)
            {
                // 任务栏自动隐藏等场景：元素移出屏幕，隐藏覆盖层
                return null;
            }

            var bounds = info.BoundingRectangle;
            if (bounds.IsEmpty || bounds.Width < 1 || bounds.Height < 1)
            {
                return null;
            }

            return new ClockRect(
                (int)Math.Round(bounds.Left),
                (int)Math.Round(bounds.Top),
                (int)Math.Round(bounds.Right),
                (int)Math.Round(bounds.Bottom));
        }
        catch (ElementNotAvailableException)
        {
            // 元素失效（任务栏重建等），下次循环重新定位
            _cachedClock = null;
            return null;
        }
        catch (Exception ex)
        {
            Logger.Error("时钟定位异常", ex);
            _cachedClock = null;
            return null;
        }
    }

    private static AutomationElement? FindClockElement()
    {
        var root = AutomationElement.RootElement;
        var tray = root.FindFirst(
            TreeScope.Children,
            new PropertyCondition(AutomationElement.ClassNameProperty, TrayClassName));
        if (tray is null)
        {
            return null;
        }

        // 策略 1：类名精确匹配
        foreach (string className in ClockClassNames)
        {
            var condition = new AndCondition(
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button),
                new PropertyCondition(AutomationElement.ClassNameProperty, className));
            var element = tray.FindFirst(TreeScope.Descendants, condition);
            if (element is not null)
            {
                return element;
            }
        }

        // 策略 2：通过内部时间文本定位其父级按钮
        var timeText = tray.FindFirst(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.AutomationIdProperty, TimeInnerTextId));
        if (timeText is not null)
        {
            var parent = TreeWalker.ControlViewWalker.GetParent(timeText);
            if (parent is not null)
            {
                return parent;
            }
        }

        // 策略 3：名称符合时间格式的按钮
        var buttons = tray.FindAll(
            TreeScope.Descendants,
            new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
        foreach (AutomationElement button in buttons)
        {
            string name = button.Current.Name ?? string.Empty;
            if (TimeTextRegex.IsMatch(name))
            {
                return button;
            }
        }

        return null;
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
