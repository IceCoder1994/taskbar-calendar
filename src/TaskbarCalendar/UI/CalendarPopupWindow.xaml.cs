using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using TaskbarCalendar.Calendar;
using TaskbarCalendar.Interop;
using TaskbarCalendar.Services;

// WinForms 全局 using 与 WPF 同名类型消歧
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using MouseWheelEventArgs = System.Windows.Input.MouseWheelEventArgs;

namespace TaskbarCalendar.UI;

/// <summary>
/// 日历弹出面板：月视图、农历 / 节气 / 节假日、年 / 月选择器、详情栏
/// </summary>
public partial class CalendarPopupWindow : Window
{
    private const int PanelWidthDip = 360;
    private const int PanelHeightDip = 460;
    private const int GapDip = 8;
    private const int MarginDip = 8;
    private const int YearRangeStart = 2000;
    private const int YearRangeEnd = 2100;

    private static readonly string[] WeekNamesMondayFirst = { "一", "二", "三", "四", "五", "六", "日" };

    private readonly HolidayService _holidayService = new();

    private DateTime _today = DateTime.Today;
    private DateTime _selectedDate = DateTime.Today;
    private DateTime _viewMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    private bool _lightTheme;
    private Brush _primaryText = Brushes.White;
    private Brush _secondaryText = Brushes.White;
    private Brush _mutedText = Brushes.Gray;
    private Brush _accent = Brushes.DodgerBlue;
    private Brush _accentForeground = Brushes.Black;
    private Brush _todayBackground = Brushes.Transparent;
    private Brush _weekend = Brushes.Salmon;
    private Brush _holiday = Brushes.Salmon;
    private Brush _markBackground = Brushes.Transparent;
    private Brush _markForeground = Brushes.White;
    private readonly Brush _transparent = Brushes.Transparent;

    private bool _suppressYearSelection;

    public CalendarPopupWindow()
    {
        InitializeComponent();
        Width = PanelWidthDip;
        Height = PanelHeightDip;

        _holidayService.Load();
        BuildMonthPicker();
        ApplyTheme();
        BuildWeekHeader();
        RebuildCalendar();
        UpdateDetail();

        // 设置变化时立即刷新面板（保存发生在 UI 线程）
        SettingsService.Instance.Changed += OnSettingsChanged;

        // 节假日数据在线更新后刷新面板
        HolidayService.DataFileUpdated += OnHolidayDataUpdated;
    }

    /// <summary>显示面板并按时钟位置定位（时钟未知时停靠在右下角）</summary>
    public void ShowNear(ClockRect? clock)
    {
        RefreshIfNeeded();

        if (!IsVisible)
        {
            Show();
        }

        MoveTo(clock);

        // 尝试激活：保证失焦关闭与键盘操作可用
        Activate();
        NativeMethods.SetForegroundWindow(new WindowInteropHelper(this).Handle);
    }

    /// <summary>跨天或系统主题变化时刷新界面（打开面板时调用）</summary>
    private void RefreshIfNeeded()
    {
        bool light = ResolveLightTheme();
        bool dateChanged = DateTime.Today != _today;
        if (!dateChanged && light == _lightTheme)
        {
            return;
        }

        if (dateChanged)
        {
            _today = DateTime.Today;
            _selectedDate = _today;
            _viewMonth = new DateTime(_today.Year, _today.Month, 1);
        }

        ClosePickers();
        ApplyTheme();
        BuildWeekHeader();
        RebuildCalendar();
        UpdateDetail();
    }

    private void OnSettingsChanged(object? sender, EventArgs e)
    {
        ApplyTheme();
        BuildWeekHeader();
        RebuildCalendar();
        UpdateDetail();
    }

    /// <summary>节假日数据在线更新后重新加载并刷新视图</summary>
    private void OnHolidayDataUpdated(object? sender, EventArgs e)
    {
        Dispatcher.BeginInvoke(() =>
        {
            _holidayService.Load();
            RebuildCalendar();
            UpdateDetail();
        });
    }

