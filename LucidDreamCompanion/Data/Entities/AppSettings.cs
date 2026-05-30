namespace LucidDreamCompanion.Data.Entities;

public enum RealityCheckMode
{
    Count = 0,               // N pings spread evenly across the window
    FixedInterval = 1,       // every X minutes from start to end
}

public enum RealityCheckDelivery
{
    Notification = 0,        // normal — a visible notification you tap
    Audio = 1,               // private — the phone quietly plays a cue sound, no notification
}

public class AppSettings
{
    public int Id { get; set; }

    // Legacy slots kept so old DBs upgrade in place; new code reads from JournalReminders table.
    public TimeOnly JournalReminderTime1 { get; set; } = new(6, 30);
    public TimeOnly? JournalReminderTime2 { get; set; } = new(8, 0);

    public bool RealityCheckEnabled { get; set; } = false;
    public TimeOnly RealityCheckStart { get; set; } = new(9, 0);
    public TimeOnly RealityCheckEnd { get; set; } = new(22, 0);

    public RealityCheckMode RealityCheckMode { get; set; } = RealityCheckMode.Count;
    public int RealityCheckCountPerDay { get; set; } = 5;
    public int RealityCheckIntervalMinutes { get; set; } = 90;

    // How a reality check reaches you: a notification (normal) or a quietly-played sound (private).
    public RealityCheckDelivery RealityCheckDelivery { get; set; } = RealityCheckDelivery.Notification;
    public int? RealityCheckAudioAssetId { get; set; }
    public int RealityCheckVolumePercent { get; set; } = 50;

    public int? ActiveCueProfileId { get; set; }

    // Voice journaling — BCP-47 tag (e.g. "en-US", "cs-CZ"). Empty = device default.
    public string SpeechLocale { get; set; } = "";
}
