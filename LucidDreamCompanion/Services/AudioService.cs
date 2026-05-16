using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LucidDreamCompanion.Services;

/// <summary>
/// Cross-platform audio service. Playback and recording are implemented in platform-specific
/// partial files (e.g. AudioService.Android.cs). The desktop / Windows path falls back to no-ops
/// so the journal UI keeps working when running the Windows target for development.
/// </summary>
public partial class AudioService : IAudioService
{
    private readonly IDbContextFactory<LucidDbContext> _dbFactory;
    protected readonly ILogger<AudioService> Logger;

    public event EventHandler<PlaybackEventArgs>? PlaybackFinished;

    public string? LastError { get; protected set; }

    public AudioService(IDbContextFactory<LucidDbContext> dbFactory, ILogger<AudioService> logger)
    {
        _dbFactory = dbFactory;
        Logger = logger;
    }

    protected void RaisePlaybackFinished(int handle, int assetId, bool completedNormally)
        => PlaybackFinished?.Invoke(this, new PlaybackEventArgs
        {
            Handle = handle,
            AudioAssetId = assetId,
            CompletedNormally = completedNormally,
        });

    public async Task<AudioAsset?> ImportFileAsync(CancellationToken ct = default)
    {
        try
        {
            var fileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
            {
                [DevicePlatform.Android] = new[] { "audio/*" },
                [DevicePlatform.WinUI] = new[] { ".mp3", ".wav", ".m4a", ".ogg", ".flac" },
            });
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle = "Pick an audio file",
                FileTypes = fileTypes,
            });
            if (result is null) return null;

            var destDir = Path.Combine(FileSystem.AppDataDirectory, "audio", "imports");
            Directory.CreateDirectory(destDir);
            var ext = Path.GetExtension(result.FileName);
            if (string.IsNullOrWhiteSpace(ext)) ext = ".bin";
            var destPath = Path.Combine(destDir, $"{Guid.NewGuid():N}{ext}");
            await using (var src = await result.OpenReadAsync())
            await using (var dst = File.Create(destPath))
            {
                await src.CopyToAsync(dst, ct);
            }

            var displayName = string.IsNullOrWhiteSpace(result.FileName)
                ? Path.GetFileName(destPath)
                : Path.GetFileNameWithoutExtension(result.FileName);

            await using var db = await _dbFactory.CreateDbContextAsync(ct);
            var asset = new AudioAsset
            {
                DisplayName = displayName,
                Source = AudioAssetSource.ImportedFile,
                PathOrResourceKey = destPath,
                DurationMs = await ProbeDurationMsAsync(destPath),
            };
            db.AudioAssets.Add(asset);
            await db.SaveChangesAsync(ct);
            Logger.LogInformation("Imported audio asset {Name} → {Path}", displayName, destPath);
            return asset;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Audio import failed");
            return null;
        }
    }

    public async Task DeleteAssetAsync(int assetId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var asset = await db.AudioAssets.FirstOrDefaultAsync(a => a.Id == assetId);
        if (asset is null) return;
        if (asset.Source == AudioAssetSource.Bundled)
        {
            Logger.LogWarning("Refusing to delete bundled asset {Id}", assetId);
            return;
        }
        try
        {
            if (!string.IsNullOrEmpty(asset.PathOrResourceKey) && File.Exists(asset.PathOrResourceKey))
                File.Delete(asset.PathOrResourceKey);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not delete audio file at {Path}", asset.PathOrResourceKey);
        }
        db.AudioAssets.Remove(asset);
        await db.SaveChangesAsync();
    }

    public async Task RenameAssetAsync(int assetId, string newName)
    {
        var trimmed = newName.Trim();
        if (string.IsNullOrEmpty(trimmed)) return;
        await using var db = await _dbFactory.CreateDbContextAsync();
        var asset = await db.AudioAssets.FirstOrDefaultAsync(a => a.Id == assetId);
        if (asset is null) return;
        if (asset.Source == AudioAssetSource.Bundled)
        {
            Logger.LogWarning("Refusing to rename bundled asset {Id}", assetId);
            return;
        }
        asset.DisplayName = trimmed;
        await db.SaveChangesAsync();
        Logger.LogInformation("Renamed audio asset {Id} → {Name}", assetId, trimmed);
    }

    protected async Task<AudioAsset> PersistRecordingAsync(string filePath, int durationMs)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var asset = new AudioAsset
        {
            DisplayName = $"Voice memo · {DateTime.Now:MMM d HH:mm}",
            Source = AudioAssetSource.UserRecording,
            PathOrResourceKey = filePath,
            DurationMs = durationMs,
        };
        db.AudioAssets.Add(asset);
        await db.SaveChangesAsync();
        return asset;
    }

#if !ANDROID
    // Desktop / non-Android stubs so the Windows build keeps working.
    public bool IsPlaying => false;
    public Task<int> PlayAsync(AudioAsset asset, AudioPlaybackOptions options, CancellationToken ct = default) => Task.FromResult(0);
    public Task StopAsync(int handle) => Task.CompletedTask;
    public Task StopAllAsync() => Task.CompletedTask;
    public Task<bool> RequestMicrophonePermissionAsync() => Task.FromResult(false);
    public Task<AudioAsset?> RecordAsync(TimeSpan maxDuration, CancellationToken ct = default) => Task.FromResult<AudioAsset?>(null);
    public Task<bool> IsRecordingAsync() => Task.FromResult(false);
    public Task StopRecordingAsync() => Task.CompletedTask;

    protected Task<int> ProbeDurationMsAsync(string path) => Task.FromResult(0);
#endif
}