    /// <summary>解析当前应使用的主题（考虑设置中的主题模式）</summary>
    private static bool ResolveLightTheme() => SettingsService.Instance.Current.Theme switch
    {
        ThemeMode.Light => true,
        ThemeMode.Dark => false,
        _ => ThemeService.IsLightTheme(),
    };

    /// <summary>设置中是否周日起始</summary>
    private static bool IsSundayFirst()
        => SettingsService.Instance.Current.WeekStart == WeekStartMode.Sunday;

    /// <summary>按系统深浅色与强调色重建主题画刷</summary>
    private void ApplyTheme()
    {
        _lightTheme = ResolveLightTheme();
        Color accentColor = ThemeService.GetAccentColor()
            ?? (_lightTheme ? Color.FromRgb(0x00, 0x67, 0xC0) : Color.FromRgb(0x4C, 0xC2, 0xFF));

        Color primary = _lightTheme ? Color.FromRgb(0x1A, 0x1A, 0x1A) : Colors.White;
        Color secondary = _lightTheme ? Color.FromRgb(0x5A, 0x5A, 0x5A) : Color.FromArgb(0xB0, 0xFF, 0xFF, 0xFF);
        Color muted = _lightTheme ? Color.FromArgb(0xA0, 0x00, 0x00, 0x00) : Color.FromArgb(0x58, 0xFF, 0xFF, 0xFF);
        Color weekend = _lightTheme ? Color.FromRgb(0xC4, 0x2B, 0x1C) : Color.FromRgb(0xFF, 0x9E, 0x9E);
        Color background = _lightTheme ? Color.FromArgb(0xF2, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0xF0, 0x1F, 0x1F, 0x1F);
        Color border = _lightTheme ? Color.FromArgb(0x22, 0x00, 0x00, 0x00) : Color.FromArgb(0x28, 0xFF, 0xFF, 0xFF);
        Color divider = _lightTheme ? Color.FromArgb(0x18, 0x00, 0x00, 0x00) : Color.FromArgb(0x20, 0xFF, 0xFF, 0xFF);
        Color hover = _lightTheme ? Color.FromArgb(0x12, 0x00, 0x00, 0x00) : Color.FromArgb(0x18, 0xFF, 0xFF, 0xFF);
        Color todayBackground = Color.FromArgb(0x2E, accentColor.R, accentColor.G, accentColor.B);
        Color markBackground = _lightTheme ? Color.FromRgb(0xEA, 0xEA, 0xEA) : Color.FromArgb(0x30, 0xFF, 0xFF, 0xFF);
        Color accentForeground = IsDark(accentColor) ? Colors.White : Colors.Black;

        _primaryText = new SolidColorBrush(primary);
        _secondaryText = new SolidColorBrush(secondary);
        _mutedText = new SolidColorBrush(muted);
        _accent = new SolidColorBrush(accentColor);
        _accentForeground = new SolidColorBrush(accentForeground);
        _todayBackground = new SolidColorBrush(todayBackground);
        _weekend = new SolidColorBrush(weekend);
        _holiday = new SolidColorBrush(weekend);
        _markBackground = new SolidColorBrush(markBackground);
        _markForeground = new SolidColorBrush(weekend);

        Resources["AppBackground"] = new SolidColorBrush(background);
        Resources["AppBorder"] = new SolidColorBrush(border);
        Resources["PrimaryText"] = new SolidColorBrush(primary);
        Resources["SecondaryText"] = new SolidColorBrush(secondary);
        Resources["MutedText"] = new SolidColorBrush(muted);
        Resources["AccentBrush"] = new SolidColorBrush(accentColor);
        Resources["AccentForeground"] = new SolidColorBrush(accentForeground);
        Resources["TodayBackground"] = new SolidColorBrush(todayBackground);
        Resources["WeekEndText"] = new SolidColorBrush(weekend);
        Resources["HoverBackground"] = new SolidColorBrush(hover);
        Resources["DividerBrush"] = new SolidColorBrush(divider);
    }

