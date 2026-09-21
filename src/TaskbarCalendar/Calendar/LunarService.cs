using System.Globalization;

namespace TaskbarCalendar.Calendar;

/// <summary>
/// 农历、节气与传统节日计算服务（基于 .NET 内置 ChineseLunisolarCalendar，支持 1901-2100）
/// </summary>
public static class LunarService
{
    private static readonly ChineseLunisolarCalendar Lunar = new();

    private static readonly string[] LunarDayNames =
    {
        "初一", "初二", "初三", "初四", "初五", "初六", "初七", "初八", "初九", "初十",
        "十一", "十二", "十三", "十四", "十五", "十六", "十七", "十八", "十九", "二十",
        "廿一", "廿二", "廿三", "廿四", "廿五", "廿六", "廿七", "廿八", "廿九", "三十",
    };

    private static readonly string[] LunarMonthNames =
    {
        "正月", "二月", "三月", "四月", "五月", "六月",
        "七月", "八月", "九月", "十月", "冬月", "腊月",
    };

    private static readonly string[] GanNames = { "甲", "乙", "丙", "丁", "戊", "己", "庚", "辛", "壬", "癸" };

    private static readonly string[] ZhiNames = { "子", "丑", "寅", "卯", "辰", "巳", "午", "未", "申", "酉", "戌", "亥" };

    private static readonly string[] ZodiacNames = { "鼠", "牛", "虎", "兔", "龙", "蛇", "马", "羊", "猴", "鸡", "狗", "猪" };

    /// <summary>二十四节气名称（按公历月内顺序排列）</summary>
    private static readonly string[] SolarTermNames =
    {
        "小寒", "大寒", "立春", "雨水", "惊蛰", "春分", "清明", "谷雨",
        "立夏", "小满", "芒种", "夏至", "小暑", "大暑", "立秋", "处暑",
        "白露", "秋分", "寒露", "霜降", "立冬", "小雪", "大雪", "冬至",
    };

    /// <summary>21 世纪二十四节气寿星公式常数表（个别年份存在 ±1 天误差）</summary>
    private static readonly double[] SolarTermConstants =
    {
        5.4055, 20.12, 3.87, 18.73, 5.63, 20.646, 4.81, 20.1,
        5.52, 21.04, 5.678, 21.37, 7.108, 22.83, 7.5, 23.13,
        7.646, 23.042, 8.318, 23.438, 7.438, 22.36, 7.18, 21.94,
    };

    /// <summary>公历固定节日</summary>
    private static readonly Dictionary<(int Month, int Day), string> SolarFestivals = new()
    {
        [(1, 1)] = "元旦",
        [(2, 14)] = "情人节",
        [(3, 8)] = "妇女节",
        [(3, 12)] = "植树节",
        [(4, 1)] = "愚人节",
        [(5, 1)] = "劳动节",
        [(5, 4)] = "青年节",
        [(6, 1)] = "儿童节",
        [(7, 1)] = "建党节",
        [(8, 1)] = "建军节",
        [(9, 10)] = "教师节",
        [(10, 1)] = "国庆节",
        [(12, 25)] = "圣诞节",
    };

    /// <summary>日期是否在农历支持范围内</summary>
    public static bool IsSupported(DateTime date)
        => date >= Lunar.MinSupportedDateTime.Date && date <= Lunar.MaxSupportedDateTime.Date;

    /// <summary>获取日历格子中的农历文本：初一显示月名，其余显示日名</summary>
    public static string GetLunarDayText(DateTime date)
    {
        if (!IsSupported(date))
        {
            return string.Empty;
        }

        int day = Lunar.GetDayOfMonth(date);
        if (day == 1)
        {
            return GetLunarMonthName(date);
        }

        return LunarDayNames[day - 1];
    }

