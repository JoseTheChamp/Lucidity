using LucidDreamCompanion.Data.Entities;
using LucidDreamCompanion.Services;
using Microsoft.EntityFrameworkCore;

namespace LucidDreamCompanion.Data.Seed;

public static class TechniqueTemplatesSeeder
{
    public static async Task SeedAsync(LucidDbContext db, CancellationToken ct = default)
    {
        var existingKeys = await db.CueProfiles
            .Where(p => p.IsTechniqueTemplate && p.TechniqueKey != null)
            .Select(p => p.TechniqueKey!)
            .ToListAsync(ct);
        var present = new HashSet<string>(existingKeys, StringComparer.OrdinalIgnoreCase);

        var assets = await db.AudioAssets
            .Where(a => a.Source == AudioAssetSource.Bundled)
            .ToDictionaryAsync(a => a.PathOrResourceKey, a => a.Id, StringComparer.OrdinalIgnoreCase, ct);

        var added = false;
        foreach (var def in TechniqueDefinitions.All)
        {
            if (present.Contains(def.Key)) continue;

            var profile = new CueProfile
            {
                Name = def.Name,
                Description = def.Tagline,
                IsTechniqueTemplate = true,
                TechniqueKey = def.Key,
                Cues = new List<AudioCue>(),
            };

            var order = 0;
            foreach (var spec in def.Cues)
            {
                if (!assets.TryGetValue(spec.AudioKey, out var assetId))
                    continue;   // bundled asset missing — skip rather than fail
                profile.Cues.Add(new AudioCue
                {
                    Label = spec.Label,
                    TriggerMode = spec.Mode,
                    OffsetFromSleepStart = spec.Offset,
                    AudioAssetId = assetId,
                    VolumePercent = spec.VolumePercent,
                    FadeInSeconds = spec.FadeInSeconds,
                    LoopCount = spec.LoopCount,
                    Enabled = true,
                    SortOrder = order++,
                });
            }

            db.CueProfiles.Add(profile);
            added = true;
        }

        if (added) await db.SaveChangesAsync(ct);
    }
}
