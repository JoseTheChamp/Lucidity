namespace LucidDreamCompanion.Data.Entities;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string ColorHex { get; set; } = "#888888";
    public bool IsBuiltIn { get; set; }
    public List<DreamEntry> Entries { get; set; } = new();
}
