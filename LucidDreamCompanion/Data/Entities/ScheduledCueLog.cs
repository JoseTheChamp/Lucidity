namespace LucidDreamCompanion.Data.Entities;

public static class CueLogStatus
{
    public const string Scheduled = "Scheduled";
    public const string Fired = "Fired";
    public const string Skipped = "Skipped";
    public const string Failed = "Failed";
    public const string Cancelled = "Cancelled";
}

public class ScheduledCueLog
{
    public int Id { get; set; }

    // Nullable: in Phase 4 cues can be armed without a sleep session (sessions arrive in Phase 5).
    public int? SleepSessionId { get; set; }
    public SleepSession? SleepSession { get; set; }

    // Kept directly so a log survives even if its cue or profile is later deleted.
    public int? CueProfileId { get; set; }
    public int AudioCueId { get; set; }
    public string CueLabel { get; set; } = "";

    public DateTime ScheduledFor { get; set; }            // UTC
    public DateTime? FiredAt { get; set; }                // UTC
    public string? Status { get; set; }                   // see CueLogStatus

    // Snapshotted from the cue so a reboot re-arm knows to ring-until-dismissed (see CueSchedulerService).
    public bool IsAlarm { get; set; }
}
