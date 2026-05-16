using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LucidDreamCompanion.Services;

public class TechniqueTemplateService : ITechniqueTemplateService
{
    private readonly IDbContextFactory<LucidDbContext> _factory;

    public TechniqueTemplateService(IDbContextFactory<LucidDbContext> factory)
    {
        _factory = factory;
    }

    public IReadOnlyList<TechniqueDefinition> Definitions => TechniqueDefinitions.All;

    public TechniqueDefinition? GetDefinition(string key) => TechniqueDefinitions.ByKey(key);

    public async Task<CueProfile?> GetProfileAsync(string key, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.CueProfiles
            .AsNoTracking()
            .Include(p => p.Cues)
            .FirstOrDefaultAsync(p => p.IsTechniqueTemplate && p.TechniqueKey == key, ct);
    }
}
