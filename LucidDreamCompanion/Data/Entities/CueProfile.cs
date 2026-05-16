namespace LucidDreamCompanion.Data.Entities;

public class CueProfile
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public bool IsTechniqueTemplate { get; set; }
    public string? TechniqueKey { get; set; }
    public List<AudioCue> Cues { get; set; } = new();
}
