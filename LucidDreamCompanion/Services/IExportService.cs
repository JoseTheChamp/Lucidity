namespace LucidDreamCompanion.Services;

public enum ImportMode { Merge, Replace }

public record ImportResult(bool Success, string Message, int EntriesImported);

public interface IExportService
{
    /// <summary>Serialises the whole database to a JSON file and opens the share sheet.</summary>
    Task<bool> ExportJsonAsync(CancellationToken ct = default);

    /// <summary>Writes one Markdown file per dream into a zip (dreams/YYYY/MM/) and opens the share sheet.</summary>
    Task<bool> ExportMarkdownAsync(CancellationToken ct = default);

    /// <summary>Picks a JSON backup and restores it (merge appends, replace wipes first).</summary>
    Task<ImportResult> ImportJsonAsync(ImportMode mode, CancellationToken ct = default);

    Task ClearLogsAsync(CancellationToken ct = default);
    Task VacuumAsync(CancellationToken ct = default);
    Task DeleteAllDataAsync(CancellationToken ct = default);
}