    private static bool IsDark(Color color)
        => (0.299 * color.R) + (0.587 * color.G) + (0.114 * color.B) < 140;

    private void BuildWeekHeader()
    {
        bool sundayFirst = IsSundayFirst();
        string[] names = sundayFirst
            ? new[] { "日", "一", "二", "三", "四", "五", "六" }
            : WeekNamesMondayFirst;

        TextBlock[] headers = { WeekHeader0, WeekHeader1, WeekHeader2, WeekHeader3, WeekHeader4, WeekHeader5, WeekHeader6 };
        for (int i = 0; i < headers.Length; i++)
        {
            bool isWeekend = sundayFirst ? i is 0 or 6 : i >= 5;
            headers[i].Text = names[i];
            headers[i].Foreground = isWeekend ? _weekend : _secondaryText;
        }
    }

    /// <summary>重建 42 个日期单元格</summary>
    private void RebuildCalendar()
    {
        var cells = new List<DayCell>(42);
        int offset = IsSundayFirst()
            ? (int)_viewMonth.DayOfWeek
            : ((int)_viewMonth.DayOfWeek + 6) % 7;
        DateTime start = _viewMonth.AddDays(-offset);
        for (int i = 0; i < 42; i++)
        {
            cells.Add(CreateCell(start.AddDays(i)));
        }

        DayGrid.ItemsSource = cells;
        YearButton.Content = $"{_viewMonth.Year}年";
        MonthButton.Content = $"{_viewMonth.Month}月";
    }

    private DayCell CreateCell(DateTime date)
    {
        bool isCurrentMonth = date.Year == _viewMonth.Year && date.Month == _viewMonth.Month;
        bool isToday = date == _today;
        bool isSelected = date == _selectedDate.Date;
        bool isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;

        AppSettings settings = SettingsService.Instance.Current;
        HolidayInfo? holiday = _holidayService.Get(date);
        string? festival = LunarService.GetTraditionalFestival(date) ?? LunarService.GetSolarFestival(date);
        string? term = LunarService.GetSolarTerm(date);
        string lunarText = settings.ShowLunar
            ? festival ?? term ?? LunarService.GetLunarDayText(date)
            : festival ?? term ?? string.Empty;

        Brush dayBrush;
        Brush lunarBrush;
        Brush background = _transparent;

        if (isSelected)
        {
            background = _accent;
            dayBrush = _accentForeground;
            lunarBrush = _accentForeground;
        }
        else if (isToday)
        {
            background = _todayBackground;
            dayBrush = _accent;
            lunarBrush = _accent;
        }
        else if (!isCurrentMonth)
        {
            dayBrush = _mutedText;
            lunarBrush = _mutedText;
        }
        else
        {
            bool isRest = holiday?.Type == HolidayType.Holiday
                || (isWeekend && holiday?.Type != HolidayType.Workday);
            dayBrush = isRest ? _holiday : _primaryText;
            lunarBrush = _secondaryText;
        }

        string? mark = settings.ShowHolidayMark
            ? holiday?.Type switch
            {
                HolidayType.Holiday => "休",
                HolidayType.Workday => "班",
                _ => null,
            }
            : null;

        return new DayCell
        {
            Date = date,
            DayText = date.Day.ToString(),
            LunarText = lunarText,
            DayForeground = dayBrush,
            LunarForeground = lunarBrush,
            Background = background,
            HolidayMark = mark,
            HolidayMarkVisibility = mark is null ? Visibility.Collapsed : Visibility.Visible,
            HolidayMarkBackground = _markBackground,
            HolidayMarkForeground = _markForeground,
            Tooltip = BuildTooltip(date, festival, term, holiday),
        };
    }

