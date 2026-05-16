namespace LucidDreamCompanion.Data.Entities;

public enum RealityCheckMode
{
    RandomCountPerDay = 0,   // N pings spread randomly across the window
    FixedInterval = 1,       // every X minutes from start to end
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

    public RealityCheckMode RealityCheckMode { get; set; } = RealityCheckMode.RandomCountPerDay;
    public int RealityCheckCountPerDay { get; set; } = 5;
    public int RealityCheckIntervalMinutes { get; set; } = 90;

    public int? ActiveCueProfileId { get; set; }

    // Voice journaling — BCP-47 tag (e.g. "en-US", "cs-CZ"). Empty = device default.
    public string SpeechLocale { get; set; } = "";
}
