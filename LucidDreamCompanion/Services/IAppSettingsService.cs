using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

public interface IAppSettingsService
{
    event Action? Changed;
    Task<AppSettings> GetAsync(CancellationToken ct = default);
    Task SaveAsync(AppSettings settings, CancellationToken ct = default);
}
