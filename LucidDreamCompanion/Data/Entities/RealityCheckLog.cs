namespace LucidDreamCompanion.Data.Entities;

public class RealityCheckLog
{
    public int Id { get; set; }
    public DateTime FiredAt { get; set; }       // UTC, when user opened the prompt
    public DateTime? CompletedAt { get; set; }  // UTC, when "Done" was tapped
    public bool WasDreaming { get; set; }       // user's self-report on the prompt
}
