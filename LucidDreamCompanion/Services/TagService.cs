using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace LucidDreamCompanion.Services;

public class TagService : ITagService
{
    private readonly IDbContextFactory<LucidDbContext> _dbFactory;
    private readonly ILogger<TagService> _logger;

    public event Action? Changed;

    public TagService(IDbContextFactory<LucidDbContext> dbFactory, ILogger<TagService> logger)
    {
        _dbFactory = dbFactory;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Tag>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Tags.AsNoTracking().OrderBy(t => t.Name).ToListAsync(ct);
    }

    public async Task<Tag?> GetAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.Tags.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct);
    }

    public async Task<Tag> CreateAsync(string name, string colorHex, CancellationToken ct = default)
    {
        var trimmed = name.Trim();
        if (string.IsNullOrEmpty(trimmed)) throw new ArgumentException("Tag name required.");
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var existing = await db.Tags.FirstOrDefaultAsync(t => t.Name.ToLower() == trimmed.ToLower(), ct);
        if (existing is not null) return existing;
        var tag = new Tag { Name = trimmed, ColorHex = colorHex, IsBuiltIn = false };
        db.Tags.Add(tag);
        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Created tag {Name}", trimmed);
        Changed?.Invoke();
        return tag;
    }

    public async Task UpdateAsync(int id, string name, string colorHex, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null) return;
        tag.Name = name.Trim();
        tag.ColorHex = colorHex;
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        var tag = await db.Tags.Include(t => t.Entries).FirstOrDefaultAsync(t => t.Id == id, ct);
        if (tag is null) return;
        tag.Entries.Clear();
        db.Tags.Remove(tag);
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }

    public async Task<int> CountUsageAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _dbFactory.CreateDbContextAsync(ct);
        return await db.DreamEntries.CountAsync(e => e.Tags.Any(t => t.Id == id), ct);
    }
}
