namespace LucidDreamCompanion.Data.Entities;

public class SleepSession
{
    public int Id { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string? Notes { get; set; }
    public int? Quality { get; set; }            // 1-5, captured when the session ends
    public int? TechniqueProfileId { get; set; }
    public CueProfile? TechniqueProfile { get; set; }
    public List<DreamEntry> Entries { get; set; } = new();
    public List<ScheduledCueLog> CueLogs { get; set; } = new();
}
