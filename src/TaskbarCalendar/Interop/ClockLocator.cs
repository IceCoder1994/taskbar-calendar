using System.Text;
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
/// 通过 UI Automation 定位任务栏时钟按钮，并在后台线程周期跟踪其位置。
/// 多级兜底：类名+名称校验 → 名称特征+位置 → 类名兜底；自动全部失败时回退到用户手动校准的位置。
/// 定位持续失败时输出任务栏结构诊断快照，便于远程排查与适配。
/// </summary>
public sealed class ClockLocator : IDisposable
{
    /// <summary>各 Windows 版本下时钟按钮的类名候选（顺序即优先级）</summary>
    private static readonly string[] ClockClassNames =
    {
        "SystemTray.OmniButton",      // Win11 25H2+
        "SystemTray.OmniButtonLeft",  // Win11 22H2 - 24H2
        "SystemTray.ClockButton",     // 部分 Win11
        "ClockButton",                // Win10
    };

    private const string TrayClassName = "Shell_TrayWnd";
    private const string SystemTrayClassPrefix = "SystemTray";

    /// <summary>连续失败达到该次数后输出一次诊断快照</summary>
    private const int DiagnosticFailThreshold = 5;

    /// <summary>时间文本特征（不锚定开头以兼容"时间 14:33"等前缀；支持全角冒号与 12/24 小时制）</summary>
    private static readonly Regex TimeRegex = new(@"\d{1,2}\s*[:：]\s*\d{2}", RegexOptions.Compiled);

    /// <summary>
    /// 日期文本特征（覆盖 2026/9/21、2026-09-21、2026年9月21日、9/21、9月21日）。
    /// 数字段之间允许少量非数字字符（如 UIA 名称中的 Unicode 双向标记）。
    /// </summary>
    private static readonly Regex DateRegex = new(
        @"\d{4}[/\-年]\D{0,2}\d{1,2}[/\-月]\D{0,2}\d{1,2}日?|\d{1,2}[/\-月]\D{0,2}\d{1,2}日?",
        RegexOptions.Compiled);

    private readonly object _sync = new();
    private readonly CancellationTokenSource _cts = new();

    private Thread? _thread;
    private AutomationElement? _cachedClock;
    private string _cachedStrategy = string.Empty;
    private volatile bool _refreshRequested;
    private int _consecutiveFails;
    private bool _diagnosticLogged;

    /// <summary>当前时钟矩形（物理像素），未找到时为 null</summary>
    public ClockRect? CurrentRect { get; private set; }

    /// <summary>时钟矩形变化时触发（首次定位成功也触发；定位失败时参数为 null）</summary>
    public event EventHandler<ClockRect?>? ClockRectChanged;

    /// <summary>当前是否在使用手动校准位置（自动定位失败时的兜底）</summary>
    public bool IsUsingManualRect { get; private set; }

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
                if (rect is { } r)
                {
                    string mode = IsUsingManualRect ? "手动校准" : _cachedStrategy;
                    Logger.Info($"时钟位置更新: ({r.Left},{r.Top})-({r.Right},{r.Bottom}) [{mode}]");
                }
                else
                {
                    Logger.Warn("未找到任务栏时钟（自动定位与手动校准均不可用）");
                }

