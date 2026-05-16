using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using LucidDreamCompanion.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LucidDreamCompanion.Platforms.Android.Services;

[Service(Exported = false, ForegroundServiceType = ForegroundService.TypeMediaPlayback)]
public class CueForegroundService : Service
{
    public const string ActionStart = "com.josef.lucid.cue.START";
    public const string ActionPlay = "com.josef.lucid.cue.PLAY";
    public const string ActionStop = "com.josef.lucid.cue.STOP";
    public const string ExtraLogId = "logId";

    private const int NotificationId = 7341;
    private const string ChannelId = "cues_active";

    private PowerManager.WakeLock? _wakeLock;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        EnterForeground();

        var action = intent?.Action ?? ActionStart;
        if (action == ActionStop)
        {
            StopForegroundCompat();
            StopSelf();
            return StartCommandResult.NotSticky;
        }
        if (action == ActionPlay)
        {
            var logId = intent?.GetIntExtra(ExtraLogId, -1) ?? -1;
            if (logId > 0)
                _ = HandlePlayAsync(logId);
        }
        return StartCommandResult.Sticky;
    }

    private void EnterForeground()
    {
        var notification = BuildNotification();
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                StartForeground(NotificationId, notification, ForegroundService.TypeMediaPlayback);
            else
                StartForeground(NotificationId, notification);
        }
        catch (Exception ex)
        {
            Logger()?.LogError(ex, "StartForeground failed");
            StartForeground(NotificationId, notification);
        }
    }

    private Notification BuildNotification()
    {
        var launch = new Intent(this, typeof(MainActivity));
        launch.AddFlags(ActivityFlags.SingleTop);
        var contentPi = PendingIntent.GetActivity(this, 0, launch,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = new NotificationCompat.Builder(this, ChannelId);
        builder.SetContentTitle("Lucid Dream Companion");
        builder.SetContentText("Monitoring cues tonight");
        builder.SetSmallIcon(global::Android.Resource.Drawable.IcLockIdleAlarm);
        builder.SetOngoing(true);
        builder.SetCategory(NotificationCompat.CategoryService);
        builder.SetPriority(NotificationCompat.PriorityLow);
        builder.SetContentIntent(contentPi);
        return builder.Build()!;
    }

    private async Task HandlePlayAsync(int logId)
    {
        AcquireWakeLock();
        try
        {
            var scheduler = IPlatformApplication.Current?.Services?.GetService<ICueSchedulerService>();
            if (scheduler is not null)
                await scheduler.PlayCueByLogAsync(logId);
        }
        catch (Exception ex)
        {
            Logger()?.LogError(ex, "Cue playback failed for log {LogId}", logId);
        }
        finally
        {
            ReleaseWakeLock();
        }
    }

    private void AcquireWakeLock()
    {
        try
        {
            var pm = (PowerManager?)GetSystemService(PowerService);
            _wakeLock = pm?.NewWakeLock(WakeLockFlags.Partial, "ldc:cue-playback");
            _wakeLock?.Acquire(6 * 60 * 1000);
        }
        catch (Exception ex)
        {
            Logger()?.LogWarning(ex, "Could not acquire wake lock");
        }
    }

    private void ReleaseWakeLock()
    {
        try
        {
            if (_wakeLock is { IsHeld: true })
                _wakeLock.Release();
        }
        catch { /* ignore */ }
        _wakeLock = null;
    }

    private void StopForegroundCompat()
    {
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(24))
                StopForeground(StopForegroundFlags.Remove);
            else
#pragma warning disable CA1422
                StopForeground(true);
#pragma warning restore CA1422
        }
        catch { /* ignore */ }
    }

    public override void OnDestroy()
    {
        ReleaseWakeLock();
        base.OnDestroy();
    }

    private static ILogger? Logger()
        => IPlatformApplication.Current?.Services?.GetService<ILogger<CueForegroundService>>();
}
