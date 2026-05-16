using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LucidDreamCompanion.Services;

public class StatsService : IStatsService
{
    private const LucidityLevel LucidThreshold = LucidityLevel.BriefLucidity;

    private readonly IDbContextFactory<LucidDbContext> _factory;
    private readonly IAppSettingsService _settings;

    public StatsService(IDbContextFactory<LucidDbContext> factory, IAppSettingsService settings)
    {
        _factory = factory;
        _settings = settings;
    }

    public async Task<int?> GetFirstLucidEntryIdAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var id = await db.DreamEntries
            .Where(e => e.Lucidity >= LucidThreshold)
            .OrderBy(e => e.DreamDate).ThenBy(e => e.CreatedAt)
            .Select(e => (int?)e.Id)
            .FirstOrDefaultAsync(ct);
        return id;
    }

    public async Task<StatsSnapshot> GetSnapshotAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var loaded = await db.DreamEntries
            .AsNoTracking()
            .Include(e => e.Tags)
            .ToListAsync(ct);
        var entries = loaded
            .Select(e => new EntryRow(
                e.Id, e.DreamDate, e.Lucidity, e.SleepSessionId,
                e.Tags.Select(t => new TagRef(t.Name, t.ColorHex)).ToList()))
            .ToList();

        var today = DateOnly.FromDateTime(DateTime.Now);
        var entryDays = entries
            .Select(e => DateOnly.FromDateTime(e.DreamDate))
            .ToHashSet();

        var (current, longest) = ComputeStreaks(entryDays, today);

        var total = entries.Count;
        var totalLucid = entries.Count(e => e.Lucidity >= LucidThreshold);

        double Rate(IEnumerable<EntryRow> set)
        {
            var list = set.ToList();
            return list.Count == 0 ? 0 : (double)list.Count(e => e.Lucidity >= LucidThreshold) / list.Count;
        }

        var from30 = today.AddDays(-29);
        var from90 = today.AddDays(-89);
        var rateAll = Rate(entries);
        var rate30 = Rate(entries.Where(e => DateOnly.FromDateTime(e.DreamDate) >= from30));
        var rate90 = Rate(entries.Where(e => DateOnly.FromDateTime(e.DreamDate) >= from90));

        // Top tags
        var topTags = entries
            .SelectMany(e => e.Tags)
            .GroupBy(t => t.Name)
            .Select(g => new TagCount(g.Key, g.First().ColorHex, g.Count()))
            .OrderByDescending(t => t.Count)
            .Take(5)
            .ToList();

        // Heatmap — last 90 days
        var heatmap = new List<HeatmapDay>();
        for (var i = 89; i >= 0; i--)
        {
            var day = today.AddDays(-i);
            var dayEntries = entries.Where(e => DateOnly.FromDateTime(e.DreamDate) == day).ToList();
            heatmap.Add(new HeatmapDay(
                day,
                dayEntries.Count == 0 ? -1 : dayEntries.Max(e => (int)e.Lucidity),
                dayEntries.Count > 0,
                InFuture: false));
        }

        // Rolling 30-day lucidity rate, sampled weekly for 12 weeks
        var trend = new List<TrendPoint>();
        for (var w = 11; w >= 0; w--)
        {
            var end = today.AddDays(-7 * w);
            var start = end.AddDays(-29);
            var window = entries.Where(e =>
            {
                var d = DateOnly.FromDateTime(e.DreamDate);
                return d >= start && d <= end;
            });
            trend.Add(new TrendPoint(end, Rate(window)));
        }

        // Technique comparison
        var sessions = await db.SleepSessions
            .AsNoTracking()
            .Where(s => s.TechniqueProfileId != null)
            .Select(s => new { s.Id, s.TechniqueProfileId })
            .ToListAsync(ct);
        var profileNames = await db.CueProfiles
            .AsNoTracking()
            .ToDictionaryAsync(p => p.Id, p => p.Name, ct);

        var techniqueStats = new List<TechniqueStat>();
        foreach (var grp in sessions.GroupBy(s => s.TechniqueProfileId!.Value))
        {
            var sessionIds = grp.Select(s => s.Id).ToHashSet();
            var linkedDreams = entries
                .Where(e => e.SleepSessionId.HasValue && sessionIds.Contains(e.SleepSessionId.Value))
                .ToList();
            var name = profileNames.TryGetValue(grp.Key, out var n) ? n : "Profile";
            techniqueStats.Add(new TechniqueStat(
                name, grp.Count(), linkedDreams.Count, Rate(linkedDreams)));
        }
        techniqueStats = techniqueStats
            .OrderByDescending(t => t.LucidityRate)
            .ThenByDescending(t => t.Sessions)
            .ToList();

        // Reality checks — last 7 days
        var sevenDaysAgo = DateTime.UtcNow.AddDays(-7);
        var rcCompleted = await db.RealityCheckLogs
            .CountAsync(l => l.FiredAt >= sevenDaysAgo, ct);
        var settings = await _settings.GetAsync(ct);
        double? rcRate = null;
        if (settings.RealityCheckEnabled)
        {
            var perDay = settings.RealityCheckMode == RealityCheckMode.FixedInterval
                ? EstimateIntervalCount(settings)
                : settings.RealityCheckCountPerDay;
            var scheduled = Math.Max(1, perDay * 7);
            rcRate = Math.Min(1.0, (double)rcCompleted / scheduled);
        }

        return new StatsSnapshot(
            current, longest, total, totalLucid,
            rateAll, rate30, rate90,
            rcCompleted, rcRate,
            topTags, techniqueStats, heatmap, trend);
    }

    private static int EstimateIntervalCount(AppSettings s)
    {
        if (s.RealityCheckIntervalMinutes <= 0) return 0;
        var startMin = s.RealityCheckStart.Hour * 60 + s.RealityCheckStart.Minute;
        var endMin = s.RealityCheckEnd.Hour * 60 + s.RealityCheckEnd.Minute;
        var span = endMin - startMin;
        return span <= 0 ? 0 : span / s.RealityCheckIntervalMinutes + 1;
    }

    private static (int Current, int Longest) ComputeStreaks(HashSet<DateOnly> days, DateOnly today)
    {
        if (days.Count == 0) return (0, 0);

        // Current streak: count back from today, or from yesterday if today has no entry yet.
        var cursor = days.Contains(today) ? today : today.AddDays(-1);
        var current = 0;
        while (days.Contains(cursor))
        {
            current++;
            cursor = cursor.AddDays(-1);
        }

        // Longest streak across all recorded days.
        var sorted = days.OrderBy(d => d).ToList();
        var longest = 1;
        var run = 1;
        for (var i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] == sorted[i - 1].AddDays(1))
            {
                run++;
                longest = Math.Max(longest, run);
            }
            else
            {
                run = 1;
            }
        }
        return (current, Math.Max(longest, current));
    }

    private record TagRef(string Name, string ColorHex);
    private record EntryRow(int Id, DateTime DreamDate, LucidityLevel Lucidity, int? SleepSessionId, List<TagRef> Tags);
}
