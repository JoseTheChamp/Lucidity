#if ANDROID
using Android.Content;
using Android.Media;
using Android.OS;
using LucidDreamCompanion.Data.Entities;
using Microsoft.Extensions.Logging;
using AndroidUri = Android.Net.Uri;
using AContext = Android.Content.Context;

namespace LucidDreamCompanion.Services;

public partial class AudioService
{
    private readonly Dictionary<int, ActiveHandle> _active = new();
    private int _nextHandle = 1;
    private readonly object _gate = new();

    private MediaRecorder? _recorder;
    private string? _recorderPath;
    private DateTime _recorderStartedAtUtc;

    public bool IsPlaying
    {
        get { lock (_gate) return _active.Count > 0; }
    }

    private sealed class ActiveHandle
    {
        public required MediaPlayer Player { get; init; }
        public required int AssetId { get; init; }
        public required float TargetVolume { get; init; }
        public CancellationTokenSource? FadeCts { get; set; }
        public AudioFocusRequestClass? FocusRequest { get; set; }
    }

    public async Task<int> PlayAsync(AudioAsset asset, AudioPlaybackOptions options, CancellationToken ct = default)
    {
        var ctx = Android.App.Application.Context;
        var player = new MediaPlayer();
        var attrs = new AudioAttributes.Builder()
            ?.SetUsage(AudioUsageKind.Alarm)
            ?.SetContentType(AudioContentType.Sonification)
            ?.Build();
        if (attrs is not null) player.SetAudioAttributes(attrs);

        try
        {
            await ConfigureSourceAsync(player, ctx, asset);
            await PrepareAsync(player);
            LastError = null;
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to prepare MediaPlayer for asset {Id}", asset.Id);
            LastError = $"{ex.GetType().Name}: {ex.Message}";
            player.Release();
            return 0;
        }

        var handle = 0;
        var targetVolume = Math.Clamp(options.VolumePercent, 0, 100) / 100f;
        var loop = Math.Max(1, options.LoopCount);
        var startVolume = options.FadeInSeconds > 0 ? 0f : targetVolume;
        player.SetVolume(startVolume, startVolume);

        var active = new ActiveHandle { Player = player, AssetId = asset.Id, TargetVolume = targetVolume };

        // Audio focus so the cue plays over DnD when usage = Alarm
        var am = (AudioManager?)ctx.GetSystemService(AContext.AudioService);
        if (am is not null && attrs is not null)
        {
            var req = new AudioFocusRequestClass.Builder(AudioFocus.GainTransientMayDuck)
                .SetAudioAttributes(attrs)
                .Build();
            if (req is not null)
            {
                active.FocusRequest = req;
                am.RequestAudioFocus(req);
            }
        }

        lock (_gate)
        {
            handle = _nextHandle++;
            _active[handle] = active;
        }

        var capturedHandle = handle;
        var loopForever = options.LoopForever;
        player.Completion += (_, _) => OnCompletion(capturedHandle, loop, loopForever);

        TriggerVibration(ctx, options.VibrationPattern, options.RepeatVibration);

        player.Start();

        if (options.FadeInSeconds > 0)
        {
            active.FadeCts = new CancellationTokenSource();
            _ = FadeInAsync(active, options.FadeInSeconds, active.FadeCts.Token);
        }
        // first iteration is index 1; remaining loops handled in OnCompletion
        active.Player.Looping = false;
        // store remaining-loop count on the player tag via a parallel dict
        _loopRemaining[handle] = loop - 1;
        Logger.LogInformation("Playing asset {Id} (handle {H}, vol={V}, fade={F}, loops={L})",
            asset.Id, handle, targetVolume, options.FadeInSeconds, loop);
        return handle;
    }

    private readonly Dictionary<int, int> _loopRemaining = new();

    /// <summary>
    /// Copies a bundled MauiAsset out of the APK into the cache dir (once) and returns the file path.
    /// Tries the stored key and a few likely variants, since the asset's logical name depends on the
    /// MauiAsset glob (files under Resources/Raw/Audio land at "Audio/&lt;file&gt;").
    /// </summary>
    private async Task<string> EnsureBundledCachedAsync(string key)
    {
        var fileName = key.Replace('\\', '/').Split('/').Last();
        var cacheDir = Path.Combine(FileSystem.CacheDirectory, "bundled-audio");
        Directory.CreateDirectory(cacheDir);
        var dest = Path.Combine(cacheDir, fileName);
        if (File.Exists(dest) && new FileInfo(dest).Length > 0)
            return dest;

        var candidates = new[]
        {
            key,
            key.Replace('\\', '/'),
            $"Audio/{fileName}",
            fileName,
        };

        foreach (var candidate in candidates.Distinct())
        {
            try
            {
                await using var src = await FileSystem.OpenAppPackageFileAsync(candidate);
                await using var dst = File.Create(dest);
                await src.CopyToAsync(dst);
                Logger.LogInformation("Cached bundled asset '{Candidate}' → {Dest}", candidate, dest);
                return dest;
            }
            catch (Exception ex)
            {
                Logger.LogDebug(ex, "Bundled asset candidate '{Candidate}' not found", candidate);
            }
        }
        throw new FileNotFoundException(
            $"Bundled audio '{key}' not found in app package (tried: {string.Join(", ", candidates.Distinct())})");
    }

