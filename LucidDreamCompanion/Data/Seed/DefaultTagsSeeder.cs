using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LucidDreamCompanion.Data.Seed;

public static class DefaultTagsSeeder
{
    private static readonly (string Name, string Color)[] BuiltIn =
    {
        ("dream",            "#6B8AFF"),
        ("lucid",            "#5EEAD4"),
        ("nightmare",        "#F87171"),
        ("recurring",        "#A78BFA"),
        ("false awakening",  "#FBBF24"),
        ("bad sleep",        "#8B92A8"),
        ("WBTB",             "#34D399"),
        ("FILD",             "#60A5FA"),
        ("MILD",             "#F472B6"),
    };

    public static async Task SeedAsync(LucidDbContext db, CancellationToken ct = default)
    {
        var existing = await db.Tags
            .Select(t => t.Name.ToLower())
            .ToListAsync(ct);
        var existingSet = new HashSet<string>(existing);

        foreach (var (name, color) in BuiltIn)
        {
            if (!existingSet.Contains(name.ToLower()))
            {
                db.Tags.Add(new Tag { Name = name, ColorHex = color, IsBuiltIn = true });
            }
        }
        if (!await db.AppSettings.AnyAsync(s => s.Id == 1, ct))
        {
            db.AppSettings.Add(new AppSettings { Id = 1 });
        }
        await db.SaveChangesAsync(ct);
    }
}
