using Android.App;
using Android.Content;
using LucidDreamCompanion.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LucidDreamCompanion.Platforms.Android.Receivers;

/// <summary>
/// After a reboot, re-arms any still-pending cue alarms for the active profile.
/// </summary>
[BroadcastReceiver(Exported = true, Enabled = true, Permission = "android.permission.RECEIVE_BOOT_COMPLETED")]
[IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionLockedBootCompleted })]
public class CueBootReceiver : BroadcastReceiver
{
    public override void OnReceive(Context? context, Intent? intent)
    {
        if (intent?.Action is not (Intent.ActionBootCompleted or Intent.ActionLockedBootCompleted))
            return;

        var pending = GoAsync();
        _ = Task.Run(async () =>
        {
            try
            {
                var scheduler = IPlatformApplication.Current?.Services?.GetService<ICueSchedulerService>();
                if (scheduler is not null)
                    await scheduler.RearmAsync();
            }
            finally
            {
                pending?.Finish();
            }
        });
    }
}
