using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

public interface IJournalReminderService
{
    event Action? Changed;
    Task<IReadOnlyList<JournalReminder>> ListAsync(CancellationToken ct = default);
    Task<JournalReminder> AddAsync(TimeOnly time, CancellationToken ct = default);
    Task UpdateAsync(int id, TimeOnly time, bool enabled, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
}
