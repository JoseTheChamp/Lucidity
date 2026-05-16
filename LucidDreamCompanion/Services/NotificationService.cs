using LucidDreamCompanion.Data.Entities;
using Microsoft.Extensions.Logging;
using Plugin.LocalNotification;
using Plugin.LocalNotification.Core.Models;
using Plugin.LocalNotification.Core.Models.AndroidOption;

namespace LucidDreamCompanion.Services;

public class NotificationService : INotificationService
{
    public const string ChannelJournal      = "journal_reminders";
    public const string ChannelRealityCheck = "reality_checks";
    public const string ChannelCuesActive   = "cues_active";

    // Stable ID ranges:
    //   1000..1099 reserved for journal reminders (use 1000 + reminder.Id, capped)
    //   2000..2199 reserved for reality-check pings of the day
    public const int JournalBaseId = 1000;
    public const int JournalMaxIds = 100;
    public const int RealityCheckBaseId = 2000;
    public const int RealityCheckMaxIds = 200;

    public const string ActionJournalNew    = "journal-new";
    public const string ActionRealityCheck  = "reality-check";
    public const string ActionDreamVoice    = "journal-voice";

    public const int DreamCaptureId = 3001;

    private readonly ILogger<NotificationService> _logger;
    private readonly IAppSettingsService _settings;
    private readonly IJournalReminderService _reminders;

    public NotificationService(
        ILogger<NotificationService> logger,
        IAppSettingsService settings,
        IJournalReminderService reminders)
    {
        _logger = logger;
        _settings = settings;
        _reminders = reminders;
    }

    public async Task<bool> RequestPermissionAsync()
    {
        try
        {
            var granted = await LocalNotificationCenter.Current.RequestNotificationPermission(
                new NotificationPermission { AskPermission = true });
            _logger.LogInformation("Notification permission granted={Granted}", granted);
            return granted;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to request notification permission");
            return false;
        }
    }

    public async Task<bool> AreNotificationsEnabledAsync()
    {
        try
        {
            return await LocalNotificationCenter.Current.AreNotificationsEnabled(
                new NotificationPermission { AskPermission = false });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AreNotificationsEnabled failed");
            return false;
        }
    }

    public Task<bool> CanScheduleExactAlarmsAsync()
    {
#if ANDROID
        try
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(31))
                return Task.FromResult(true);
            var ctx = Android.App.Application.Context;
            var am = (Android.App.AlarmManager?)ctx.GetSystemService(Android.Content.Context.AlarmService);
            return Task.FromResult(am is not null && am.CanScheduleExactAlarms());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CanScheduleExactAlarms check failed");
            return Task.FromResult(false);
        }
#else
        return Task.FromResult(true);
