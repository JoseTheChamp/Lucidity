using Android.App;
using Android.Content;
using LucidDreamCompanion.Platforms.Android.Services;
using LucidDreamCompanion.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LucidDreamCompanion.Platforms.Android.Receivers;

/// <summary>
/// Receives the exact-alarm broadcasts armed by <see cref="CueSchedulerService"/> and either
/// hands the cue to the foreground service for playback or ends the night's session.
/// </summary>
[BroadcastReceiver(Exported = false, Enabled = true)]
public class CueAlarmReceiver : BroadcastReceiver
{
    public const string ActionFire = "com.josef.lucid.cue.FIRE";
    public const string ActionStop = "com.josef.lucid.cue.STOP";

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

        // Cue fire — start (or wake) the foreground service so playback survives a killed process.
        var logId = intent.GetIntExtra("logId", -1);
        var play = new Intent(context, typeof(CueForegroundService));
        play.SetAction(CueForegroundService.ActionPlay);
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
