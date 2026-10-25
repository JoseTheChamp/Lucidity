using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
#if ANDROID
using Android.App;
using Android.Content;
using Android.OS;
using AContext = Android.Content.Context;
#endif

namespace LucidDreamCompanion.Services;

public class CueSchedulerService : ICueSchedulerService
{
    public const int StopAlarmRequestCode = 990001;
    public const string PrefSleepStart = "cue.sleepStartUtc";

    // Dedicated request-code range for private-mode reality-check audio alarms.
    private const int RcAudioBaseId = 4000;
    private const int RcAudioMaxIds = 200;

    private const int MaxCuesPerActivation = 60;

    private readonly IDbContextFactory<LucidDbContext> _dbFactory;
    private readonly IAppSettingsService _settings;
    private readonly IAudioService _audio;
    private readonly ILogger<CueSchedulerService> _logger;

    // Handle of the alarm currently ringing-until-dismissed, if any.
    private int _activeAlarmHandle;

    public bool IsAlarmRinging => _activeAlarmHandle > 0;
    public event Action? AlarmStateChanged;

    public event Action? ActiveProfileChanged;

    public CueSchedulerService(
        IDbContextFactory<LucidDbContext> dbFactory,
        IAppSettingsService settings,
        IAudioService audio,
        ILogger<CueSchedulerService> logger)
    {
        _dbFactory = dbFactory;
        _settings = settings;
        _audio = audio;
        _logger = logger;
    }

    public async Task<int?> GetActiveProfileIdAsync()
    {
        var s = await _settings.GetAsync();
        return s.ActiveCueProfileId;
    }

    public async Task ActivateProfileAsync(int cueProfileId, DateTime sleepStartUtc, int? sleepSessionId = null)
    {
        await DeactivateAsync();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var profile = await db.CueProfiles
            .Include(p => p.Cues)
            .FirstOrDefaultAsync(p => p.Id == cueProfileId);
        if (profile is null)
        {
            _logger.LogWarning("ActivateProfileAsync: profile {Id} not found", cueProfileId);
            return;
        }

        var referenceLocal = sleepStartUtc.ToLocalTime();
        var now = DateTime.Now;
        var logs = new List<ScheduledCueLog>();

        foreach (var cue in profile.Cues.Where(c => c.Enabled).OrderBy(c => c.SortOrder).ThenBy(c => c.Id))
        {
            foreach (var fireLocal in ComputeFireTimes(cue, referenceLocal))
            {
                if (fireLocal <= now.AddSeconds(2)) continue;
                logs.Add(new ScheduledCueLog
                {
                    CueProfileId = profile.Id,
                    SleepSessionId = sleepSessionId,
                    AudioCueId = cue.Id,
                    CueLabel = string.IsNullOrWhiteSpace(cue.Label) ? "Cue" : cue.Label,
                    ScheduledFor = fireLocal.ToUniversalTime(),
                    Status = CueLogStatus.Scheduled,
                    IsAlarm = cue.IsAlarm,
                });
                if (logs.Count >= MaxCuesPerActivation) break;
            }
            if (logs.Count >= MaxCuesPerActivation) break;
        }

        if (logs.Count > 0)
        {
            db.ScheduledCueLogs.AddRange(logs);
            await db.SaveChangesAsync();
        }

        foreach (var log in logs)
        {
            PlatformArmAlarm(log.Id, log.ScheduledFor, isStop: false, isAlarm: log.IsAlarm);
        }

        if (logs.Count > 0)
        {
            var stopAt = logs.Max(l => l.ScheduledFor).AddMinutes(30);
            PlatformArmAlarm(StopAlarmRequestCode, stopAt, isStop: true);
        }

        var settings = await _settings.GetAsync();
        settings.ActiveCueProfileId = profile.Id;
        await _settings.SaveAsync(settings);
        Preferences.Set(PrefSleepStart, sleepStartUtc.ToString("o"));

        PlatformStartMonitor();
        _logger.LogInformation("Activated profile {Id} '{Name}' — {N} cues armed", profile.Id, profile.Name, logs.Count);
        ActiveProfileChanged?.Invoke();
    }

