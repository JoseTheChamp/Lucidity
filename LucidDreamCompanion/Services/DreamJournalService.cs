using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LucidDreamCompanion.Services;

public class DreamJournalService : IDreamJournalService
{
    private readonly IDbContextFactory<LucidDbContext> _dbFactory;
    private readonly ILogger<DreamJournalService> _logger;

    public event Action? Changed;

    public DreamJournalService(IDbContextFactory<LucidDbContext> dbFactory, ILogger<DreamJournalService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<DreamEntry>> ListAsync(JournalQuery? query = null, CancellationToken ct = default)
    {
        query ??= new JournalQuery();
        await using var db = await _dbFactory.CreateDbContextAsync(ct);

        IQueryable<DreamEntry> q = db.DreamEntries
            .Include(e => e.Tags)
            .AsNoTracking();

        if (query.MinLucidity is { } minL)
        {
            q = q.Where(e => e.Lucidity >= minL);
        }
        if (query.FromDate is { } from)
        {
            q = q.Where(e => e.DreamDate >= from);
        }
        if (query.ToDate is { } to)
        {
            q = q.Where(e => e.DreamDate <= to);
        }
        if (query.TagIds is { Count: > 0 } tagIds)
        {
            foreach (var id in tagIds)
            {
                q = q.Where(e => e.Tags.Any(t => t.Id == id));
            }
        }
        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var s = query.Search.Trim();
            q = q.Where(e => EF.Functions.Like(e.Title, $"%{s}%") || EF.Functions.Like(e.Body, $"%{s}%"));
        }

        return await q
            .OrderByDescending(e => e.DreamDate)
            .ThenByDescending(e => e.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<DreamEntry?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.DreamEntries
            .Include(e => e.Tags)
            .AsNoTracking()
            .FirstOrDefaultAsync(e => e.Id == id, ct);
    }

    public async Task<DreamEntry> CreateAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var now = DateTime.UtcNow;
        var local = DateTime.Now;

        // Auto-link to a sleep session that's active or ended within the last 12 hours.
        var cutoff = now.AddHours(-12);
        var session = await db.SleepSessions
            .AsNoTracking()
            .Where(s => s.EndedAt == null || s.EndedAt >= cutoff)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);

        var dreamDate = session is not null
            ? session.StartedAt.ToLocalTime().Date
            : local.Date;

        var entry = new DreamEntry
        {
            CreatedAt = now,
            DreamDate = dreamDate,
            Title = "",
            Body = "",
            Lucidity = LucidityLevel.None,
            SleepSessionId = session?.Id,
        };
        db.DreamEntries.Add(entry);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Created dream entry {Id} (session {Session})", entry.Id, session?.Id);
        Changed?.Invoke();
        return entry;
    }

    public async Task<IReadOnlyList<SleepSession>> ListRecentSessionsAsync(int take = 12, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.SleepSessions
            .AsNoTracking()
            .OrderByDescending(s => s.StartedAt)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task SaveAsync(DreamEntry entry, IEnumerable<int> tagIds, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tracked = await db.DreamEntries
            .Include(e => e.Tags)
            .FirstOrDefaultAsync(e => e.Id == entry.Id, ct);
        if (tracked is null)
        {
            _logger.LogWarning("SaveAsync: entry {Id} not found", entry.Id);
            return;
        }
        tracked.Title = entry.Title;
        tracked.Body = entry.Body;
        tracked.Lucidity = entry.Lucidity;
        tracked.SleepQuality = entry.SleepQuality;
        tracked.DreamDate = entry.DreamDate;
        tracked.SleepSessionId = entry.SleepSessionId;

        var desiredIds = tagIds.ToHashSet();
        tracked.Tags.RemoveAll(t => !desiredIds.Contains(t.Id));
        var existingIds = tracked.Tags.Select(t => t.Id).ToHashSet();
        var toAdd = desiredIds.Except(existingIds).ToList();
        if (toAdd.Count > 0)
        {
            var newTags = await db.Tags.Where(t => toAdd.Contains(t.Id)).ToListAsync(ct);
            tracked.Tags.AddRange(newTags);
        }
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var entry = await db.DreamEntries.FirstOrDefaultAsync(e => e.Id == id, ct);
        if (entry is null) return;
        db.DreamEntries.Remove(entry);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Deleted dream entry {Id}", id);
        Changed?.Invoke();
    }

    public async Task RestoreAsync(DreamEntry entry, IEnumerable<int> tagIds, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var ids = tagIds.ToList();
        var tags = ids.Count > 0
            ? await db.Tags.Where(t => ids.Contains(t.Id)).ToListAsync(ct)
            : new List<Tag>();
        var copy = new DreamEntry
        {
            CreatedAt = entry.CreatedAt,
            DreamDate = entry.DreamDate,
            Title = entry.Title,
            Body = entry.Body,
            Lucidity = entry.Lucidity,
            SleepQuality = entry.SleepQuality,
            SleepSessionId = entry.SleepSessionId,
            Tags = tags,
        };
        db.DreamEntries.Add(copy);
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }
}
