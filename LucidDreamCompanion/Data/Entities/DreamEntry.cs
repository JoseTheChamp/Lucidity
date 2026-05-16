namespace LucidDreamCompanion.Data.Entities;

public class DreamEntry
{
    public int Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime DreamDate { get; set; }
    public string Title { get; set; } = "";
    public string Body { get; set; } = "";
    public LucidityLevel Lucidity { get; set; }
    public int? SleepQuality { get; set; }
    public int? SleepSessionId { get; set; }
    public SleepSession? SleepSession { get; set; }
    public List<Tag> Tags { get; set; } = new();
}
