using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using TaskbarCalendar.Services;

namespace TaskbarCalendar.Calendar;

/// <summary>节假日类型</summary>
public enum HolidayType
{
    /// <summary>普通日期</summary>
    None,

    /// <summary>法定放假日（休）</summary>
    Holiday,

    /// <summary>调休上班日（班）</summary>
    Workday,
}

/// <summary>某一天的节假日信息</summary>
public sealed record HolidayInfo(string Name, HolidayType Type);

/// <summary>节假日在线更新结果</summary>
public sealed record HolidayUpdateResult(
    bool Success,
    string Message,
    IReadOnlyList<int> UpdatedYears,
    IReadOnlyList<int> NotPublishedYears);

/// <summary>
/// 法定节假日与调休数据服务：从 Data/holidays.json 加载，支持在线更新（手动 + 自动）
/// </summary>
public sealed class HolidayService
{
    private const string ApiBaseUrl = "https://timor.tech/api/holiday/year/";
    private const string DefaultNotPublishedHint = "请稍后再试";

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        // 与内置数据文件保持一致的 camelCase 字段风格
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // 保持中文原样输出，便于人工查阅与编辑
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>跨实例同步锁：防止自动同步与手动同步并发执行</summary>
    private static readonly SemaphoreSlim SyncLock = new(1, 1);

    private Dictionary<string, HolidayInfo> _days = new();

    /// <summary>数据文件被在线更新后触发（UI 线程）</summary>
    public static event EventHandler? DataFileUpdated;

    /// <summary>数据版本号（来自数据文件）</summary>
    public string Version { get; private set; } = "无";

    /// <summary>上次成功同步时间（UTC），从未同步为 null</summary>
    public DateTime? LastSyncUtc { get; private set; }

    public void Load()
    {
        try
        {
            string path = AppPaths.HolidaysFilePath;
            if (!File.Exists(path))
            {
                Logger.Warn($"节假日数据文件不存在: {path}");
                return;
            }

            using FileStream stream = File.OpenRead(path);
            HolidayFile? file = JsonSerializer.Deserialize<HolidayFile>(stream, ReadOptions);
            if (file?.Days is null)
            {
                Logger.Warn("节假日数据文件内容为空");
                return;
            }

            var days = new Dictionary<string, HolidayInfo>(file.Days.Count);
            foreach ((string date, HolidayDayEntry entry) in file.Days)
            {
                HolidayType type = entry.Type?.ToLowerInvariant() switch
                {
                    "holiday" => HolidayType.Holiday,
                    "workday" => HolidayType.Workday,
                    _ => HolidayType.None,
                };
                days[date] = new HolidayInfo(entry.Name ?? string.Empty, type);
            }

            _days = days;
            Version = file.Version ?? "未知";
            LastSyncUtc = DateTime.TryParse(
                file.LastSync,
                CultureInfo.InvariantCulture,
                DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal,
                out DateTime lastSync)
                ? lastSync
                : null;
            Logger.Info($"节假日数据已加载: 版本 {Version}，共 {days.Count} 条");
        }
        catch (Exception ex)
        {
            Logger.Error("加载节假日数据失败", ex);
        }
    }

    /// <summary>获取指定日期的节假日信息，无则为 null</summary>
    public HolidayInfo? Get(DateTime date)
        => _days.TryGetValue(date.ToString("yyyy-MM-dd"), out HolidayInfo? info) ? info : null;

    /// <summary>获取数据已覆盖的年份列表</summary>
    public IReadOnlyList<int> GetCoveredYears()
    {
        var years = new SortedSet<int>();
        foreach (string key in _days.Keys)
        {
            if (key.Length >= 4 && int.TryParse(key.AsSpan(0, 4), out int year))
            {
                years.Add(year);
            }
        }

        return years.ToList();
    }

