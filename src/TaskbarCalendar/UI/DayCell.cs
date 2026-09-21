using System.Windows;
using System.Windows.Media;

// WinForms 全局 using 与 WPF 同名类型消歧
using Brush = System.Windows.Media.Brush;

namespace TaskbarCalendar.UI;

/// <summary>日历单元格展示数据（每次重建日历生成新集合，因此为不可变对象）</summary>
public sealed class DayCell
{
    /// <summary>对应的公历日期</summary>
    public required DateTime Date { get; init; }

    /// <summary>公历日文本</summary>
    public required string DayText { get; init; }

    /// <summary>副标题：节日 / 节气 / 农历日</summary>
    public required string LunarText { get; init; }

    /// <summary>公历日文字颜色</summary>
    public required Brush DayForeground { get; init; }

    /// <summary>副标题文字颜色</summary>
    public required Brush LunarForeground { get; init; }

    /// <summary>单元格背景</summary>
    public required Brush Background { get; init; }

    /// <summary>休息/上班角标文本（"休" / "班"）</summary>
    public string? HolidayMark { get; init; }

    /// <summary>角标可见性</summary>
    public required Visibility HolidayMarkVisibility { get; init; }

    /// <summary>角标背景色</summary>
    public required Brush HolidayMarkBackground { get; init; }

    /// <summary>角标文字色</summary>
    public required Brush HolidayMarkForeground { get; init; }

    /// <summary>悬停提示</summary>
    public string? Tooltip { get; init; }
}
