using LucidDreamCompanion.Data;
using LucidDreamCompanion.Data.Entities;
using LucidDreamCompanion.Data.Seed;
using LucidDreamCompanion.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models.AndroidOption;
using INotificationService = LucidDreamCompanion.Services.INotificationService;

namespace LucidDreamCompanion;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseLocalNotification(cfg =>
            {
                cfg.AddAndroid(android =>
                {
                    android.AddChannel(new AndroidNotificationChannelRequest
                    {
                        Id = NotificationService.ChannelJournal,
                        Name = "Journal reminders",
                        Description = "Daily nudges to write down your dreams.",
                        Importance = AndroidImportance.Default,
                    });
                    android.AddChannel(new AndroidNotificationChannelRequest
                    {
                        Id = NotificationService.ChannelRealityCheck,
                        Name = "Reality checks",
                        Description = "Daytime prompts to test whether you're dreaming.",
                        Importance = AndroidImportance.High,
                        VibrationPattern = new long[] { 0, 180, 80, 180 },
                    });
                    android.AddChannel(new AndroidNotificationChannelRequest
                    {
                        Id = NotificationService.ChannelCuesActive,
                        Name = "Cue session",
                        Description = "Foreground notification while overnight cues are armed.",
                        Importance = AndroidImportance.Low,
                    });
                    android.AddChannel(new AndroidNotificationChannelRequest
                    {
                        Id = NotificationService.ChannelCueAlarm,
                        Name = "Wake-up alarm",
                        Description = "WBTB alarm that rings until you dismiss it.",
                        Importance = AndroidImportance.High,
                        VibrationPattern = new long[] { 0, 800, 400, 800 },
                    });
                });
            })
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        builder.Services.AddMauiBlazorWebView();

        var dbPath = Path.Combine(FileSystem.AppDataDirectory, "lucid.db");
        builder.Services.AddDbContextFactory<LucidDbContext>(opts =>
            opts.UseSqlite($"Data Source={dbPath}"));

        builder.Services.AddSingleton<IDreamJournalService, DreamJournalService>();
        builder.Services.AddSingleton<ITagService, TagService>();
        builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();
        builder.Services.AddSingleton<IJournalReminderService, JournalReminderService>();
        builder.Services.AddSingleton<INotificationService, NotificationService>();
        builder.Services.AddSingleton<IAudioService, AudioService>();
        builder.Services.AddSingleton<ICueProfileService, CueProfileService>();
        builder.Services.AddSingleton<ICueSchedulerService, CueSchedulerService>();
        builder.Services.AddSingleton<ISleepSessionService, SleepSessionService>();
        builder.Services.AddSingleton<ISpeechToTextService, SpeechToTextService>();
        builder.Services.AddSingleton<ITechniqueTemplateService, TechniqueTemplateService>();
        builder.Services.AddSingleton<IStatsService, StatsService>();
        builder.Services.AddSingleton<IExportService, ExportService>();
        builder.Services.AddSingleton<NotificationRouter>();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        InitializeDatabaseAsync(app.Services).GetAwaiter().GetResult();
        WireNotificationTapHandler(app.Services);

        // Re-arm reminders and reality-check pings on each launch (covers reboots and daily renewal).
        _ = RescheduleOnStartupAsync(app.Services);

        return app;
    }

    private static async Task RescheduleOnStartupAsync(IServiceProvider services)
    {
        try
        {
            var notifications = services.GetRequiredService<INotificationService>();
            await notifications.RescheduleAllAsync();
        }
        catch (Exception ex)
        {
            services.GetRequiredService<ILogger<NotificationService>>()
                .LogWarning(ex, "Startup reschedule failed");
        }
    }

    private static async Task InitializeDatabaseAsync(IServiceProvider services)
    {
        var factory = services.GetRequiredService<IDbContextFactory<LucidDbContext>>();
        var logger = services.GetRequiredService<ILogger<LucidDbContext>>();
        await using var db = await factory.CreateDbContextAsync();
        await db.Database.EnsureCreatedAsync();
        await ApplyAdditiveSchemaAsync(db);
        await DefaultTagsSeeder.SeedAsync(db);
        await BundledAudioSeeder.SeedAsync(db);
        await TechniqueTemplatesSeeder.SeedAsync(db);
        logger.LogInformation("Database initialized at {Path}", db.Database.GetDbConnection().DataSource);
    }

    private static async Task ApplyAdditiveSchemaAsync(LucidDbContext db)
    {
        // Idempotent forward-only schema patches for old DBs. EF migrations land in ~Phase 5.
        await db.Database.ExecuteSqlRawAsync(@"
            CREATE TABLE IF NOT EXISTS RealityCheckLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                FiredAt TEXT NOT NULL,
                CompletedAt TEXT NULL,
                WasDreaming INTEGER NOT NULL
            );
            CREATE TABLE IF NOT EXISTS JournalReminders (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                Time TEXT NOT NULL,
                Enabled INTEGER NOT NULL
            );");

        // Phase 3 follow-up: bundled audio keys were originally stored without the "Audio/" prefix.
        // Normalize any old rows so the seeder doesn't create duplicates.
        await db.Database.ExecuteSqlRawAsync(
            "UPDATE AudioAssets SET PathOrResourceKey = 'Audio/' || PathOrResourceKey " +
            "WHERE Source = 0 AND PathOrResourceKey NOT LIKE 'Audio/%' AND PathOrResourceKey NOT LIKE '%/%';");

        // Phase 4: ScheduledCueLogs gained nullable SleepSessionId + CueProfileId + CueLabel.
        // The table has never held rows in shipped builds, so recreate it cleanly.
        await db.Database.ExecuteSqlRawAsync(@"
            DROP TABLE IF EXISTS ScheduledCueLogs;
            CREATE TABLE ScheduledCueLogs (
                Id INTEGER PRIMARY KEY AUTOINCREMENT,
                SleepSessionId INTEGER NULL,
                CueProfileId INTEGER NULL,
                AudioCueId INTEGER NOT NULL,
                CueLabel TEXT NOT NULL DEFAULT '',
                ScheduledFor TEXT NOT NULL,
                FiredAt TEXT NULL,
                Status TEXT NULL,
                IsAlarm INTEGER NOT NULL DEFAULT 0
            );
            CREATE INDEX IF NOT EXISTS IX_ScheduledCueLogs_ScheduledFor ON ScheduledCueLogs(ScheduledFor);");

        // Phase 4: AudioCue gained a SortOrder column.
        await EnsureColumnAsync(db, "AudioCues", "SortOrder", "INTEGER NOT NULL DEFAULT 0");

        // WBTB alarm: a cue can ring until dismissed; ScheduledCueLogs carries it for boot re-arm.
        await EnsureColumnAsync(db, "AudioCues", "IsAlarm", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(db, "ScheduledCueLogs", "IsAlarm", "INTEGER NOT NULL DEFAULT 0");

        // Phase 5: SleepSession gained a Quality column.
        await EnsureColumnAsync(db, "SleepSessions", "Quality", "INTEGER NULL");
        // Sleep aim/intention text.
        await EnsureColumnAsync(db, "SleepSessions", "Aim", "TEXT NULL");

        // Phase 6: AppSettings gained a SpeechLocale column.
        await EnsureColumnAsync(db, "AppSettings", "SpeechLocale", "TEXT NOT NULL DEFAULT ''");

        // Reality-check delivery mode (notification vs private audio cue).
        await EnsureColumnAsync(db, "AppSettings", "RealityCheckDelivery", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(db, "AppSettings", "RealityCheckAudioAssetId", "INTEGER NULL");
        await EnsureColumnAsync(db, "AppSettings", "RealityCheckVolumePercent", "INTEGER NOT NULL DEFAULT 50");

        // Phase 2: AppSettings reality-check mode/interval columns.
        await EnsureColumnAsync(db, "AppSettings", "RealityCheckMode", "INTEGER NOT NULL DEFAULT 0");
        await EnsureColumnAsync(db, "AppSettings", "RealityCheckIntervalMinutes", "INTEGER NOT NULL DEFAULT 90");

        // One-time migration of legacy slot times into the new table.
        if (!await db.JournalReminders.AnyAsync())
        {
            var s = await db.AppSettings.AsNoTracking().FirstOrDefaultAsync(x => x.Id == 1);
            if (s is not null)
            {
                db.JournalReminders.Add(new JournalReminder { Time = s.JournalReminderTime1, Enabled = true });
                if (s.JournalReminderTime2.HasValue)
                    db.JournalReminders.Add(new JournalReminder { Time = s.JournalReminderTime2.Value, Enabled = true });
            }
            else
            {
                db.JournalReminders.Add(new JournalReminder { Time = new TimeOnly(6, 30), Enabled = true });
            }
            await db.SaveChangesAsync();
        }
    }

    private static async Task EnsureColumnAsync(LucidDbContext db, string table, string column, string definition)
    {
        var conn = db.Database.GetDbConnection();
        var wasClosed = conn.State != System.Data.ConnectionState.Open;
        if (wasClosed) await conn.OpenAsync();
        try
        {
            var cols = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            await using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = $"PRAGMA table_info({table});";
                await using var r = await cmd.ExecuteReaderAsync();
                while (await r.ReadAsync()) cols.Add(r.GetString(1));
            }
            if (!cols.Contains(column))
            {
                // table/column/definition are hardcoded constants from our own callers, never user input.
#pragma warning disable EF1002
                await db.Database.ExecuteSqlRawAsync($"ALTER TABLE {table} ADD COLUMN {column} {definition};");
#pragma warning restore EF1002
            }
        }
        finally
        {
            if (wasClosed) await conn.CloseAsync();
        }
    }

    private static void WireNotificationTapHandler(IServiceProvider services)
    {
        var router = services.GetRequiredService<NotificationRouter>();
        var journal = services.GetRequiredService<IDreamJournalService>();
        var logger = services.GetRequiredService<ILogger<NotificationRouter>>();

        LocalNotificationCenter.Current.NotificationActionTapped += async (e) =>
        {
            var data = e.Request?.ReturningData ?? "";
            logger.LogInformation("Notification tapped: {Data}", data);
            try
            {
                if (data == NotificationService.ActionJournalNew)
                {
                    var entry = await journal.CreateAsync();
                    router.Request($"/journal/edit/{entry.Id}?source=reminder");
                }
                else if (data == NotificationService.ActionDreamVoice)
                {
                    var notifications = services.GetRequiredService<INotificationService>();
                    await notifications.CancelDreamCaptureAsync();
                    var entry = await journal.CreateAsync();
                    router.Request($"/journal/edit/{entry.Id}?source=voice&mic=1");
                }
                else if (data == NotificationService.ActionRealityCheck)
                {
                    router.Request("/reality-check");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Notification tap handler failed");
            }
        };
    }
}