    private async Task ConfigureSourceAsync(MediaPlayer player, AContext ctx, AudioAsset asset)
    {
        switch (asset.Source)
        {
            case AudioAssetSource.Bundled:
                {
                    // MauiAssets are compressed inside the APK, so AssetManager.OpenFd() fails on them.
                    // Copy the bundled asset out to the cache directory once, then play from that file.
                    var cachePath = await EnsureBundledCachedAsync(asset.PathOrResourceKey);
                    player.SetDataSource(cachePath);
                    break;
                }
            case AudioAssetSource.ImportedFile:
            case AudioAssetSource.UserRecording:
                {
                    var path = asset.PathOrResourceKey;
                    if (!File.Exists(path))
                        throw new FileNotFoundException("Audio file missing", path);
                    player.SetDataSource(path);
                    break;
                }
            default:
                throw new InvalidOperationException("Unknown audio source");
        }
        await Task.CompletedTask;
    }

    private static Task PrepareAsync(MediaPlayer player)
    {
        var tcs = new TaskCompletionSource<bool>();
        EventHandler? onPrepared = null;
        EventHandler<MediaPlayer.ErrorEventArgs>? onError = null;
        onPrepared = (_, _) =>
        {
            player.Prepared -= onPrepared;
            player.Error -= onError;
            tcs.TrySetResult(true);
        };
        onError = (_, e) =>
        {
            player.Prepared -= onPrepared;
            player.Error -= onError;
            tcs.TrySetException(new InvalidOperationException($"MediaPlayer error {e.What}/{e.Extra}"));
        };
        player.Prepared += onPrepared;
        player.Error += onError;
        player.PrepareAsync();
        return tcs.Task;
    }

    private async Task FadeInAsync(ActiveHandle active, int seconds, CancellationToken ct)
    {
        const int stepMs = 80;
        var steps = Math.Max(1, (seconds * 1000) / stepMs);
        for (var i = 1; i <= steps; i++)
        {
            if (ct.IsCancellationRequested) return;
            var v = active.TargetVolume * (i / (float)steps);
            try { active.Player.SetVolume(v, v); } catch { return; }
            try { await Task.Delay(stepMs, ct); } catch { return; }
        }
    }

    private void OnCompletion(int handle, int loopsTotal, bool loopForever = false)
    {
        ActiveHandle? active = null;
        var remaining = 0;
        lock (_gate)
        {
            if (!_active.TryGetValue(handle, out active)) return;
            if (!_loopRemaining.TryGetValue(handle, out remaining)) remaining = 0;
        }
        if (loopForever || remaining > 0)
        {
            lock (_gate) _loopRemaining[handle] = remaining - 1;
            try
            {
                active.Player.SeekTo(0);
                active.Player.Start();
                return;
            }
            catch (Exception ex)
            {
                Logger.LogWarning(ex, "Failed to loop handle {H}", handle);
            }
        }
        ReleaseHandle(handle, completedNormally: true);
    }

    public Task StopAsync(int handle)
    {
        ReleaseHandle(handle, completedNormally: false);
        return Task.CompletedTask;
    }

    public Task StopAllAsync()
    {
        int[] handles;
        lock (_gate) handles = _active.Keys.ToArray();
        foreach (var h in handles) ReleaseHandle(h, completedNormally: false);
        return Task.CompletedTask;
    }

    private void ReleaseHandle(int handle, bool completedNormally)
    {
        ActiveHandle? active;
        lock (_gate)
        {
            if (!_active.TryGetValue(handle, out active)) return;
            _active.Remove(handle);
            _loopRemaining.Remove(handle);
        }
        try { active.FadeCts?.Cancel(); } catch { }
        try { if (active.Player.IsPlaying) active.Player.Stop(); } catch { }
        try { active.Player.Release(); } catch { }
        CancelVibration(Android.App.Application.Context);
        if (active.FocusRequest is not null)
        {
            try
            {
                var am = (AudioManager?)Android.App.Application.Context.GetSystemService(AContext.AudioService);
                am?.AbandonAudioFocusRequest(active.FocusRequest);
            }
            catch { }
        }
        RaisePlaybackFinished(handle, active.AssetId, completedNormally);
    }

