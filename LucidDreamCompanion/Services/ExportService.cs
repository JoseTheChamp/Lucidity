using System.IO.Compression;
using System.Text;
using System.Text.Json;
using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using LucidDreamCompanion.Data.Seed;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LucidDreamCompanion.Services;

public class ExportService : IExportService
{
    private const int SchemaVersion = 1;

    private readonly IDbContextFactory<LucidDbContext> _factory;
    private readonly ILogger<ExportService> _logger;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public ExportService(IDbContextFactory<LucidDbContext> factory, ILogger<ExportService> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    // ─── Export: JSON ────────────────────────────────────────────────

    public async Task<bool> ExportJsonAsync(CancellationToken ct = default)
    {
        try
        {
            var doc = await BuildDocumentAsync(ct);
            var json = JsonSerializer.Serialize(doc, JsonOpts);
            var path = Path.Combine(FileSystem.CacheDirectory, $"lucid-backup-{DateTime.Now:yyyyMMdd-HHmmss}.json");
            await File.WriteAllTextAsync(path, json, ct);
            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Lucid Dream backup (JSON)",
                File = new ShareFile(path),
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "JSON export failed");
            return false;
        }
    }

    // ─── Export: Markdown ────────────────────────────────────────────

    public async Task<bool> ExportMarkdownAsync(CancellationToken ct = default)
    {
        try
        {
            await using var db = await _factory.CreateDbContextAsync(ct);
            var entries = await db.DreamEntries
                .AsNoTracking()
                .Include(e => e.Tags)
                .OrderBy(e => e.DreamDate)
                .ToListAsync(ct);

            var zipPath = Path.Combine(FileSystem.CacheDirectory, $"lucid-dreams-{DateTime.Now:yyyyMMdd-HHmmss}.zip");
            if (File.Exists(zipPath)) File.Delete(zipPath);

            using (var zip = ZipFile.Open(zipPath, ZipArchiveMode.Create))
            {
                foreach (var e in entries)
                {
                    var entryPath = $"dreams/{e.DreamDate:yyyy}/{e.DreamDate:MM}/{e.DreamDate:yyyy-MM-dd}-{e.Id}.md";
                    var zipEntry = zip.CreateEntry(entryPath, CompressionLevel.Optimal);
                    await using var stream = zipEntry.Open();
                    await using var writer = new StreamWriter(stream, Encoding.UTF8);
                    await writer.WriteAsync(RenderMarkdown(e));
                }
            }

            await Share.Default.RequestAsync(new ShareFileRequest
            {
                Title = "Lucid Dream journal (Markdown)",
                File = new ShareFile(zipPath),
            });
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Markdown export failed");
            return false;
        }
    }

    private static string RenderMarkdown(DreamEntry e)
    {
        var sb = new StringBuilder();
        sb.AppendLine("---");
        sb.AppendLine($"title: {Yaml(string.IsNullOrWhiteSpace(e.Title) ? "(untitled)" : e.Title)}");
        sb.AppendLine($"date: {e.DreamDate:yyyy-MM-dd}");
        sb.AppendLine($"created: {e.CreatedAt:o}");
        sb.AppendLine($"lucidity: {e.Lucidity}");
        if (e.SleepQuality.HasValue) sb.AppendLine($"sleepQuality: {e.SleepQuality.Value}");
        if (e.SleepSessionId.HasValue) sb.AppendLine($"sleepSessionId: {e.SleepSessionId.Value}");
        var tags = string.Join(", ", e.Tags.Select(t => t.Name));
        sb.AppendLine($"tags: [{tags}]");
        sb.AppendLine("---");
        sb.AppendLine();
        sb.AppendLine($"# {(string.IsNullOrWhiteSpace(e.Title) ? "(untitled)" : e.Title)}");
        sb.AppendLine();
        sb.AppendLine(e.Body);
        return sb.ToString();
    }

