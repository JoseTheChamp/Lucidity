using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

public interface ICueSchedulerService
{
    event Action? ActiveProfileChanged;

    Task<int?> GetActiveProfileIdAsync();

    /// <summary>Arms every schedulable cue in the profile and starts the foreground monitor.</summary>
    Task ActivateProfileAsync(int cueProfileId, DateTime sleepStartUtc, int? sleepSessionId = null);

    /// <summary>Cancels all pending alarms, stops the monitor, clears the active profile.</summary>
    Task DeactivateAsync();

    /// <summary>Re-arms still-pending alarms after a reboot.</summary>
    Task RearmAsync();

    Task<IReadOnlyList<ScheduledCueLog>> GetUpcomingAsync(CancellationToken ct = default);
    Task<IReadOnlyList<ScheduledCueLog>> GetRecentAsync(int hours = 24, CancellationToken ct = default);

    /// <summary>Invoked by the foreground service when an alarm fires; plays the cue and logs it.</summary>
    Task PlayCueByLogAsync(int logId);

    /// <summary>Plays a WBTB alarm cue that rings until dismissed; returns once playback has started.</summary>
    Task PlayAlarmByLogAsync(int logId);

    /// <summary>Stops the currently-ringing alarm (user dismissed it).</summary>
    Task StopAlarmAsync();

    // ─── Private-mode reality checks: play a sound instead of showing a notification ───
    Task ScheduleRealityCheckAudioAsync(IReadOnlyList<DateTime> localTimes, int audioAssetId, int volumePercent);
    Task CancelRealityCheckAudioAsync();
    /// <summary>Invoked by the alarm receiver when a private reality-check ping fires.</summary>
    Task PlayRealityCheckAudioAsync(int audioAssetId, int volumePercent);

    bool ExactAlarmsAllowed { get; }
    void RequestExactAlarmPermission();
    bool BatteryOptimizationIgnored { get; }
    void RequestBatteryExemption();
}