    /// <summary>
    /// 按需自动同步：受设置开关与最小间隔约束（未到间隔或已关闭时直接跳过）。
    /// 返回是否实际执行了同步。
    /// </summary>
    public async Task<bool> MaybeAutoSyncAsync(TimeSpan minimumInterval)
    {
        if (!SettingsService.Instance.Current.AutoSyncHolidays)
        {
            Logger.Info("自动同步已关闭，跳过");
            return false;
        }

        if (LastSyncUtc is { } lastSync && DateTime.UtcNow - lastSync < minimumInterval)
        {
            Logger.Info($"距上次同步不足 {minimumInterval.TotalHours:0} 小时，跳过自动同步");
            return false;
        }

        int year = DateTime.Today.Year;
        Logger.Info($"节假日自动同步开始（上次同步：{LastSyncUtc?.ToString("u") ?? "从未"}）");
        HolidayUpdateResult result = await UpdateYearsAsync(year, year + 1);
        Logger.Info($"节假日自动同步结束：{result.Message}");
        return true;
    }

    /// <summary>
    /// 从网络更新指定年份的节假日数据并合并保存到本地文件。
    /// 未公布的年份会被跳过并提示；更新按年份独立容错。
    /// </summary>
    public async Task<HolidayUpdateResult> UpdateYearsAsync(params int[] years)
    {
        await SyncLock.WaitAsync();
        try
        {
            return await UpdateYearsCoreAsync(years);
        }
        finally
        {
            SyncLock.Release();
        }
    }

    private async Task<HolidayUpdateResult> UpdateYearsCoreAsync(int[] years)
    {
        var updatedYears = new List<int>();
        var notPublishedYears = new List<int>();
        var failedYears = new List<int>();
        var newEntries = new Dictionary<string, HolidayInfo>();

        using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        http.DefaultRequestHeaders.UserAgent.ParseAdd("TaskbarCalendar/1.0");

        foreach (int year in years)
        {
            try
            {
                string json = await http.GetStringAsync($"{ApiBaseUrl}{year}");
                Dictionary<string, HolidayInfo> entries = ParseYearResponse(json, year);
                if (entries.Count == 0)
                {
                    notPublishedYears.Add(year);
                    continue;
                }

                foreach ((string key, HolidayInfo value) in entries)
                {
                    newEntries[key] = value;
                }

                updatedYears.Add(year);
            }
            catch (Exception ex)
            {
                Logger.Error($"更新 {year} 年节假日数据失败", ex);
                failedYears.Add(year);
            }
        }

        if (newEntries.Count == 0)
        {
            string failMessage = BuildMessage(updatedYears, notPublishedYears, failedYears);
            Logger.Warn($"节假日在线更新未获得新数据: {failMessage}");

            // 全部请求成功只是暂无数据（如次年尚未公布）时同样记录检查时间，避免高频重复请求
            if (failedYears.Count == 0)
            {
                LastSyncUtc = DateTime.UtcNow;
                SaveToFile();
            }

            return new HolidayUpdateResult(false, failMessage, updatedYears, notPublishedYears);
        }

        // 合并：移除目标年份的旧数据后写入新数据，其他年份保持不变
        var merged = new Dictionary<string, HolidayInfo>(_days);
        foreach (string key in _days.Keys)
        {
            if (key.Length >= 4
                && int.TryParse(key.AsSpan(0, 4), out int keyYear)
                && updatedYears.Contains(keyYear))
            {
                merged.Remove(key);
            }
        }

        foreach ((string key, HolidayInfo value) in newEntries)
        {
            merged[key] = value;
        }

        _days = merged;
        Version = $"{string.Join("、", updatedYears)}（在线更新 {DateTime.Now:yyyy-MM-dd}）";
        LastSyncUtc = DateTime.UtcNow;
        SaveToFile();

        string message = BuildMessage(updatedYears, notPublishedYears, failedYears);
        Logger.Info($"节假日在线更新完成: {message}");
        DataFileUpdated?.Invoke(this, EventArgs.Empty);
        return new HolidayUpdateResult(true, message, updatedYears, notPublishedYears);
    }