#endif
    }

    public Task CancelJournalRemindersAsync()
    {
        var ids = new int[JournalMaxIds];
        for (var i = 0; i < JournalMaxIds; i++) ids[i] = JournalBaseId + i;
        LocalNotificationCenter.Current.Cancel(ids);
        return Task.CompletedTask;
    }

    public async Task ScheduleJournalRemindersAsync(IEnumerable<JournalReminder> reminders)
    {
        await CancelJournalRemindersAsync();
        var i = 0;
        foreach (var r in reminders.Where(r => r.Enabled).Take(JournalMaxIds))
        {
            var next = NextOccurrence(r.Time);
            var request = new NotificationRequest
            {
                NotificationId = JournalBaseId + i,
                Title = i == 0 ? "Capture last night's dream" : "Another pass?",
                Subtitle = "Lucid Dream Companion",
                Description = "30 seconds of voice notes will do. Even fragments help.",
                ReturningData = ActionJournalNew,
                CategoryType = NotificationCategoryType.Reminder,
                Schedule = new NotificationRequestSchedule
                {
                    NotifyTime = next,
                    NotifyRepeatInterval = TimeSpan.FromDays(1),
                    RepeatType = NotificationRepeat.Daily,
                },
                Android = new AndroidOptions
                {
                    ChannelId = ChannelJournal,
                    Priority = AndroidPriority.Default,
                },
            };
            await LocalNotificationCenter.Current.Show(request);
            _logger.LogInformation("Scheduled journal reminder #{N} at {Time}", i, next);
            i++;
        }
    }

    public Task CancelRealityChecksAsync()
    {
        var ids = new int[RealityCheckMaxIds];
        for (var i = 0; i < RealityCheckMaxIds; i++) ids[i] = RealityCheckBaseId + i;
        LocalNotificationCenter.Current.Cancel(ids);
        return Task.CompletedTask;
    }

    public async Task ScheduleRealityCheckBatchAsync(DateOnly date, AppSettings settings)
    {
        await CancelRealityChecksAsync();
        if (!settings.RealityCheckEnabled) return;
        if (settings.RealityCheckEnd <= settings.RealityCheckStart) return;

        var startDt = date.ToDateTime(settings.RealityCheckStart);
        var endDt = date.ToDateTime(settings.RealityCheckEnd);
        var fireTimes = settings.RealityCheckMode switch
        {
            RealityCheckMode.FixedInterval => InvervalTimes(startDt, endDt, settings.RealityCheckIntervalMinutes),
            _                              => RandomTimes(startDt, endDt, settings.RealityCheckCountPerDay, date.DayNumber),
        };

        var i = 0;
        var now = DateTime.Now;
        foreach (var fireAt in fireTimes)
        {
            if (i >= RealityCheckMaxIds) break;
            if (fireAt <= now) continue;
            var request = new NotificationRequest
            {
                NotificationId = RealityCheckBaseId + i,
                Title = "Reality check",
                Description = "Are you dreaming? Look at your hands.",
                ReturningData = ActionRealityCheck,
                CategoryType = NotificationCategoryType.Alarm,
                Schedule = new NotificationRequestSchedule { NotifyTime = fireAt },
                Android = new AndroidOptions
                {
                    ChannelId = ChannelRealityCheck,
                    Priority = AndroidPriority.High,
                    VibrationPattern = new long[] { 0, 180, 80, 180 },
                },
            };
            await LocalNotificationCenter.Current.Show(request);
            i++;
        }
        _logger.LogInformation("Scheduled {N} reality checks ({Mode}) for {Date}", i, settings.RealityCheckMode, date);
    }

    public async Task RescheduleAllAsync()
    {
        var reminders = await _reminders.ListAsync();
        await ScheduleJournalRemindersAsync(reminders);

        var settings = await _settings.GetAsync();
        await ScheduleRealityCheckBatchAsync(DateOnly.FromDateTime(DateTime.Now), settings);
    }

    public async Task FireTestAsync(string title, string body, string? action = null, string channelId = ChannelJournal)
    {
        var req = new NotificationRequest
        {
            NotificationId = 9999,
            Title = title,
            Description = body,
            ReturningData = action ?? "",
            Schedule = new NotificationRequestSchedule
            {
                NotifyTime = DateTime.Now.AddSeconds(3),
            },
            Android = new AndroidOptions { ChannelId = channelId },
        };
        await LocalNotificationCenter.Current.Show(req);
    }

    private static IEnumerable<DateTime> RandomTimes(DateTime startDt, DateTime endDt, int count, int seed)
    {
        if (count <= 0) yield break;
        var totalSec = (int)(endDt - startDt).TotalSeconds;
        if (totalSec <= 0) yield break;
        var slotSec = totalSec / count;
        if (slotSec <= 0) yield break;
        var rng = new Random(seed);
        for (var i = 0; i < count; i++)
        {
            var slotStart = startDt.AddSeconds(slotSec * i);
            yield return slotStart.AddSeconds(rng.Next(0, slotSec));
        }
    }

    private static IEnumerable<DateTime> InvervalTimes(DateTime startDt, DateTime endDt, int minutes)
    {
        if (minutes <= 0) yield break;
        var step = TimeSpan.FromMinutes(minutes);
        for (var t = startDt; t <= endDt; t = t.Add(step))
        {
            yield return t;
        }
    }

    public async Task ShowDreamCaptureAsync()
    {
        var req = new NotificationRequest
        {
            NotificationId = DreamCaptureId,
            Title = "Log your dream",
            Subtitle = "Lucid Dream Companion",
            Description = "Tap to capture it before it fades — the mic opens ready.",
            ReturningData = ActionDreamVoice,
            CategoryType = NotificationCategoryType.Reminder,
            Android = new AndroidOptions
            {
                ChannelId = ChannelJournal,
                Priority = AndroidPriority.Low,
            },
        };
        await LocalNotificationCenter.Current.Show(req);
        _logger.LogInformation("Shown dream-capture notification");
    }

    public Task CancelDreamCaptureAsync()
    {
        LocalNotificationCenter.Current.Cancel(DreamCaptureId, string.Empty);
        return Task.CompletedTask;
    }

    private static DateTime NextOccurrence(TimeOnly time)
    {
        var now = DateTime.Now;
        var today = now.Date.Add(time.ToTimeSpan());
        return today > now ? today : today.AddDays(1);
    }
}
