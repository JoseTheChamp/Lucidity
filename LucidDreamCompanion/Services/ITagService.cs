using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

public interface ITagService
{
    event Action? Changed;

    Task<IReadOnlyList<Tag>> ListAsync(CancellationToken ct = default);
    Task<Tag?> GetAsync(int id, CancellationToken ct = default);
    Task<Tag> CreateAsync(string name, string colorHex, CancellationToken ct = default);
    Task UpdateAsync(int id, string name, string colorHex, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);
    Task<int> CountUsageAsync(int id, CancellationToken ct = default);
}
