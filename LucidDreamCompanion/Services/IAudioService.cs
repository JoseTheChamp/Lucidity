using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

public record AudioPlaybackOptions
{
    public int VolumePercent { get; init; } = 60;
    public int FadeInSeconds { get; init; } = 0;
    public int LoopCount { get; init; } = 1;
    public string? VibrationPattern { get; init; }   // CSV ms, e.g. "0,200,100,200"
}

public class PlaybackEventArgs : EventArgs
{
    public int Handle { get; init; }
    public int AudioAssetId { get; init; }
    public bool CompletedNormally { get; init; }
}

public interface IAudioService
{
    bool IsPlaying { get; }
    string? LastError { get; }
    event EventHandler<PlaybackEventArgs>? PlaybackFinished;

    Task<int> PlayAsync(AudioAsset asset, AudioPlaybackOptions options, CancellationToken ct = default);
    Task StopAsync(int handle);
    Task StopAllAsync();

    Task<AudioAsset?> ImportFileAsync(CancellationToken ct = default);
    Task<bool> RequestMicrophonePermissionAsync();
    Task<AudioAsset?> RecordAsync(TimeSpan maxDuration, CancellationToken ct = default);
    Task<bool> IsRecordingAsync();
    Task StopRecordingAsync();
    Task DeleteAssetAsync(int assetId);
    Task RenameAssetAsync(int assetId, string newName);
}