    private static string BuildTooltip(DateTime date, string? festival, string? term, HolidayInfo? holiday)
    {
        var parts = new List<string>
        {
            date.ToString("yyyy-MM-dd"),
            LunarService.GetLunarFullText(date),
        };
        if (festival is not null)
        {
            parts.Add(festival);
        }

        if (term is not null && term != festival)
        {
            parts.Add(term);
        }

        if (holiday is not null)
        {
            parts.Add(holiday.Name);
        }

        return string.Join(" · ", parts);
    }

    private void UpdateDetail()
    {
        DateTime d = _selectedDate;
        DetailDateText.Text = $"{d:M月d日} {GetWeekName(d)}";

        string ganZhi = LunarService.GetGanZhiYear(d).TrimEnd('年');
        DetailLunarText.Text = $"{ganZhi}{LunarService.GetZodiac(d)}年 {LunarService.GetLunarFullText(d)}";

        string? festival = LunarService.GetTraditionalFestival(d) ?? LunarService.GetSolarFestival(d);
        string? term = LunarService.GetSolarTerm(d);
        HolidayInfo? holiday = _holidayService.Get(d);

        var parts = new List<string>();
        if (festival is not null)
        {
            parts.Add(festival);
        }

        if (term is not null && term != festival)
        {
            parts.Add(term);
        }

        if (holiday is not null)
        {
            if (holiday.Type == HolidayType.Workday)
            {
                parts.Add("调休上班");
            }
            else if (festival is null && term is null)
            {
                parts.Add(holiday.Name);
            }
        }

        DetailExtraText.Text = string.Join(" · ", parts);
    }

    private static string GetWeekName(DateTime date) => date.DayOfWeek switch
    {
        DayOfWeek.Monday => "周一",
        DayOfWeek.Tuesday => "周二",
        DayOfWeek.Wednesday => "周三",
        DayOfWeek.Thursday => "周四",
        DayOfWeek.Friday => "周五",
        DayOfWeek.Saturday => "周六",
        _ => "周日",
    };

    private void ChangeMonth(int delta)
    {
        ClosePickers();
        _viewMonth = _viewMonth.AddMonths(delta);
        RebuildCalendar();
    }

    private void MoveSelection(int days)
    {
        ClosePickers();
        _selectedDate = _selectedDate.AddDays(days);
        if (_selectedDate.Year != _viewMonth.Year || _selectedDate.Month != _viewMonth.Month)
        {
            _viewMonth = new DateTime(_selectedDate.Year, _selectedDate.Month, 1);
        }

        RebuildCalendar();
        UpdateDetail();
    }

    private void ClosePickers()
    {
        MainView.Visibility = Visibility.Visible;
        YearPickerView.Visibility = Visibility.Collapsed;
        MonthPickerView.Visibility = Visibility.Collapsed;
    }

    private void ShowPicker(UIElement view)
    {
        MainView.Visibility = Visibility.Hidden;
        YearPickerView.Visibility = Visibility.Collapsed;
        MonthPickerView.Visibility = Visibility.Collapsed;
        view.Visibility = Visibility.Visible;
    }

    private void BuildMonthPicker()
    {
        MonthPickerGrid.Children.Clear();
        var style = (Style)FindResource("NavButtonStyle");
        for (int m = 1; m <= 12; m++)
        {
            int month = m;
            var button = new Button
            {
                Content = $"{month}月",
                Style = style,
                FontSize = 13,
                Margin = new Thickness(4),
            };
            button.Click += (_, _) =>
            {
                _viewMonth = new DateTime(_viewMonth.Year, month, 1);
                ClosePickers();
                RebuildCalendar();
            };
            MonthPickerGrid.Children.Add(button);
        }
    }