                ClockRectChanged?.Invoke(this, rect);
            }

            // 持续失败时输出一次任务栏结构诊断快照，便于远程排查
            if (rect is null)
            {
                _consecutiveFails++;
                if (_consecutiveFails >= DiagnosticFailThreshold && !_diagnosticLogged)
                {
                    _diagnosticLogged = true;
                    Logger.Warn("连续多次未定位到任务栏时钟，输出任务栏结构诊断快照");
                    LogTrayDiagnostics();
                }
            }
            else
            {
                _consecutiveFails = 0;
                _diagnosticLogged = false;
            }

            // 等待 1 秒或收到取消信号
            _cts.Token.WaitHandle.WaitOne(1000);
        }
    }

    private ClockRect? TryGetClockRect()
    {
        AppSettings settings = SettingsService.Instance.Current;

        // 用户选择始终使用手动位置时直接返回
        if (settings.ForceManualClockRect)
        {
            ClockRect? forced = BuildManualRect(settings);
            IsUsingManualRect = forced is not null;
            return forced;
        }

        ClockRect? auto = TryGetAutoClockRect();
        if (auto is not null)
        {
            IsUsingManualRect = false;
            return auto;
        }

        // 自动定位失败：回退到手动校准位置（如已完成校准）
        ClockRect? manual = BuildManualRect(settings);
        IsUsingManualRect = manual is not null;
        return manual;
    }

    private ClockRect? TryGetAutoClockRect()
    {
        try
        {
            AutomationElement? clock = _cachedClock;
            if (clock is null)
            {
                var found = FindClockElement();
                if (found is null)
                {
                    return null;
                }

                _cachedClock = found.Value.Element;
                _cachedStrategy = found.Value.Strategy;
                clock = _cachedClock;
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

    /// <summary>根据设置构建手动校准矩形，未校准时返回 null</summary>
    private static ClockRect? BuildManualRect(AppSettings settings)
    {
        if (!settings.HasManualClockRect)
        {
            return null;
        }

        int width = MathCompat.Clamp(settings.ManualClockRectWidth, 20, 400);
        int height = MathCompat.Clamp(settings.ManualClockRectHeight, 10, 200);
        int left = settings.ManualClockRectX - (width / 2);
        int top = settings.ManualClockRectY - (height / 2);
        return new ClockRect(left, top, left + width, top + height);
    }

    /// <summary>
    /// 多级兜底查找时钟按钮：
    /// ① 类名候选 + 名称校验（类名可能被多个按钮共用，必须逐项校验）；
    /// ② 名称特征 + 位置（限定 SystemTray 前缀，取最靠右者，与类名和语言无关）；
    /// ③ 类名候选兜底（不校验名称，取最靠右者）。
    /// </summary>
    private static (AutomationElement Element, string Strategy)? FindClockElement()
    {
        var root = AutomationElement.RootElement;
        var tray = root.FindFirst(
            TreeScope.Children,
            new PropertyCondition(AutomationElement.ClassNameProperty, TrayClassName));
        if (tray is null)
        {
            return null;
        }

        var buttonCondition = new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button);

        // 策略 1：类名候选 + 名称校验（多个同类名候选取最靠右，防止共用类名时选错）
        foreach (string className in ClockClassNames)
        {
            var condition = new AndCondition(
                buttonCondition,
                new PropertyCondition(AutomationElement.ClassNameProperty, className));

            AutomationElement? best = null;
            double bestRight = double.MinValue;
            foreach (AutomationElement candidate in tray.FindAll(TreeScope.Descendants, condition))
            {
                if (!IsClockLikeName(candidate.Current.Name))
                {
                    continue;
                }

                double right = candidate.Current.BoundingRectangle.Right;
                if (right > bestRight)
                {
                    best = candidate;
                    bestRight = right;
                }
            }

            if (best is not null)
            {
                return (best, $"策略1:类名{className}+名称校验");
            }
        }

        // 策略 2：名称特征 + 位置（时钟恒位于通知区域，取名称匹配且最靠右的 SystemTray 按钮）
        AutomationElement? rightmost = null;
        double maxRight = double.MinValue;
        foreach (AutomationElement button in tray.FindAll(TreeScope.Descendants, buttonCondition))
        {
            string className = button.Current.ClassName ?? string.Empty;
            if (!className.StartsWith(SystemTrayClassPrefix, StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsClockLikeName(button.Current.Name))
            {
                continue;
            }

            double right = button.Current.BoundingRectangle.Right;
            if (right > maxRight)
            {
                rightmost = button;
                maxRight = right;
            }
        }

        if (rightmost is not null)
        {
            return (rightmost, "策略2:名称特征+位置");
        }

        // 策略 3：类名候选兜底（不校验名称，多候选取最靠右）
        AutomationElement? fallback = null;
        double fallbackRight = double.MinValue;
        foreach (string className in ClockClassNames)
        {
            var condition = new AndCondition(
                buttonCondition,
                new PropertyCondition(AutomationElement.ClassNameProperty, className));

            foreach (AutomationElement candidate in tray.FindAll(TreeScope.Descendants, condition))
            {
                double right = candidate.Current.BoundingRectangle.Right;
                if (right > fallbackRight)
                {
                    fallback = candidate;
                    fallbackRight = right;
                }
            }
        }

        return fallback is not null ? (fallback, "策略3:类名兜底") : null;
    }

    /// <summary>名称是否包含时间或日期特征（与系统语言无关）</summary>
    private static bool IsClockLikeName(string? name)
        => !string.IsNullOrEmpty(name) && (TimeRegex.IsMatch(name) || DateRegex.IsMatch(name));

    /// <summary>输出任务栏按钮结构快照（仅诊断用，便于未适配系统的问题排查）</summary>
    private static void LogTrayDiagnostics()
    {
        try
        {
            var root = AutomationElement.RootElement;
            var tray = root.FindFirst(
                TreeScope.Children,
                new PropertyCondition(AutomationElement.ClassNameProperty, TrayClassName));
            if (tray is null)
            {
                Logger.Warn("诊断：未找到任务栏窗口 Shell_TrayWnd");
                return;
            }

            var sb = new StringBuilder();
            sb.AppendLine("诊断：任务栏按钮结构快照（排查定位失败时可附此段日志）");

            var buttons = tray.FindAll(
                TreeScope.Descendants,
                new PropertyCondition(AutomationElement.ControlTypeProperty, ControlType.Button));
            foreach (AutomationElement button in buttons)
            {
                AutomationElement.AutomationElementInformation info = button.Current;
                string name = (info.Name ?? string.Empty).Replace("\r", " ").Replace("\n", " ");
                if (name.Length > 60)
                {
                    name = name.Substring(0, 60) + "…";
                }

                var bounds = info.BoundingRectangle;
                sb.AppendLine(
                    $"  [{info.ClassName}] ID=[{info.AutomationId}] " +
                    $"Rect=({(int)bounds.Left},{(int)bounds.Top})-({(int)bounds.Right},{(int)bounds.Bottom}) Name=[{name}]");
            }

            Logger.Warn(sb.ToString());
        }
        catch (Exception ex)
        {
            Logger.Error("生成任务栏诊断信息失败", ex);
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
