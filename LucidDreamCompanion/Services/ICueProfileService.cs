using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

public interface ICueProfileService
{
    event Action? Changed;

    Task<IReadOnlyList<CueProfile>> ListAsync(CancellationToken ct = default);
    Task<CueProfile?> GetAsync(int id, CancellationToken ct = default);
    Task<CueProfile> CreateAsync(string name, CancellationToken ct = default);
    Task<CueProfile?> DuplicateAsync(int id, CancellationToken ct = default);
    Task RenameAsync(int id, string name, string? description, CancellationToken ct = default);
    Task DeleteAsync(int id, CancellationToken ct = default);

    Task<AudioCue?> GetCueAsync(int cueId, CancellationToken ct = default);
    Task<AudioCue> AddCueAsync(int profileId, CancellationToken ct = default);
    Task SaveCueAsync(AudioCue cue, CancellationToken ct = default);
    Task DeleteCueAsync(int cueId, CancellationToken ct = default);
    Task MoveCueAsync(int cueId, int direction, CancellationToken ct = default);  // -1 up, +1 down
}