    public async Task DeactivateAsync()
    {
        // Silence a ringing WBTB alarm so it doesn't loop on after the night ends or is corrected.
        if (_activeAlarmHandle > 0) await StopAlarmAsync();

        await using var db = await _dbFactory.CreateDbContextAsync();
        var pending = await db.ScheduledCueLogs
            .Where(l => l.Status == CueLogStatus.Scheduled)
            .ToListAsync();
        foreach (var log in pending)
        {
            PlatformCancelAlarm(log.Id, isStop: false, isAlarm: log.IsAlarm);
            log.Status = CueLogStatus.Cancelled;
        }
        PlatformCancelAlarm(StopAlarmRequestCode, isStop: true);
        if (pending.Count > 0) await db.SaveChangesAsync();

        var settings = await _settings.GetAsync();
        var had = settings.ActiveCueProfileId;
        settings.ActiveCueProfileId = null;
        await _settings.SaveAsync(settings);

        PlatformStopMonitor();
        if (had is not null)
            _logger.LogInformation("Deactivated profile {Id}; {N} pending cues cancelled", had, pending.Count);
        ActiveProfileChanged?.Invoke();
    }

    public async Task RearmAsync()
    {
        var settings = await _settings.GetAsync();
        if (settings.ActiveCueProfileId is null) return;

        await using var db = await _dbFactory.CreateDbContextAsync();
        var pending = await db.ScheduledCueLogs
            .Where(l => l.Status == CueLogStatus.Scheduled && l.ScheduledFor > DateTime.UtcNow)
            .ToListAsync();
        if (pending.Count == 0)
        {
            await DeactivateAsync();
            return;
        }
        foreach (var log in pending)
        {
            PlatformArmAlarm(log.Id, log.ScheduledFor, isStop: false, isAlarm: log.IsAlarm);
        }
        PlatformArmAlarm(StopAlarmRequestCode, pending.Max(l => l.ScheduledFor).AddMinutes(30), isStop: true);
        PlatformStartMonitor();
        _logger.LogInformation("Re-armed {N} pending cues after boot", pending.Count);
    }

    public async Task<IReadOnlyList<ScheduledCueLog>> GetUpcomingAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.ScheduledCueLogs
            .AsNoTracking()
            .Where(l => l.Status == CueLogStatus.Scheduled && l.ScheduledFor > DateTime.UtcNow)
            .OrderBy(l => l.ScheduledFor)
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ScheduledCueLog>> GetRecentAsync(int hours = 24, CancellationToken ct = default)
    {
        var since = DateTime.UtcNow.AddHours(-hours);
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.ScheduledCueLogs
            .AsNoTracking()
            .Where(l => l.ScheduledFor >= since)
            .OrderByDescending(l => l.ScheduledFor)
            .ToListAsync(ct);
    }

    public async Task PlayCueByLogAsync(int logId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var log = await db.ScheduledCueLogs.FirstOrDefaultAsync(l => l.Id == logId);
        if (log is null)
        {
            _logger.LogWarning("PlayCueByLogAsync: log {Id} not found", logId);
            return;
        }
        var cue = await db.AudioCues
            .Include(c => c.AudioAsset)
            .FirstOrDefaultAsync(c => c.Id == log.AudioCueId);
        if (cue?.AudioAsset is null)
        {
            log.Status = CueLogStatus.Failed;
            await db.SaveChangesAsync();
            _logger.LogWarning("PlayCueByLogAsync: cue/asset missing for log {Id}", logId);
            return;
        }

        try
        {
            var handle = await _audio.PlayAsync(cue.AudioAsset, new AudioPlaybackOptions
            {
                VolumePercent = cue.VolumePercent,
                FadeInSeconds = cue.FadeInSeconds,
                LoopCount = cue.LoopCount,
                VibrationPattern = cue.VibrationPattern,
            });
            if (handle > 0)
            {
                await WaitForPlaybackAsync(handle, timeoutMs: 6 * 60 * 1000);
                log.Status = CueLogStatus.Fired;
                log.FiredAt = DateTime.UtcNow;
                _logger.LogInformation("Cue '{Label}' fired (log {Id})", log.CueLabel, logId);
            }
            else
            {
                log.Status = CueLogStatus.Failed;
                _logger.LogWarning("Cue playback returned no handle (log {Id}): {Err}", logId, _audio.LastError);
            }
        }
        catch (Exception ex)
        {
            log.Status = CueLogStatus.Failed;
            _logger.LogError(ex, "Cue playback threw (log {Id})", logId);
        }
        await db.SaveChangesAsync();
    }

