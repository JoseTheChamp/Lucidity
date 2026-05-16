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

    private const int MaxCuesPerActivation = 60;

    private readonly IDbContextFactory<LucidDbContext> _dbFactory;
    private readonly IAppSettingsService _settings;
    private readonly IAudioService _audio;
    private readonly ILogger<CueSchedulerService> _logger;

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
            PlatformArmAlarm(log.Id, log.ScheduledFor, isStop: false);
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
        await using var db = await _dbFactory.CreateDbContextAsync();
        var pending = await db.ScheduledCueLogs
            .Where(l => l.Status == CueLogStatus.Scheduled)
            .ToListAsync();
        foreach (var log in pending)
        {
            PlatformCancelAlarm(log.Id, isStop: false);
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
            PlatformArmAlarm(log.Id, log.ScheduledFor, isStop: false);
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
                    var startAbs = NextAfter(referenceLocal, s);
                    var windowLen = e >= s ? (e - s) : (TimeSpan.FromDays(1) - (s - e));
                    var endAbs = startAbs + windowLen;
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

    private static PendingIntent? BuildAlarmPi(int requestCode, bool isStop)
    {
        var ctx = Android.App.Application.Context;
        var intent = new Intent(ctx, typeof(LucidDreamCompanion.Platforms.Android.Receivers.CueAlarmReceiver));
        intent.SetAction(isStop ? "com.josef.lucid.cue.STOP" : "com.josef.lucid.cue.FIRE");
        if (!isStop) intent.PutExtra("logId", requestCode);
        return PendingIntent.GetBroadcast(ctx, requestCode, intent,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);
    }

    private void PlatformArmAlarm(int requestCode, DateTime triggerUtc, bool isStop)
    {
        try
        {
            var am = (AlarmManager?)Android.App.Application.Context.GetSystemService(AContext.AlarmService);
            var pi = BuildAlarmPi(requestCode, isStop);
            if (am is null || pi is null) return;
            var triggerMs = new DateTimeOffset(triggerUtc, TimeSpan.Zero).ToUnixTimeMilliseconds();
            am.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, triggerMs, pi);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to arm alarm {Code}", requestCode);
        }
    }

    private void PlatformCancelAlarm(int requestCode, bool isStop)
    {
        try
        {
            var am = (AlarmManager?)Android.App.Application.Context.GetSystemService(AContext.AlarmService);
            var pi = BuildAlarmPi(requestCode, isStop);
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
#else
    public bool ExactAlarmsAllowed => true;
    public void RequestExactAlarmPermission() { }
    public bool BatteryOptimizationIgnored => true;
    public void RequestBatteryExemption() { }
    private void PlatformArmAlarm(int requestCode, DateTime triggerUtc, bool isStop) { }
    private void PlatformCancelAlarm(int requestCode, bool isStop) { }
    private void PlatformStartMonitor() { }
    private void PlatformStopMonitor() { }
#endif
}
