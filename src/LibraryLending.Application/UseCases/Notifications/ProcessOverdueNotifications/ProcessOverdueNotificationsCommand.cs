namespace LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;

public record ProcessOverdueNotificationsCommand;

public record ProcessOverdueNotificationsResult(
    int ProcessedCount,
    int SentCount,
    int FailedCount
);
