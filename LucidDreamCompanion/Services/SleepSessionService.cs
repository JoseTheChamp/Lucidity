using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LucidDreamCompanion.Services;

public class SleepSessionService : ISleepSessionService
{
    private readonly IDbContextFactory<LucidDbContext> _dbFactory;
    private readonly ICueSchedulerService _scheduler;
    private readonly INotificationService _notifications;
    private readonly ILogger<SleepSessionService> _logger;

    public event Action? Changed;

    public SleepSessionService(
        IDbContextFactory<LucidDbContext> dbFactory,
        ICueSchedulerService scheduler,
        INotificationService notifications,
        ILogger<SleepSessionService> logger)
    {
        _dbFactory = dbFactory;
        _scheduler = scheduler;
        _notifications = notifications;
        _logger = logger;
    }

    public async Task<SleepSession?> GetActiveAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.SleepSessions
            .AsNoTracking()
            .Where(s => s.EndedAt == null)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<SleepSession> StartAsync(int? cueProfileId, CancellationToken ct = default)
    {
        var existing = await GetActiveAsync(ct);
        if (existing is not null) return existing;

        SleepSession session;
        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            session = new SleepSession
            {
                StartedAt = DateTime.UtcNow,
                TechniqueProfileId = cueProfileId,
            };
            db.SleepSessions.Add(session);
            await db.SaveChangesAsync(ct);
        }

        if (cueProfileId is { } pid)
        {
            await _scheduler.ActivateProfileAsync(pid, session.StartedAt, session.Id);
        }

        _logger.LogInformation("Sleep session {Id} started (profile {Profile})", session.Id, cueProfileId);
        Changed?.Invoke();
        return session;
    }

    public async Task EndAsync(int sessionId, int? quality, string? notes, CancellationToken ct = default)
    {
        await using (var db = await _dbFactory.CreateDbContextAsync(ct))
        {
            var session = await db.SleepSessions.FirstOrDefaultAsync(s => s.Id == sessionId, ct);
            if (session is null || session.EndedAt is not null) return;
            session.EndedAt = DateTime.UtcNow;
            session.Quality = quality;
            session.Notes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            await db.SaveChangesAsync(ct);
        }

        // Ending the night stops any armed cues.
        await _scheduler.DeactivateAsync();

        // Lock-screen prompt so dreams can be captured before they fade.
        await _notifications.ShowDreamCaptureAsync();

        _logger.LogInformation("Sleep session {Id} ended", sessionId);
        Changed?.Invoke();
    }

    public async Task<IReadOnlyList<SleepSession>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.SleepSessions
            .AsNoTracking()
            .Include(s => s.TechniqueProfile)
            .OrderByDescending(s => s.StartedAt)
            .ToListAsync(ct);
    }

    public async Task<SleepSession?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.SleepSessions
            .AsNoTracking()
            .Include(s => s.TechniqueProfile)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<SessionStats> GetStatsAsync(int sessionId, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var dreams = await db.DreamEntries
            .AsNoTracking()
            .Where(e => e.SleepSessionId == sessionId)
            .Select(e => e.Lucidity)
            .ToListAsync(ct);
        var lucid = dreams.Count(l => l >= LucidityLevel.BriefLucidity);
        return new SessionStats(dreams.Count, lucid);
    }

    public async Task<SleepSession?> GetRecentLinkableAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var cutoff = DateTime.UtcNow.AddHours(-12);
        return await db.SleepSessions
            .AsNoTracking()
            .Where(s => s.EndedAt == null || s.EndedAt >= cutoff)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync(ct);
    }
}
