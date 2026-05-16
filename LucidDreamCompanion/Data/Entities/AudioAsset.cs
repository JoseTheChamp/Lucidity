namespace LucidDreamCompanion.Data.Entities;

public enum AudioAssetSource { Bundled, UserRecording, ImportedFile }

public class AudioAsset
{
    public int Id { get; set; }
    public string DisplayName { get; set; } = "";
    public AudioAssetSource Source { get; set; }
    public string PathOrResourceKey { get; set; } = "";
    public int DurationMs { get; set; }
}
