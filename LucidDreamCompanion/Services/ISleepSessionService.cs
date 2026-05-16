using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

public record SessionStats(int DreamCount, int LucidCount);

public interface ISleepSessionService
{
    event Action? Changed;

    Task<SleepSession?> GetActiveAsync(CancellationToken ct = default);
    Task<SleepSession> StartAsync(int? cueProfileId, CancellationToken ct = default);
    Task EndAsync(int sessionId, int? quality, string? notes, CancellationToken ct = default);
    Task DeleteAsync(int sessionId, CancellationToken ct = default);

    Task<IReadOnlyList<SleepSession>> ListAsync(CancellationToken ct = default);
    Task<SleepSession?> GetAsync(int id, CancellationToken ct = default);
    Task<SessionStats> GetStatsAsync(int sessionId, CancellationToken ct = default);

    /// <summary>The session a freshly-written dream should attach to: active, or ended within 12 h.</summary>
    Task<SleepSession?> GetRecentLinkableAsync(CancellationToken ct = default);
}
