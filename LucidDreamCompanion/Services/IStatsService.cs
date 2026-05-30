namespace LucidDreamCompanion.Services;

public record TagCount(string Name, string ColorHex, int Count);
public record TechniqueStat(string Name, int Sessions, int Dreams, double LucidityRate);
public record HeatmapDay(DateOnly Date, int MaxLucidity, bool HasEntry, bool InFuture);
public record TrendPoint(DateOnly Date, double Rate);

public record StatsSnapshot(
    int CurrentStreak,
    int LongestStreak,
    int TotalEntries,
    int TotalLucid,
    double LucidityRate,        // 0..1, all-time
    double LucidityRate30d,
    double LucidityRate90d,
    int RealityChecks7d,
    double? RealityCheckRate7d, // null when reality checks are disabled
    int RecordedNights,         // completed sleep sessions, all-time
    IReadOnlyList<TagCount> TopTags,
    IReadOnlyList<TechniqueStat> Techniques,
    IReadOnlyList<HeatmapDay> Heatmap,
    IReadOnlyList<TrendPoint> Trend);

public enum StatsPeriod { Week, Month, Quarter, Year }

public record SleepAverages(
    int SessionCount,
    TimeOnly? AvgBedtime,
    TimeOnly? AvgWakeTime,
    TimeSpan? AvgDuration);

public interface IStatsService
{
    Task<StatsSnapshot> GetSnapshotAsync(CancellationToken ct = default);
    Task<int?> GetFirstLucidEntryIdAsync(CancellationToken ct = default);
    Task<SleepAverages> GetSleepAveragesAsync(StatsPeriod period, CancellationToken ct = default);
}
