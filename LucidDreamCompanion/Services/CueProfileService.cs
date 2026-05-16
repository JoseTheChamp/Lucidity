using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LucidDreamCompanion.Services;

public class CueProfileService : ICueProfileService
{
    private readonly IDbContextFactory<LucidDbContext> _factory;
    private readonly ILogger<CueProfileService> _logger;

    public event Action? Changed;

    public CueProfileService(IDbContextFactory<LucidDbContext> factory, ILogger<CueProfileService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<CueProfile>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.CueProfiles
            .AsNoTracking()
            .Include(p => p.Cues)
            .OrderBy(p => p.Name)
            .ToListAsync(ct);
    }

    public async Task<CueProfile?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var profile = await db.CueProfiles
            .AsNoTracking()
            .Include(p => p.Cues)
            .ThenInclude(c => c.AudioAsset)
            .FirstOrDefaultAsync(p => p.Id == id, ct);
        if (profile is not null)
            profile.Cues = profile.Cues.OrderBy(c => c.SortOrder).ThenBy(c => c.Id).ToList();
        return profile;
    }

    public async Task<CueProfile> CreateAsync(string name, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var profile = new CueProfile { Name = string.IsNullOrWhiteSpace(name) ? "New profile" : name.Trim() };
        db.CueProfiles.Add(profile);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Created cue profile {Id} '{Name}'", profile.Id, profile.Name);
        Changed?.Invoke();
        return profile;
    }

    public async Task<CueProfile?> DuplicateAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var src = await db.CueProfiles.Include(p => p.Cues).FirstOrDefaultAsync(p => p.Id == id, ct);
        if (src is null) return null;

        var copy = new CueProfile
        {
            Name = $"{src.Name} (copy)",
            Description = src.Description,
            // Duplicates are always editable, even when copied from a locked technique template.
            IsTechniqueTemplate = false,
            TechniqueKey = null,
            Cues = src.Cues.Select(c => new AudioCue
            {
                Label = c.Label,
                TriggerMode = c.TriggerMode,
                OffsetFromSleepStart = c.OffsetFromSleepStart,
                ClockTime = c.ClockTime,
                IntervalStart = c.IntervalStart,
                IntervalEnd = c.IntervalEnd,
                IntervalEvery = c.IntervalEvery,
                AudioAssetId = c.AudioAssetId,
                VolumePercent = c.VolumePercent,
                FadeInSeconds = c.FadeInSeconds,
                LoopCount = c.LoopCount,
                VibrationPattern = c.VibrationPattern,
                Enabled = c.Enabled,
                SortOrder = c.SortOrder,
            }).ToList(),
        };
        db.CueProfiles.Add(copy);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Duplicated profile {Src} → {New}", id, copy.Id);
        Changed?.Invoke();
        return copy;
    }

    public async Task RenameAsync(int id, string name, string? description, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var p = await db.CueProfiles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return;
        if (!string.IsNullOrWhiteSpace(name)) p.Name = name.Trim();
        p.Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var p = await db.CueProfiles.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (p is null) return;
        db.CueProfiles.Remove(p);   // cascade removes its cues
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Deleted cue profile {Id}", id);
        Changed?.Invoke();
    }

    public async Task<AudioCue?> GetCueAsync(int cueId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.AudioCues
            .AsNoTracking()
            .Include(c => c.AudioAsset)
            .FirstOrDefaultAsync(c => c.Id == cueId, ct);
    }

    public async Task<AudioCue> AddCueAsync(int profileId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var firstAsset = await db.AudioAssets.OrderBy(a => a.Id).FirstOrDefaultAsync(ct);
        var maxOrder = await db.AudioCues.Where(c => c.CueProfileId == profileId)
            .Select(c => (int?)c.SortOrder).MaxAsync(ct) ?? -1;
        var cue = new AudioCue
        {
            CueProfileId = profileId,
            Label = "New cue",
            TriggerMode = CueTriggerMode.ClockBased,
            ClockTime = new TimeOnly(3, 0),
            AudioAssetId = firstAsset?.Id ?? 0,
            VolumePercent = 40,
            FadeInSeconds = 5,
            LoopCount = 1,
            Enabled = true,
            SortOrder = maxOrder + 1,
        };
        db.AudioCues.Add(cue);
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
        return cue;
    }

    public async Task SaveCueAsync(AudioCue cue, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var tracked = await db.AudioCues.FirstOrDefaultAsync(c => c.Id == cue.Id, ct);
        if (tracked is null) return;
        tracked.Label = cue.Label;
        tracked.TriggerMode = cue.TriggerMode;
        tracked.OffsetFromSleepStart = cue.OffsetFromSleepStart;
        tracked.ClockTime = cue.ClockTime;
        tracked.IntervalStart = cue.IntervalStart;
        tracked.IntervalEnd = cue.IntervalEnd;
        tracked.IntervalEvery = cue.IntervalEvery;
        tracked.AudioAssetId = cue.AudioAssetId;
        tracked.VolumePercent = cue.VolumePercent;
        tracked.FadeInSeconds = cue.FadeInSeconds;
        tracked.LoopCount = cue.LoopCount;
        tracked.VibrationPattern = cue.VibrationPattern;
        tracked.Enabled = cue.Enabled;
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }

    public async Task DeleteCueAsync(int cueId, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var cue = await db.AudioCues.FirstOrDefaultAsync(c => c.Id == cueId, ct);
        if (cue is null) return;
        db.AudioCues.Remove(cue);
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }

    public async Task MoveCueAsync(int cueId, int direction, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var cue = await db.AudioCues.FirstOrDefaultAsync(c => c.Id == cueId, ct);
        if (cue is null || cue.CueProfileId is null) return;
        var siblings = await db.AudioCues
            .Where(c => c.CueProfileId == cue.CueProfileId)
            .OrderBy(c => c.SortOrder).ThenBy(c => c.Id)
            .ToListAsync(ct);
        // Normalize order first.
        for (var i = 0; i < siblings.Count; i++) siblings[i].SortOrder = i;
        var idx = siblings.FindIndex(c => c.Id == cueId);
        var swap = idx + direction;
        if (idx >= 0 && swap >= 0 && swap < siblings.Count)
        {
            (siblings[idx].SortOrder, siblings[swap].SortOrder) = (siblings[swap].SortOrder, siblings[idx].SortOrder);
        }
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }
}