    /// <summary>解析接口单年响应，返回日期 → 节假日信息（无数据返回空字典）</summary>
    private static Dictionary<string, HolidayInfo> ParseYearResponse(string json, int year)
    {
        var result = new Dictionary<string, HolidayInfo>();
        using JsonDocument doc = JsonDocument.Parse(json);
        JsonElement root = doc.RootElement;

        if (!root.TryGetProperty("code", out JsonElement codeElement) || codeElement.GetInt32() != 0)
        {
            throw new InvalidOperationException("接口返回失败状态");
        }

        if (!root.TryGetProperty("holiday", out JsonElement holidayElement)
            || holidayElement.ValueKind != JsonValueKind.Object)
        {
            return result;
        }

        foreach (JsonProperty property in holidayElement.EnumerateObject())
        {
            JsonElement item = property.Value;
            if (!item.TryGetProperty("holiday", out JsonElement holidayFlag)
                || !item.TryGetProperty("date", out JsonElement dateElement))
            {
                continue;
            }

            string? date = dateElement.GetString();
            if (string.IsNullOrEmpty(date) || !date.StartsWith(year.ToString(), StringComparison.Ordinal))
            {
                continue;
            }

            bool isHoliday = holidayFlag.GetBoolean();
            string name;
            if (isHoliday)
            {
                name = item.TryGetProperty("name", out JsonElement nameElement)
                    ? nameElement.GetString() ?? string.Empty
                    : string.Empty;
            }
            else
            {
                // 补班日：统一命名为"目标节日 + 调休"，与内置数据风格一致
                name = item.TryGetProperty("target", out JsonElement targetElement)
                    ? $"{targetElement.GetString()}调休"
                    : item.TryGetProperty("name", out JsonElement fallbackElement)
                        ? fallbackElement.GetString() ?? string.Empty
                        : string.Empty;
            }

            result[date] = new HolidayInfo(name, isHoliday ? HolidayType.Holiday : HolidayType.Workday);
        }

        return result;
    }

    /// <summary>将内存数据写回本地数据文件</summary>
    private void SaveToFile()
    {
        try
        {
            var file = new HolidayFile
            {
                Version = Version,
                Source = "在线更新（timor.tech 节假日接口）",
                LastSync = LastSyncUtc?.ToString("O"),
                Days = new Dictionary<string, HolidayDayEntry>(_days.Count),
            };

            foreach ((string key, HolidayInfo value) in _days)
            {
                file.Days[key] = new HolidayDayEntry
                {
                    Name = value.Name,
                    Type = value.Type == HolidayType.Holiday ? "holiday" : "workday",
                };
            }

            Directory.CreateDirectory(Path.GetDirectoryName(AppPaths.HolidaysFilePath)!);
            string json = JsonSerializer.Serialize(file, WriteOptions);
            File.WriteAllText(AppPaths.HolidaysFilePath, json, new UTF8Encoding(false));
            Logger.Info($"节假日数据已写入: {AppPaths.HolidaysFilePath}");
        }
        catch (Exception ex)
        {
            Logger.Error("保存节假日数据失败", ex);
        }
    }

    private static string BuildMessage(List<int> updated, List<int> notPublished, List<int> failed)
    {
        var parts = new List<string>();
        if (updated.Count > 0)
        {
            parts.Add($"已更新 {string.Join("、", updated)} 年");
        }

        if (notPublished.Count > 0)
        {
            parts.Add($"{string.Join("、", notPublished)} 年数据尚未公布，{DefaultNotPublishedHint}");
        }

        if (failed.Count > 0)
        {
            parts.Add($"{string.Join("、", failed)} 年更新失败（请检查网络）");
        }

        return parts.Count > 0 ? string.Join("；", parts) : "未获取到数据";
    }

    private sealed class HolidayFile
    {
        public string? Version { get; set; }

        public string? Source { get; set; }

        /// <summary>上次成功同步时间（ISO 8601 UTC）</summary>
        public string? LastSync { get; set; }

        public Dictionary<string, HolidayDayEntry>? Days { get; set; }
    }

    private sealed class HolidayDayEntry
    {
        public string? Name { get; set; }

        public string? Type { get; set; }
    }
}
