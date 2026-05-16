using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

public interface ITechniqueTemplateService
{
    IReadOnlyList<TechniqueDefinition> Definitions { get; }
    TechniqueDefinition? GetDefinition(string key);

    /// <summary>The seeded (locked) cue profile for a technique key.</summary>
    Task<CueProfile?> GetProfileAsync(string key, CancellationToken ct = default);
}