    /// <summary>获取完整农历日期文本，如 "正月初一"</summary>
    public static string GetLunarFullText(DateTime date)
    {
        if (!IsSupported(date))
        {
            return string.Empty;
        }

        return GetLunarMonthName(date) + LunarDayNames[Lunar.GetDayOfMonth(date) - 1];
    }

    /// <summary>获取农历月份名（含闰月处理）</summary>
    public static string GetLunarMonthName(DateTime date)
    {
        if (!IsSupported(date))
        {
            return string.Empty;
        }

        int year = Lunar.GetYear(date);
        int month = Lunar.GetMonth(date);
        int leapMonth = Lunar.GetLeapMonth(year);

        bool isLeap = leapMonth > 0 && month == leapMonth;
        int nominalMonth = month;
        if (leapMonth > 0 && month > leapMonth)
        {
            nominalMonth = month - 1;
        }

        string name = LunarMonthNames[Math.Clamp(nominalMonth - 1, 0, LunarMonthNames.Length - 1)];
        return isLeap ? "闰" + name : name;
    }

    /// <summary>获取传统节日名（非闰月节日），无则为 null</summary>
    public static string? GetTraditionalFestival(DateTime date)
    {
        if (!IsSupported(date))
        {
            return null;
        }

        int year = Lunar.GetYear(date);
        int month = Lunar.GetMonth(date);
        int day = Lunar.GetDayOfMonth(date);
        int leapMonth = Lunar.GetLeapMonth(year);

        bool isLeap = leapMonth > 0 && month == leapMonth;
        int nominalMonth = month;
        if (leapMonth > 0 && month > leapMonth)
        {
            nominalMonth = month - 1;
        }

        if (!isLeap)
        {
            switch ((nominalMonth, day))
            {
                case (1, 1): return "春节";
                case (1, 15): return "元宵节";
                case (5, 5): return "端午节";
                case (7, 7): return "七夕节";
                case (7, 15): return "中元节";
                case (8, 15): return "中秋节";
                case (9, 9): return "重阳节";
                case (12, 8): return "腊八节";
            }
        }

        // 除夕：次日的农历日为正月初一
        var next = date.AddDays(1);
        if (IsSupported(next)
            && Lunar.GetMonth(next) == 1
            && Lunar.GetDayOfMonth(next) == 1)
        {
            return "除夕";
        }

        return null;
    }

    /// <summary>获取公历固定节日名，无则为 null</summary>
    public static string? GetSolarFestival(DateTime date)
        => SolarFestivals.TryGetValue((date.Month, date.Day), out string? name) ? name : null;

    /// <summary>获取节气名（无则为 null；寿星公式，个别年份可能有 ±1 天误差）</summary>
    public static string? GetSolarTerm(DateTime date)
    {
        int year = date.Year;
        if (year is < 2000 or > 2099)
        {
            return null;
        }

        int y = year % 100;
        int monthIndex = (date.Month - 1) * 2;
        for (int i = 0; i < 2; i++)
        {
            int termIndex = monthIndex + i;
            int day = (int)(y * 0.2422 + SolarTermConstants[termIndex]) - (y / 4);
            if (date.Day == day)
            {
                return SolarTermNames[termIndex];
            }
        }

        return null;
    }

    /// <summary>获取农历年干支文本，如 "丙午年"</summary>
    public static string GetGanZhiYear(DateTime date)
    {
        if (!IsSupported(date))
        {
            return string.Empty;
        }

        int index = (Lunar.GetYear(date) - 4) % 60;
        if (index < 0)
        {
            index += 60;
        }

        return GanNames[index % 10] + ZhiNames[index % 12] + "年";
    }

    /// <summary>获取生肖文本</summary>
    public static string GetZodiac(DateTime date)
    {
        if (!IsSupported(date))
        {
            return string.Empty;
        }

        int index = (Lunar.GetYear(date) - 4) % 12;
        if (index < 0)
        {
            index += 12;
        }

        return ZodiacNames[index];
    }
}