    public async Task PlayAlarmByLogAsync(int logId)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var log = await db.ScheduledCueLogs.FirstOrDefaultAsync(l => l.Id == logId);
        if (log is null)
        {
            _logger.LogWarning("PlayAlarmByLogAsync: log {Id} not found", logId);
            return;
        }
        var cue = await db.AudioCues
            .Include(c => c.AudioAsset)
            .FirstOrDefaultAsync(c => c.Id == log.AudioCueId);
        if (cue?.AudioAsset is null)
        {
            log.Status = CueLogStatus.Failed;
            await db.SaveChangesAsync();
            _logger.LogWarning("PlayAlarmByLogAsync: cue/asset missing for log {Id}", logId);
            return;
        }

        // Stop any audio already playing (a previous cue) so the alarm isn't muddled.
        await _audio.StopAllAsync();

        var handle = await _audio.PlayAsync(cue.AudioAsset, new AudioPlaybackOptions
        {
            VolumePercent = cue.VolumePercent,
            FadeInSeconds = cue.FadeInSeconds,
            LoopForever = true,
            RepeatVibration = true,
            VibrationPattern = string.IsNullOrWhiteSpace(cue.VibrationPattern) ? "0,800,400,800" : cue.VibrationPattern,
        });

        if (handle > 0)
        {
            _activeAlarmHandle = handle;
            // The alarm has fired; dismissal is a separate user action, so mark it Fired now.
            log.Status = CueLogStatus.Fired;
            log.FiredAt = DateTime.UtcNow;
            _logger.LogInformation("Alarm '{Label}' ringing (log {Id}, handle {H})", log.CueLabel, logId, handle);
            AlarmStateChanged?.Invoke();
        }
        else
        {
            log.Status = CueLogStatus.Failed;
            _logger.LogWarning("Alarm playback returned no handle (log {Id}): {Err}", logId, _audio.LastError);
        }
        await db.SaveChangesAsync();
    }

    public async Task StopAlarmAsync()
    {
        var handle = _activeAlarmHandle;
        _activeAlarmHandle = 0;
        if (handle > 0)
            await _audio.StopAsync(handle);
        else
            await _audio.StopAllAsync();
        // Tear down the alarm foreground service so its full-screen notification disappears too.
        // If more cues are still armed in AlarmManager they'll re-start the service when they fire.
        PlatformStopMonitor();
        _logger.LogInformation("Alarm dismissed (handle {H})", handle);
        AlarmStateChanged?.Invoke();
    }

    public async Task ScheduleRealityCheckAudioAsync(IReadOnlyList<DateTime> localTimes, int audioAssetId, int volumePercent)
    {
        await CancelRealityCheckAudioAsync();
        if (audioAssetId <= 0) return;

        var now = DateTime.Now;
        var i = 0;
        foreach (var local in localTimes)
        {
            if (local <= now.AddSeconds(2)) continue;
            if (i >= RcAudioMaxIds) break;
            PlatformArmRcAudioAlarm(RcAudioBaseId + i, local.ToUniversalTime(), audioAssetId, volumePercent);
            i++;
        }
        _logger.LogInformation("Armed {N} private reality-check audio pings", i);
    }

    public Task CancelRealityCheckAudioAsync()
    {
        for (var i = 0; i < RcAudioMaxIds; i++)
        {
            PlatformCancelRcAudioAlarm(RcAudioBaseId + i);
        }
        return Task.CompletedTask;
    }

    public async Task PlayRealityCheckAudioAsync(int audioAssetId, int volumePercent)
    {
        await using var db = await _dbFactory.CreateDbContextAsync();
        var asset = await db.AudioAssets.FirstOrDefaultAsync(a => a.Id == audioAssetId);
        if (asset is null)
        {
            _logger.LogWarning("PlayRealityCheckAudioAsync: asset {Id} missing", audioAssetId);
            return;
        }
        try
        {
            var handle = await _audio.PlayAsync(asset, new AudioPlaybackOptions
            {
                VolumePercent = Math.Clamp(volumePercent, 0, 100),
                FadeInSeconds = 0,
                LoopCount = 1,
            });
            if (handle > 0)
            {
                await WaitForPlaybackAsync(handle, timeoutMs: 30_000);
                _logger.LogInformation("Private reality-check sound played ({Asset})", asset.DisplayName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Private reality-check playback failed");
        }
    }

    private Task WaitForPlaybackAsync(int handle, int timeoutMs)
    {
        var tcs = new TaskCompletionSource();
        EventHandler<PlaybackEventArgs>? handler = null;
        handler = (_, e) =>
        {
            if (e.Handle != handle) return;
            _audio.PlaybackFinished -= handler;
            tcs.TrySetResult();
        };
        _audio.PlaybackFinished += handler;
        _ = Task.Delay(timeoutMs).ContinueWith(_ =>
        {
            _audio.PlaybackFinished -= handler;
            tcs.TrySetResult();
        });
        return tcs.Task;
    }

    // ─── Fire-time computation (Phase 4: ClockBased + Interval; SleepRelative arrives in Phase 5) ───

    private IEnumerable<DateTime> ComputeFireTimes(AudioCue cue, DateTime referenceLocal)
    {
        switch (cue.TriggerMode)
        {
            case CueTriggerMode.ClockBased:
                if (cue.ClockTime is { } clock)
                    yield return NextAfter(referenceLocal, clock);
                break;

            case CueTriggerMode.Interval:
                if (cue.IntervalStart is { } s && cue.IntervalEnd is { } e &&
                    cue.IntervalEvery is { } every && every > TimeSpan.Zero)
                {
                    var windowLen = e >= s ? (e - s) : (TimeSpan.FromDays(1) - (s - e));
                    // Pick the earliest interval window that hasn't fully ended. If the window
                    // already began before "Start Sleep" was pressed, this still picks it so its
                    // remaining ticks fire (past ticks are dropped later by the activation filter).
                    var startAbs = referenceLocal.Date + s.ToTimeSpan();
                    var endAbs = startAbs + windowLen;
                    for (var dayOffset = -1; dayOffset <= 1; dayOffset++)
                    {
                        var candStart = referenceLocal.Date.AddDays(dayOffset) + s.ToTimeSpan();
                        var candEnd = candStart + windowLen;
                        if (candEnd > referenceLocal)
                        {
                            startAbs = candStart;
                            endAbs = candEnd;
                            break;
                        }
                    }
                    var t = startAbs;
                    var count = 0;
                    while (t <= endAbs && count < MaxCuesPerActivation)
                    {
                        yield return t;
                        t += every;
                        count++;
                    }
                }
                break;

            case CueTriggerMode.SleepRelative:
                if (cue.OffsetFromSleepStart is { } offset && offset >= TimeSpan.Zero)
                    yield return referenceLocal + offset;
                break;
        }
    }

    private static DateTime NextAfter(DateTime reference, TimeOnly clock)
    {
        var candidate = reference.Date + clock.ToTimeSpan();
        while (candidate <= reference) candidate = candidate.AddDays(1);
        return candidate;
    }

    // ─── Platform glue ───────────────────────────────────────────────

#if ANDROID
    public bool ExactAlarmsAllowed
    {
        get
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(31)) return true;
            var am = (AlarmManager?)Android.App.Application.Context.GetSystemService(AContext.AlarmService);
            return am is not null && am.CanScheduleExactAlarms();
        }
    }

    public void RequestExactAlarmPermission()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(31)) return;
        try
        {
            var intent = new Intent(Android.Provider.Settings.ActionRequestScheduleExactAlarm);
            intent.SetData(Android.Net.Uri.Parse("package:" + Android.App.Application.Context.PackageName));
            intent.AddFlags(ActivityFlags.NewTask);
            Android.App.Application.Context.StartActivity(intent);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not open exact-alarm settings"); }
    }

    public bool BatteryOptimizationIgnored
    {
        get
        {
            try
            {
                var ctx = Android.App.Application.Context;
                var pm = (PowerManager?)ctx.GetSystemService(global::Android.Content.Context.PowerService);
                return pm is not null && pm.IsIgnoringBatteryOptimizations(ctx.PackageName!);
            }
            catch { return true; }
        }
    }

    public void RequestBatteryExemption()
    {
        try
        {
            var ctx = Android.App.Application.Context;
            var intent = new Intent(Android.Provider.Settings.ActionRequestIgnoreBatteryOptimizations);
            intent.SetData(Android.Net.Uri.Parse("package:" + ctx.PackageName));
            intent.AddFlags(ActivityFlags.NewTask);
            ctx.StartActivity(intent);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Could not open battery-optimization settings"); }
    }

    private static PendingIntent? BuildAlarmPi(int requestCode, bool isStop, bool isAlarm)
    {
        var ctx = Android.App.Application.Context;
        var intent = new Intent(ctx, typeof(LucidDreamCompanion.Platforms.Android.Receivers.CueAlarmReceiver));
        var action = isStop
            ? LucidDreamCompanion.Platforms.Android.Receivers.CueAlarmReceiver.ActionStop
            : isAlarm
                ? LucidDreamCompanion.Platforms.Android.Receivers.CueAlarmReceiver.ActionAlarm
                : LucidDreamCompanion.Platforms.Android.Receivers.CueAlarmReceiver.ActionFire;
        intent.SetAction(action);
        if (!isStop) intent.PutExtra("logId", requestCode);
        return PendingIntent.GetBroadcast(ctx, requestCode, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
    }

    private void PlatformArmAlarm(int requestCode, DateTime triggerUtc, bool isStop, bool isAlarm = false)
    {
        try
        {
            var am = (AlarmManager?)Android.App.Application.Context.GetSystemService(AContext.AlarmService);
            var pi = BuildAlarmPi(requestCode, isStop, isAlarm);
            if (am is null || pi is null) return;
            var triggerMs = new DateTimeOffset(triggerUtc, TimeSpan.Zero).ToUnixTimeMilliseconds();
            am.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerMs, pi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to arm alarm {Code}", requestCode);
        }
    }

    private void PlatformCancelAlarm(int requestCode, bool isStop, bool isAlarm = false)
    {
        try
        {
            var am = (AlarmManager?)Android.App.Application.Context.GetSystemService(AContext.AlarmService);
            var pi = BuildAlarmPi(requestCode, isStop, isAlarm);
            if (am is not null && pi is not null) am.Cancel(pi);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel alarm {Code}", requestCode);
        }
    }

    private void PlatformStartMonitor()
    {
        try
        {
            var ctx = Android.App.Application.Context;
            var intent = new Intent(ctx, typeof(LucidDreamCompanion.Platforms.Android.Services.CueForegroundService));
            intent.SetAction("com.josef.lucid.cue.START");
            ctx.StartForegroundService(intent);
        }
        catch (Exception ex) { _logger.LogError(ex, "Failed to start cue monitor service"); }
    }

    private void PlatformStopMonitor()
    {
        try
        {
            var ctx = Android.App.Application.Context;
            var intent = new Intent(ctx, typeof(LucidDreamCompanion.Platforms.Android.Services.CueForegroundService));
            ctx.StopService(intent);
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to stop cue monitor service"); }
    }

    private static PendingIntent? BuildRcAudioPi(int requestCode, int audioAssetId, int volumePercent)
    {
        var ctx = Android.App.Application.Context;
        var intent = new Intent(ctx, typeof(LucidDreamCompanion.Platforms.Android.Receivers.CueAlarmReceiver));
        intent.SetAction(LucidDreamCompanion.Platforms.Android.Receivers.CueAlarmReceiver.ActionRcAudio);
        intent.PutExtra("rcAsset", audioAssetId);
        intent.PutExtra("rcVolume", volumePercent);
        return PendingIntent.GetBroadcast(ctx, requestCode, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
    }

    private void PlatformArmRcAudioAlarm(int requestCode, DateTime triggerUtc, int audioAssetId, int volumePercent)
    {
        try
        {
            var am = (AlarmManager?)Android.App.Application.Context.GetSystemService(AContext.AlarmService);
            var pi = BuildRcAudioPi(requestCode, audioAssetId, volumePercent);
            if (am is null || pi is null) return;
            var triggerMs = new DateTimeOffset(triggerUtc, TimeSpan.Zero).ToUnixTimeMilliseconds();
            // Inexact, Doze-friendly — reality checks are habit pings, not alarms, so they don't
            // need exact timing and this needs no SCHEDULE_EXACT_ALARM permission.
            am.SetAndAllowWhileIdle(AlarmType.RtcWakeup, triggerMs, pi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to arm reality-check audio alarm {Code}", requestCode);
        }
    }

    private void PlatformCancelRcAudioAlarm(int requestCode)
    {
        try
        {
            var am = (AlarmManager?)Android.App.Application.Context.GetSystemService(AContext.AlarmService);
            var pi = BuildRcAudioPi(requestCode, 0, 0);
            if (am is not null && pi is not null) am.Cancel(pi);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to cancel reality-check audio alarm {Code}", requestCode);
        }
    }
#else
    public bool ExactAlarmsAllowed => true;
    public void RequestExactAlarmPermission() { }
    public bool BatteryOptimizationIgnored => true;
    public void RequestBatteryExemption() { }
    private void PlatformArmAlarm(int requestCode, DateTime triggerUtc, bool isStop, bool isAlarm = false) { }
    private void PlatformCancelAlarm(int requestCode, bool isStop, bool isAlarm = false) { }
    private void PlatformStartMonitor() { }
    private void PlatformStopMonitor() { }
    private void PlatformArmRcAudioAlarm(int requestCode, DateTime triggerUtc, int audioAssetId, int volumePercent) { }
    private void PlatformCancelRcAudioAlarm(int requestCode) { }
#endif
}