    private static string Yaml(string s)
        => s.Contains(':') || s.Contains('#') ? $"\"{s.Replace("\"", "\\\"")}\"" : s;

    // ─── Import ──────────────────────────────────────────────────────

    public async Task<ImportResult> ImportJsonAsync(ImportMode mode, CancellationToken ct = default)
    {
        try
        {
            var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = new[] { "application/json", "text/plain", "*/*" },
                [DevicePlatform.WinUI] = new[] { ".json" },
            });
            var picked = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pick a Lucid Dream JSON backup",
                FileTypes = fileTypes,
            });
            if (picked is null) return new ImportResult(false, "No file picked.", 0);

            BackupDoc? doc;
            await using (var stream = await picked.OpenReadAsync())
            {
                doc = await JsonSerializer.DeserializeAsync<BackupDoc>(stream, JsonOpts, ct);
            }
            if (doc is null) return new ImportResult(false, "File could not be read as a backup.", 0);

            var count = await RestoreAsync(doc, wipeFirst: mode == ImportMode.Replace, ct);
            return new ImportResult(true,
                mode == ImportMode.Replace ? "Replaced all data from backup." : "Merged backup into your journal.",
                count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Import failed");
            return new ImportResult(false, $"Import failed: {ex.Message}", 0);
        }
    }

    private async Task<int> RestoreAsync(BackupDoc doc, bool wipeFirst, CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        if (wipeFirst)
        {
            await WipeAsync(db, ct);
        }

        // Insert in dependency order, remapping old ids → new ids.
        var tagMap = new Dictionary<int, int>();
        foreach (var t in doc.Tags ?? new())
        {
            // Merge: reuse an existing tag with the same name.
            var existing = await db.Tags.FirstOrDefaultAsync(x => x.Name == t.Name, ct);
            if (existing is not null)
            {
                tagMap[t.Id] = existing.Id;
                continue;
            }
            var tag = new Tag { Name = t.Name, ColorHex = t.ColorHex, IsBuiltIn = t.IsBuiltIn };
            db.Tags.Add(tag);
            await db.SaveChangesAsync(ct);
            tagMap[t.Id] = tag.Id;
        }

        var assetMap = new Dictionary<int, int>();
        foreach (var a in doc.AudioAssets ?? new())
        {
            var existing = a.Source == AudioAssetSource.Bundled
                ? await db.AudioAssets.FirstOrDefaultAsync(x => x.PathOrResourceKey == a.PathOrResourceKey, ct)
                : null;
            if (existing is not null) { assetMap[a.Id] = existing.Id; continue; }
            var asset = new AudioAsset
            {
                DisplayName = a.DisplayName,
                Source = a.Source,
                PathOrResourceKey = a.PathOrResourceKey,
                DurationMs = a.DurationMs,
            };
            db.AudioAssets.Add(asset);
            await db.SaveChangesAsync(ct);
            assetMap[a.Id] = asset.Id;
        }

        var profileMap = new Dictionary<int, int>();
        foreach (var p in doc.CueProfiles ?? new())
        {
            var profile = new CueProfile
            {
                Name = p.Name,
                Description = p.Description,
                IsTechniqueTemplate = p.IsTechniqueTemplate,
                TechniqueKey = p.TechniqueKey,
            };
            db.CueProfiles.Add(profile);
            await db.SaveChangesAsync(ct);
            profileMap[p.Id] = profile.Id;
        }

        foreach (var c in doc.AudioCues ?? new())
        {
            if (c.AudioAssetId is { } aid && !assetMap.ContainsKey(aid)) continue;
            var cue = new AudioCue
            {
                Label = c.Label,
                TriggerMode = c.TriggerMode,
                OffsetFromSleepStart = c.OffsetFromSleepStart,
                ClockTime = c.ClockTime,
                IntervalStart = c.IntervalStart,
                IntervalEnd = c.IntervalEnd,
                IntervalEvery = c.IntervalEvery,
                AudioAssetId = c.AudioAssetId is { } a2 ? assetMap[a2] : 0,
                VolumePercent = c.VolumePercent,
                FadeInSeconds = c.FadeInSeconds,
                LoopCount = c.LoopCount,
                VibrationPattern = c.VibrationPattern,
                Enabled = c.Enabled,
                SortOrder = c.SortOrder,
                CueProfileId = c.CueProfileId is { } pid && profileMap.TryGetValue(pid, out var np) ? np : null,
            };
            db.AudioCues.Add(cue);
        }
        await db.SaveChangesAsync(ct);

        var sessionMap = new Dictionary<int, int>();
        foreach (var s in doc.SleepSessions ?? new())
        {
            var session = new SleepSession
            {
                StartedAt = s.StartedAt,
                EndedAt = s.EndedAt,
                Notes = s.Notes,
                Quality = s.Quality,
                TechniqueProfileId = s.TechniqueProfileId is { } pid && profileMap.TryGetValue(pid, out var np) ? np : null,
            };
            db.SleepSessions.Add(session);
            await db.SaveChangesAsync(ct);
            sessionMap[s.Id] = session.Id;
        }

        var imported = 0;
        foreach (var e in doc.DreamEntries ?? new())
        {
            var entry = new DreamEntry
            {
                CreatedAt = e.CreatedAt,
                DreamDate = e.DreamDate,
                Title = e.Title,
                Body = e.Body,
                Lucidity = e.Lucidity,
                SleepQuality = e.SleepQuality,
                SleepSessionId = e.SleepSessionId is { } sid && sessionMap.TryGetValue(sid, out var ns) ? ns : null,
                Tags = await ResolveTagsAsync(db, e.TagIds, tagMap, ct),
            };
            db.DreamEntries.Add(entry);
            imported++;
        }
        await db.SaveChangesAsync(ct);

        if (doc.JournalReminders is { Count: > 0 } && wipeFirst)
        {
            foreach (var r in doc.JournalReminders)
                db.JournalReminders.Add(new JournalReminder { Time = r.Time, Enabled = r.Enabled });
            await db.SaveChangesAsync(ct);
        }

        return imported;
    }

    private static async Task<List<Tag>> ResolveTagsAsync(
        LucidDbContext db, List<int>? oldIds, Dictionary<int, int> tagMap, CancellationToken ct)
    {
        var result = new List<Tag>();
        if (oldIds is null) return result;
        foreach (var oldId in oldIds)
        {
            if (!tagMap.TryGetValue(oldId, out var newId)) continue;
            var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == newId, ct);
            if (tag is not null) result.Add(tag);
        }
        return result;
    }

    private static async Task WipeAsync(LucidDbContext db, CancellationToken ct)
    {
        await db.Database.ExecuteSqlRawAsync(@"
            DELETE FROM DreamEntryTags;
            DELETE FROM ScheduledCueLogs;
            DELETE FROM DreamEntries;
            DELETE FROM AudioCues;
            DELETE FROM SleepSessions;
            DELETE FROM CueProfiles;
            DELETE FROM AudioAssets;
            DELETE FROM Tags;
            DELETE FROM RealityCheckLogs;
            DELETE FROM JournalReminders;", ct);
    }

    // ─── Data management ─────────────────────────────────────────────

    public async Task ClearLogsAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlRawAsync(
            "DELETE FROM ScheduledCueLogs; DELETE FROM RealityCheckLogs;", ct);
        _logger.LogInformation("Cleared cue + reality-check logs");
    }

    public async Task VacuumAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await db.Database.ExecuteSqlRawAsync("VACUUM;", ct);
        _logger.LogInformation("Database vacuumed");
    }

    public async Task DeleteAllDataAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        await WipeAsync(db, ct);
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE AppSettings SET ActiveCueProfileId = NULL WHERE Id = 1;", ct);
        // Re-seed the defaults so the app remains usable.
        await DefaultTagsSeeder.SeedAsync(db, ct);
        await BundledAudioSeeder.SeedAsync(db, ct);
        await TechniqueTemplatesSeeder.SeedAsync(db, ct);
        await db.Database.ExecuteSqlRawAsync("VACUUM;", ct);
        _logger.LogInformation("All user data deleted and defaults re-seeded");
    }

    // ─── Document build ──────────────────────────────────────────────

    private async Task<BackupDoc> BuildDocumentAsync(CancellationToken ct)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);

        var entries = await db.DreamEntries.AsNoTracking().Include(e => e.Tags).ToListAsync(ct);
        var settings = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(s => s.Id == 1, ct);

        return new BackupDoc
        {
            SchemaVersion = SchemaVersion,
            ExportedAt = DateTime.UtcNow,
            Tags = await db.Tags.AsNoTracking()
                .Select(t => new TagDto { Id = t.Id, Name = t.Name, ColorHex = t.ColorHex, IsBuiltIn = t.IsBuiltIn })
                .ToListAsync(ct),
            AudioAssets = await db.AudioAssets.AsNoTracking()
                .Select(a => new AudioAssetDto { Id = a.Id, DisplayName = a.DisplayName, Source = a.Source, PathOrResourceKey = a.PathOrResourceKey, DurationMs = a.DurationMs })
                .ToListAsync(ct),
            CueProfiles = await db.CueProfiles.AsNoTracking()
                .Select(p => new CueProfileDto { Id = p.Id, Name = p.Name, Description = p.Description, IsTechniqueTemplate = p.IsTechniqueTemplate, TechniqueKey = p.TechniqueKey })
                .ToListAsync(ct),
            AudioCues = await db.AudioCues.AsNoTracking()
                .Select(c => new AudioCueDto
                {
                    Id = c.Id, Label = c.Label, TriggerMode = c.TriggerMode,
                    OffsetFromSleepStart = c.OffsetFromSleepStart, ClockTime = c.ClockTime,
                    IntervalStart = c.IntervalStart, IntervalEnd = c.IntervalEnd, IntervalEvery = c.IntervalEvery,
                    AudioAssetId = c.AudioAssetId, VolumePercent = c.VolumePercent, FadeInSeconds = c.FadeInSeconds,
                    LoopCount = c.LoopCount, VibrationPattern = c.VibrationPattern, Enabled = c.Enabled,
                    SortOrder = c.SortOrder, CueProfileId = c.CueProfileId,
                })
                .ToListAsync(ct),
            SleepSessions = await db.SleepSessions.AsNoTracking()
                .Select(s => new SleepSessionDto { Id = s.Id, StartedAt = s.StartedAt, EndedAt = s.EndedAt, Notes = s.Notes, Quality = s.Quality, TechniqueProfileId = s.TechniqueProfileId })
                .ToListAsync(ct),
            DreamEntries = entries.Select(e => new DreamEntryDto
            {
                Id = e.Id, CreatedAt = e.CreatedAt, DreamDate = e.DreamDate, Title = e.Title, Body = e.Body,
                Lucidity = e.Lucidity, SleepQuality = e.SleepQuality, SleepSessionId = e.SleepSessionId,
                TagIds = e.Tags.Select(t => t.Id).ToList(),
            }).ToList(),
            CueLogs = await db.ScheduledCueLogs.AsNoTracking()
                .Select(l => new ScheduledCueLogDto { Id = l.Id, SleepSessionId = l.SleepSessionId, CueProfileId = l.CueProfileId, AudioCueId = l.AudioCueId, CueLabel = l.CueLabel, ScheduledFor = l.ScheduledFor, FiredAt = l.FiredAt, Status = l.Status })
                .ToListAsync(ct),
            RealityChecks = await db.RealityCheckLogs.AsNoTracking()
                .Select(r => new RealityCheckLogDto { Id = r.Id, FiredAt = r.FiredAt, CompletedAt = r.CompletedAt, WasDreaming = r.WasDreaming })
                .ToListAsync(ct),
            JournalReminders = await db.JournalReminders.AsNoTracking()
                .Select(r => new JournalReminderDto { Id = r.Id, Time = r.Time, Enabled = r.Enabled })
                .ToListAsync(ct),
            Settings = settings is null ? null : new AppSettingsDto
            {
                JournalReminderTime1 = settings.JournalReminderTime1,
                JournalReminderTime2 = settings.JournalReminderTime2,
                RealityCheckEnabled = settings.RealityCheckEnabled,
                RealityCheckStart = settings.RealityCheckStart,
                RealityCheckEnd = settings.RealityCheckEnd,
                RealityCheckMode = settings.RealityCheckMode,
                RealityCheckCountPerDay = settings.RealityCheckCountPerDay,
                RealityCheckIntervalMinutes = settings.RealityCheckIntervalMinutes,
                SpeechLocale = settings.SpeechLocale,
            },
        };
    }

    // ─── Backup DTOs ─────────────────────────────────────────────────

    private sealed class BackupDoc
    {
        public int SchemaVersion { get; set; }
        public DateTime ExportedAt { get; set; }
        public List<TagDto> Tags { get; set; } = new();
        public List<AudioAssetDto> AudioAssets { get; set; } = new();
        public List<CueProfileDto> CueProfiles { get; set; } = new();
        public List<AudioCueDto> AudioCues { get; set; } = new();
        public List<SleepSessionDto> SleepSessions { get; set; } = new();
        public List<DreamEntryDto> DreamEntries { get; set; } = new();
        public List<ScheduledCueLogDto> CueLogs { get; set; } = new();
        public List<RealityCheckLogDto> RealityChecks { get; set; } = new();
        public List<JournalReminderDto> JournalReminders { get; set; } = new();
        public AppSettingsDto? Settings { get; set; }
    }

    private sealed class TagDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string ColorHex { get; set; } = "#888888";
        public bool IsBuiltIn { get; set; }
    }

    private sealed class AudioAssetDto
    {
        public int Id { get; set; }
        public string DisplayName { get; set; } = "";
        public AudioAssetSource Source { get; set; }
        public string PathOrResourceKey { get; set; } = "";
        public int DurationMs { get; set; }
    }

    private sealed class CueProfileDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public string? Description { get; set; }
        public bool IsTechniqueTemplate { get; set; }
        public string? TechniqueKey { get; set; }
    }

    private sealed class AudioCueDto
    {
        public int Id { get; set; }
        public string Label { get; set; } = "";
        public CueTriggerMode TriggerMode { get; set; }
        public TimeSpan? OffsetFromSleepStart { get; set; }
        public TimeOnly? ClockTime { get; set; }
        public TimeOnly? IntervalStart { get; set; }
        public TimeOnly? IntervalEnd { get; set; }
        public TimeSpan? IntervalEvery { get; set; }
        public int? AudioAssetId { get; set; }
        public int VolumePercent { get; set; }
        public int FadeInSeconds { get; set; }
        public int LoopCount { get; set; }
        public string? VibrationPattern { get; set; }
        public bool Enabled { get; set; }
        public int SortOrder { get; set; }
        public int? CueProfileId { get; set; }
    }

    private sealed class SleepSessionDto
    {
        public int Id { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? Notes { get; set; }
        public int? Quality { get; set; }
        public int? TechniqueProfileId { get; set; }
    }

    private sealed class DreamEntryDto
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime DreamDate { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public LucidityLevel Lucidity { get; set; }
        public int? SleepQuality { get; set; }
        public int? SleepSessionId { get; set; }
        public List<int> TagIds { get; set; } = new();
    }

    private sealed class ScheduledCueLogDto
    {
        public int Id { get; set; }
        public int? SleepSessionId { get; set; }
        public int? CueProfileId { get; set; }
        public int AudioCueId { get; set; }
        public string CueLabel { get; set; } = "";
        public DateTime ScheduledFor { get; set; }
        public DateTime? FiredAt { get; set; }
        public string? Status { get; set; }
    }

    private sealed class RealityCheckLogDto
    {
        public int Id { get; set; }
        public DateTime FiredAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool WasDreaming { get; set; }
    }

    private sealed class JournalReminderDto
    {
        public int Id { get; set; }
        public TimeOnly Time { get; set; }
        public bool Enabled { get; set; }
    }

    private sealed class AppSettingsDto
    {
        public TimeOnly JournalReminderTime1 { get; set; }
        public TimeOnly? JournalReminderTime2 { get; set; }
        public bool RealityCheckEnabled { get; set; }
        public TimeOnly RealityCheckStart { get; set; }
        public TimeOnly RealityCheckEnd { get; set; }
        public RealityCheckMode RealityCheckMode { get; set; }
        public int RealityCheckCountPerDay { get; set; }
        public int RealityCheckIntervalMinutes { get; set; }
        public string SpeechLocale { get; set; } = "";
    }
}
