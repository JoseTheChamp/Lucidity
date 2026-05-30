using Android.App;
using Android.Content;
using Android.OS;
using LucidDreamCompanion.Platforms.Android.Services;
using LucidDreamCompanion.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LucidDreamCompanion.Platforms.Android.Receivers;

/// <summary>
/// Receives the exact-alarm broadcasts armed by <see cref="CueSchedulerService"/>: night cues
/// (handed to the foreground service), the end-of-night stop, and private reality-check sounds.
/// </summary>
[BroadcastReceiver(Exported = false, Enabled = true)]
public class CueAlarmReceiver : BroadcastReceiver
{
    public const string ActionFire = "com.josef.lucid.cue.FIRE";
    public const string ActionStop = "com.josef.lucid.cue.STOP";
    public const string ActionAlarm = "com.josef.lucid.cue.ALARM";
    public const string ActionRcAudio = "com.josef.lucid.cue.RC_AUDIO";

    public override void OnReceive(Context? context, Intent? intent)
    {
        if (context is null || intent is null) return;

        if (intent.Action == ActionStop)
        {
            var pending = GoAsync();
            _ = Task.Run(async () =>
            {
                try
                {
                    var scheduler = IPlatformApplication.Current?.Services?.GetService<ICueSchedulerService>();
                    if (scheduler is not null)
                        await scheduler.DeactivateAsync();
                }
                finally
                {
                    pending?.Finish();
                }
            });
            return;
        }

        if (intent.Action == ActionRcAudio)
        {
            // Private reality check — play a short sound directly, no notification, no foreground
            // service. A short wake lock + GoAsync keeps the process alive for the brief playback.
            var assetId = intent.GetIntExtra("rcAsset", -1);
            var volume = intent.GetIntExtra("rcVolume", 50);
            if (assetId <= 0) return;

            var pending = GoAsync();
            _ = Task.Run(async () =>
            {
                PowerManager.WakeLock? wakeLock = null;
                try
                {
                    var pm = (PowerManager?)context.GetSystemService(Context.PowerService);
                    wakeLock = pm?.NewWakeLock(WakeLockFlags.Partial, "ldc:rc-audio");
                    wakeLock?.Acquire(15_000);

                    var scheduler = IPlatformApplication.Current?.Services?.GetService<ICueSchedulerService>();
                    if (scheduler is not null)
                        await scheduler.PlayRealityCheckAudioAsync(assetId, volume);
                }
                catch
                {
                    // ignore — best-effort playback
                }
                finally
                {
                    try { if (wakeLock is { IsHeld: true }) wakeLock.Release(); } catch { }
                    pending?.Finish();
                }
            });
            return;
        }

        // Night cue (or WBTB alarm) — start the foreground service so playback survives a killed process.
        var logId = intent.GetIntExtra("logId", -1);
        var isAlarm = intent.Action == ActionAlarm;
        var play = new Intent(context, typeof(CueForegroundService));
        play.SetAction(isAlarm ? CueForegroundService.ActionAlarm : CueForegroundService.ActionPlay);
        play.PutExtra(CueForegroundService.ExtraLogId, logId);
        try
        {
            context.StartForegroundService(play);
        }
        catch (Exception)
        {
            // Background-start restrictions can throw on some OEMs; fall back to a plain start.
            context.StartService(play);
        }
    }
}
