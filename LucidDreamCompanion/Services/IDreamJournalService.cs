using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

public record JournalQuery(
    string? Search = null,
    IReadOnlyCollection<int>? TagIds = null,
    LucidityLevel? MinLucidity = null,
    DateTime? FromDate = null,
    DateTime? ToDate = null);

public interface IDreamJournalService
{
    event Action? Changed;

    Task<IReadOnlyList<DreamEntry>> ListAsync(JournalQuery? query = null, CancellationToken ct = default);
    Task<DreamEntry?> GetAsync(int id, CancellationToken ct = default);
    Task<DreamEntry> CreateAsync(CancellationToken ct = default);
    Task<IReadOnlyList<SleepSession>> ListRecentSessionsAsync(int take = 12, CancellationToken ct = default);
    Task SaveAsync(DreamEntry entry, IEnumerable<int> tagIds, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task RestoreAsync(DreamEntry entry, IEnumerable<int> tagIds, CancellationToken ct = default);
}
