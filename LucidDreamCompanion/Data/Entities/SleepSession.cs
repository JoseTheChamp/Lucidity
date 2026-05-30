using System.ComponentModel.DataAnnotations.Schema;

namespace LucidDreamCompanion.Data.Entities;

public class SleepSession
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }      // UTC
    public DateTime? EndedAt { get; set; }       // UTC, null while in progress
    public string? Notes { get; set; }
    public string? Aim { get; set; }             // the intention set for the night
    public int? Quality { get; set; }            // 1-5, captured when the session ends
    public int? TechniqueProfileId { get; set; }
    public CueProfile? TechniqueProfile { get; set; }
    public List<DreamEntry> Entries { get; set; } = new();
    public List<ScheduledCueLog> CueLogs { get; set; } = new();

    /// <summary>
    /// The calendar date the bulk of the session falls on (its midpoint) — e.g. a 23:30→07:00
    /// night and a 00:20→06:00 night both belong to the same morning's date.
    /// </summary>
    [NotMapped]
    public DateTime MainDate
    {
        get
        {
            var start = StartedAt.ToLocalTime();
            var end = (EndedAt ?? DateTime.UtcNow).ToLocalTime();
            if (end < start) end = start;
            return (start + (end - start) / 2).Date;
        }
    }
}