    private void MoveTo(ClockRect? clock)
    {
        var dpi = System.Windows.Media.VisualTreeHelper.GetDpi(this);
        int widthPx = (int)Math.Round(Width * dpi.DpiScaleX);
        int heightPx = (int)Math.Round(Height * dpi.DpiScaleY);
        int gapPx = (int)Math.Round(GapDip * dpi.DpiScaleY);
        int marginPx = (int)Math.Round(MarginDip * dpi.DpiScaleX);

        NativeMethods.RECT work = NativeMethods.GetWorkArea();

        int left;
        int top;
        if (clock is { } c)
        {
            // 面板右缘与时钟右缘对齐，底边位于时钟上方
            left = c.Right - widthPx;
            top = c.Top - heightPx - gapPx;
        }
        else
        {
            left = work.Right - widthPx - marginPx;
            top = work.Bottom - heightPx - marginPx;
        }

        // 限制在工作区内
        left = Math.Max(work.Left + marginPx, Math.Min(left, work.Right - widthPx - marginPx));
        top = Math.Max(work.Top + marginPx, Math.Min(top, work.Bottom - heightPx - marginPx));

        var hwnd = new WindowInteropHelper(this).Handle;
        NativeMethods.SetWindowPos(
            hwnd,
            IntPtr.Zero,
            left,
            top,
            0,
            0,
            NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOACTIVATE);

        Logger.Info($"日历面板定位: ({left},{top}) 尺寸 {widthPx}x{heightPx}");
    }

    private void OnPrevMonthClick(object sender, RoutedEventArgs e) => ChangeMonth(-1);

    private void OnNextMonthClick(object sender, RoutedEventArgs e) => ChangeMonth(1);

    private void OnTodayClick(object sender, RoutedEventArgs e)
    {
        _today = DateTime.Today;
        _selectedDate = _today;
        _viewMonth = new DateTime(_today.Year, _today.Month, 1);
        ClosePickers();
        RebuildCalendar();
        UpdateDetail();
    }

    private void OnDayCellClick(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { DataContext: DayCell cell })
        {
            return;
        }

        _selectedDate = cell.Date;
        if (cell.Date.Year != _viewMonth.Year || cell.Date.Month != _viewMonth.Month)
        {
            _viewMonth = new DateTime(cell.Date.Year, cell.Date.Month, 1);
        }

        RebuildCalendar();
        UpdateDetail();
    }

    private void OnYearClick(object sender, RoutedEventArgs e)
    {
        YearList.ItemsSource ??= Enumerable.Range(YearRangeStart, YearRangeEnd - YearRangeStart + 1).ToList();
        _suppressYearSelection = true;
        YearList.SelectedItem = _viewMonth.Year;
        _suppressYearSelection = false;
        ShowPicker(YearPickerView);
        YearList.ScrollIntoView(_viewMonth.Year);
    }

    private void OnMonthClick(object sender, RoutedEventArgs e) => ShowPicker(MonthPickerView);

    private void OnYearSelected(object sender, SelectionChangedEventArgs e)
    {
        if (_suppressYearSelection || YearList.SelectedItem is not int year)
        {
            return;
        }

        _viewMonth = new DateTime(year, _viewMonth.Month, 1);
        ClosePickers();
        RebuildCalendar();
    }

    private void OnWindowMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (MainView.Visibility != Visibility.Visible)
        {
            return;
        }

        ChangeMonth(e.Delta > 0 ? -1 : 1);
        e.Handled = true;
    }

    private void OnDeactivated(object? sender, EventArgs e)
    {
        Hide();
    }

    private void OnPreviewKeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Hide();
                e.Handled = true;
                break;

            case Key.Left:
                MoveSelection(-1);
                e.Handled = true;
                break;

            case Key.Right:
                MoveSelection(1);
                e.Handled = true;
                break;

            case Key.Up:
                MoveSelection(-7);
                e.Handled = true;
                break;

            case Key.Down:
                MoveSelection(7);
                e.Handled = true;
                break;

            case Key.PageUp:
                ChangeMonth(-1);
                e.Handled = true;
                break;

            case Key.PageDown:
                ChangeMonth(1);
                e.Handled = true;
                break;
        }
    }
}
