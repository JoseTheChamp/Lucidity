namespace LucidDreamCompanion.Data.Entities;

public class JournalReminder
{
    public int Id { get; set; }
    public TimeOnly Time { get; set; }
    public bool Enabled { get; set; } = true;
}
