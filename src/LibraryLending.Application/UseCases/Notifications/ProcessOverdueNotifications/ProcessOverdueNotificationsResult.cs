namespace LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;

public class ProcessOverdueNotificationsResult
{
    public int TotalOverdueLoans { get; set; }
    public int NewNotificationsCreated { get; set; }
    public int NotificationsSent { get; set; }
    public int NotificationsFailed { get; set; }
    public int RetriesProcessed { get; set; }
}
