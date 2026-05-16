using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LucidDreamCompanion.Data.Seed;

public static class BundledAudioSeeder
{
    public record BundledAsset(string DisplayName, string ResourceKey, int DurationMs);

    // Keys are the MauiAsset logical names — files under Resources/Raw/Audio/ resolve to "Audio/<file>".
    public static readonly BundledAsset[] Assets =
    {
        new("Soft bell",        "Audio/bell-soft.wav",            1600),
        new("Tibetan bell",     "Audio/bell-tibetan.wav",         4000),
        new("\"Are you dreaming?\" (cue)", "Audio/voice-are-you-dreaming.wav", 1600),
        new("Pink noise",       "Audio/pink-noise.wav",           8000),
        new("Brown noise",      "Audio/brown-noise.wav",          8000),
        new("Binaural · theta", "Audio/binaural-theta.wav",       12000),
    };

    public static async Task SeedAsync(LucidDbContext db, CancellationToken ct = default)
    {
        var existing = await db.AudioAssets
            .Where(a => a.Source == AudioAssetSource.Bundled)
            .Select(a => a.PathOrResourceKey)
            .ToListAsync(ct);
        var set = new HashSet<string>(existing, StringComparer.OrdinalIgnoreCase);
        foreach (var a in Assets)
        {
            if (set.Contains(a.ResourceKey)) continue;
            db.AudioAssets.Add(new AudioAsset
            {
                DisplayName = a.DisplayName,
                PathOrResourceKey = a.ResourceKey,
                Source = AudioAssetSource.Bundled,
                DurationMs = a.DurationMs,
            });
        }
        await db.SaveChangesAsync(ct);
    }
}
