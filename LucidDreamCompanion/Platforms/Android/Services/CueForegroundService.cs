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
    public const string ActionAlarm = "com.josef.lucid.cue.ALARM";
    public const string ActionStopAlarm = "com.josef.lucid.cue.STOP_ALARM";
    public const string ExtraLogId = "logId";

    private const int NotificationId = 7341;
    private const int AlarmNotificationId = 7342;
    private const string ChannelId = "cues_active";
    private const string AlarmChannelId = "cue_alarm";

    private PowerManager.WakeLock? _wakeLock;

    public override IBinder? OnBind(Intent? intent) => null;

    public override StartCommandResult OnStartCommand(Intent? intent, StartCommandFlags flags, int startId)
    {
        var action = intent?.Action ?? ActionStart;

        if (action == ActionStopAlarm)
        {
            _ = HandleStopAlarmAsync();
            StopForegroundCompat();
            StopSelf();
            return StartCommandResult.NotSticky;
        }

        if (action == ActionAlarm)
        {
            var logId = intent?.GetIntExtra(ExtraLogId, -1) ?? -1;
            EnterForegroundAlarm(logId);
            if (logId > 0)
                _ = HandleAlarmAsync(logId);
            return StartCommandResult.Sticky;
        }

        EnterForeground();

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

    private void EnterForegroundAlarm(int logId)
    {
        var notification = BuildAlarmNotification(logId);
        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(29))
                StartForeground(AlarmNotificationId, notification, ForegroundService.TypeMediaPlayback);
            else
                StartForeground(AlarmNotificationId, notification);
        }
        catch (Exception ex)
        {
            Logger()?.LogError(ex, "StartForeground (alarm) failed");
            StartForeground(AlarmNotificationId, notification);
        }
    }

    private Notification BuildAlarmNotification(int logId)
    {
        // Tapping the alarm body opens the app over the lock screen.
        var launch = new Intent(this, typeof(MainActivity));
        launch.AddFlags(ActivityFlags.SingleTop);
        var contentPi = PendingIntent.GetActivity(this, 0, launch,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        // Dismiss action stops the ringing and tears the service down.
        var dismiss = new Intent(this, typeof(CueForegroundService));
        dismiss.SetAction(ActionStopAlarm);
        dismiss.PutExtra(ExtraLogId, logId);
        var dismissPi = PendingIntent.GetService(this, 1, dismiss,
            PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable);

        var builder = new NotificationCompat.Builder(this, AlarmChannelId);
        builder.SetContentTitle("Wake up — WBTB");
        builder.SetContentText("Tap Dismiss when you're awake.");
        builder.SetSmallIcon(global::Android.Resource.Drawable.IcLockIdleAlarm);
        builder.SetOngoing(true);
        builder.SetAutoCancel(false);
        builder.SetCategory(NotificationCompat.CategoryAlarm);
        builder.SetPriority(NotificationCompat.PriorityMax);
        builder.SetVisibility(NotificationCompat.VisibilityPublic);
        builder.SetContentIntent(contentPi);
        builder.SetFullScreenIntent(contentPi, true);
        builder.AddAction(global::Android.Resource.Drawable.IcMenuCloseClearCancel, "Dismiss", dismissPi);
        return builder.Build()!;
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

    private async Task HandleAlarmAsync(int logId)
    {
        // Hold the wake lock for the whole ring; released on dismiss (HandleStopAlarmAsync).
        AcquireWakeLock(20 * 60 * 1000);
        try
        {
            var scheduler = IPlatformApplication.Current?.Services?.GetService<ICueSchedulerService>();
            if (scheduler is not null)
                await scheduler.PlayAlarmByLogAsync(logId);
        }
        catch (Exception ex)
        {
            Logger()?.LogError(ex, "Alarm playback failed for log {LogId}", logId);
        }
    }

    private async Task HandleStopAlarmAsync()
    {
        try
        {
            var scheduler = IPlatformApplication.Current?.Services?.GetService<ICueSchedulerService>();
            if (scheduler is not null)
                await scheduler.StopAlarmAsync();
        }
        catch (Exception ex)
        {
            Logger()?.LogError(ex, "Alarm dismiss failed");
        }
        finally
        {
            ReleaseWakeLock();
        }
    }

    private void AcquireWakeLock(int timeoutMs = 6 * 60 * 1000)
    {
        try
        {
            var pm = (PowerManager?)GetSystemService(PowerService);
            _wakeLock = pm?.NewWakeLock(WakeLockFlags.Partial, "ldc:cue-playback");
            _wakeLock?.Acquire(timeoutMs);
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
