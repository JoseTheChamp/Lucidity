using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LucidDreamCompanion.Services;

public class JournalReminderService : IJournalReminderService
{
    private readonly IDbContextFactory<LucidDbContext> _factory;

    public event Action? Changed;

    public JournalReminderService(IDbContextFactory<LucidDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<IReadOnlyList<JournalReminder>> ListAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        return await db.JournalReminders.AsNoTracking().OrderBy(r => r.Time).ToListAsync(ct);
    }

    public async Task<JournalReminder> AddAsync(TimeOnly time, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var r = new JournalReminder { Time = time, Enabled = true };
        db.JournalReminders.Add(r);
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
        return r;
    }

    public async Task UpdateAsync(int id, TimeOnly time, bool enabled, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var r = await db.JournalReminders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return;
        r.Time = time;
        r.Enabled = enabled;
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }

    public async Task DeleteAsync(int id, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var r = await db.JournalReminders.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (r is null) return;
        db.JournalReminders.Remove(r);
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }
}
