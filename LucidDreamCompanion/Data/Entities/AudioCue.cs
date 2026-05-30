namespace LucidDreamCompanion.Data.Entities;

public enum CueTriggerMode { SleepRelative, ClockBased, Interval }

public class AudioCue
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
    public CueTriggerMode TriggerMode { get; set; }
    public TimeSpan? OffsetFromSleepStart { get; set; }
    public TimeOnly? ClockTime { get; set; }
    public TimeOnly? IntervalStart { get; set; }
    public TimeOnly? IntervalEnd { get; set; }
    public TimeSpan? IntervalEvery { get; set; }
    public int AudioAssetId { get; set; }
    public AudioAsset AudioAsset { get; set; } = null!;
    public int VolumePercent { get; set; } = 30;
    public int FadeInSeconds { get; set; } = 5;
    public int LoopCount { get; set; } = 1;
    public string? VibrationPattern { get; set; }
    public bool Enabled { get; set; } = true;
    public int SortOrder { get; set; }

    /// <summary>
    /// An alarm rings at high volume until dismissed (WBTB), instead of playing once/looping
    /// a fixed number of times. Loops continuously with repeating vibration and a full-screen
    /// dismissible notification.
    /// </summary>
    public bool IsAlarm { get; set; }
    public int? CueProfileId { get; set; }
    public CueProfile? CueProfile { get; set; }
}