    private void TriggerVibration(AContext ctx, string? csv, bool repeat = false)
    {
        if (string.IsNullOrWhiteSpace(csv)) return;
        try
        {
            var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var pattern = parts.Select(p => long.TryParse(p, out var v) ? v : 0L).ToArray();
            if (pattern.Length == 0) return;

            // repeat == true loops the waveform from index 0 (alarm); -1 plays it once.
            var repeatIndex = repeat ? 0 : -1;

            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var vm = (VibratorManager?)ctx.GetSystemService(AContext.VibratorManagerService);
                var vib = vm?.DefaultVibrator;
                vib?.Vibrate(VibrationEffect.CreateWaveform(pattern, repeatIndex));
            }
            else
            {
#pragma warning disable CA1416
                var vib = (Vibrator?)ctx.GetSystemService(AContext.VibratorService);
                if (vib is null) return;
                if (OperatingSystem.IsAndroidVersionAtLeast(26))
                    vib.Vibrate(VibrationEffect.CreateWaveform(pattern, repeatIndex));
                else
                    vib.Vibrate(pattern, repeatIndex);
#pragma warning restore CA1416
            }
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Vibration failed for pattern {Csv}", csv);
        }
    }

    private void CancelVibration(AContext ctx)
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                var vm = (VibratorManager?)ctx.GetSystemService(AContext.VibratorManagerService);
                vm?.DefaultVibrator?.Cancel();
            }
            else
            {
#pragma warning disable CA1416
                ((Vibrator?)ctx.GetSystemService(AContext.VibratorService))?.Cancel();
#pragma warning restore CA1416
            }
        }
        catch { /* best-effort */ }
    }

    public async Task<bool> RequestMicrophonePermissionAsync()
    {
        var status = await Permissions.CheckStatusAsync<Permissions.Microphone>();
        if (status == PermissionStatus.Granted) return true;
        status = await Permissions.RequestAsync<Permissions.Microphone>();
        return status == PermissionStatus.Granted;
    }

    public Task<bool> IsRecordingAsync() => Task.FromResult(_recorder is not null);

    public async Task<AudioAsset?> RecordAsync(TimeSpan maxDuration, CancellationToken ct = default)
    {
        if (_recorder is not null) return null;
        if (!await RequestMicrophonePermissionAsync()) return null;

        var dir = Path.Combine(FileSystem.AppDataDirectory, "audio", "recordings");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, $"{Guid.NewGuid():N}.m4a");

        try
        {
            var ctx = Android.App.Application.Context;
            MediaRecorder rec;
            if (OperatingSystem.IsAndroidVersionAtLeast(31))
            {
                rec = new MediaRecorder(ctx);
            }
            else
            {
#pragma warning disable CA1416, CS0618
                rec = new MediaRecorder();
#pragma warning restore CA1416, CS0618
            }
            rec.SetAudioSource(AudioSource.Mic);
            rec.SetOutputFormat(OutputFormat.Mpeg4);
            rec.SetAudioEncoder(AudioEncoder.Aac);
            rec.SetAudioEncodingBitRate(96_000);
            rec.SetAudioSamplingRate(44_100);
            rec.SetMaxDuration((int)maxDuration.TotalMilliseconds);
            rec.SetOutputFile(path);
            rec.Prepare();
            rec.Start();
            _recorder = rec;
            _recorderPath = path;
            _recorderStartedAtUtc = DateTime.UtcNow;
            Logger.LogInformation("Recording started → {Path} (max {Ms} ms)", path, maxDuration.TotalMilliseconds);
            return null; // caller will await StopRecording or auto-stop
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Recording failed to start");
            _recorder = null;
            return null;
        }
    }

    public async Task StopRecordingAsync()
    {
        var rec = _recorder;
        var path = _recorderPath;
        _recorder = null;
        _recorderPath = null;
        if (rec is null || path is null) return;

        try { rec.Stop(); } catch (Exception ex) { Logger.LogWarning(ex, "MediaRecorder.Stop threw"); }
        try { rec.Release(); } catch { }

        try
        {
            var durMs = (int)(DateTime.UtcNow - _recorderStartedAtUtc).TotalMilliseconds;
            if (!File.Exists(path))
            {
                Logger.LogWarning("Recording file missing at {Path}", path);
                return;
            }
            var asset = await PersistRecordingAsync(path, durMs);
            Logger.LogInformation("Recording saved: {Path} ({Ms} ms, asset {Id})", path, durMs, asset.Id);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Failed to persist recording");
        }
    }

    protected Task<int> ProbeDurationMsAsync(string path)
    {
        try
        {
            using var mmr = new MediaMetadataRetriever();
            mmr.SetDataSource(path);
            var s = mmr.ExtractMetadata(MetadataKey.Duration);
            if (int.TryParse(s, out var ms)) return Task.FromResult(ms);
        }
        catch (Exception ex)
        {
            Logger.LogWarning(ex, "Could not probe duration for {Path}", path);
        }
        return Task.FromResult(0);
    }
}
#endif
