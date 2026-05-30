using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace LucidDreamCompanion.Services;

public class AppSettingsService : IAppSettingsService
{
    private readonly IDbContextFactory<LucidDbContext> _factory;

    public event Action? Changed;

    public AppSettingsService(IDbContextFactory<LucidDbContext> factory)
    {
        _factory = factory;
    }

    public async Task<AppSettings> GetAsync(CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var s = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (s is null)
        {
            s = new AppSettings { Id = 1 };
            db.AppSettings.Add(s);
            await db.SaveChangesAsync(ct);
        }
        return s;
    }

    public async Task SaveAsync(AppSettings settings, CancellationToken ct = default)
    {
        await using var db = await _factory.CreateDbContextAsync(ct);
        var tracked = await db.AppSettings.FirstOrDefaultAsync(x => x.Id == 1, ct);
        if (tracked is null)
        {
            settings.Id = 1;
            db.AppSettings.Add(settings);
        }
        else
        {
            tracked.JournalReminderTime1 = settings.JournalReminderTime1;
            tracked.JournalReminderTime2 = settings.JournalReminderTime2;
            tracked.RealityCheckEnabled = settings.RealityCheckEnabled;
            tracked.RealityCheckStart = settings.RealityCheckStart;
            tracked.RealityCheckEnd = settings.RealityCheckEnd;
            tracked.RealityCheckMode = settings.RealityCheckMode;
            tracked.RealityCheckCountPerDay = settings.RealityCheckCountPerDay;
            tracked.RealityCheckIntervalMinutes = settings.RealityCheckIntervalMinutes;
            tracked.RealityCheckDelivery = settings.RealityCheckDelivery;
            tracked.RealityCheckAudioAssetId = settings.RealityCheckAudioAssetId;
            tracked.RealityCheckVolumePercent = settings.RealityCheckVolumePercent;
            tracked.ActiveCueProfileId = settings.ActiveCueProfileId;
            tracked.SpeechLocale = settings.SpeechLocale;
        }
        await db.SaveChangesAsync(ct);
        Changed?.Invoke();
    }
}
