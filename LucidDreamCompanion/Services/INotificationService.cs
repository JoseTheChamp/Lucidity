using LucidDreamCompanion.Data.Entities;

namespace LucidDreamCompanion.Services;

public interface INotificationService
{
    Task<bool> RequestPermissionAsync();
    Task<bool> AreNotificationsEnabledAsync();
    Task<bool> CanScheduleExactAlarmsAsync();

    Task CancelJournalRemindersAsync();
    Task ScheduleJournalRemindersAsync(IEnumerable<JournalReminder> reminders);

    Task CancelRealityChecksAsync();
    Task ScheduleRealityCheckBatchAsync(DateOnly date, AppSettings settings);

    Task RescheduleAllAsync();

    Task FireTestAsync(string title, string body, string? action = null, string channelId = NotificationService.ChannelJournal);

    /// <summary>Lock-screen "log your dream" prompt, shown after a sleep session ends.</summary>
    Task ShowDreamCaptureAsync();
    Task CancelDreamCaptureAsync();
}
