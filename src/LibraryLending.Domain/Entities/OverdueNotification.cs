using LibraryLending.Domain.Exceptions;

namespace LibraryLending.Domain.Entities;

public class OverdueNotification
{
    public Guid Id { get; private set; }
    public Guid LoanId { get; private set; }
    public Guid PatronId { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public OverdueNotificationStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }
    public int RetryCount { get; private set; }
    public DateTime? NextRetryAt { get; private set; }

    // Navigation properties
    public Loan Loan { get; private set; } = null!;
    public Patron Patron { get; private set; } = null!;

    // EF Core constructor
    private OverdueNotification() { }

    public OverdueNotification(Guid loanId, Guid patronId)
    {
        Id = Guid.NewGuid();
        LoanId = loanId;
        PatronId = patronId;
        CreatedAt = DateTime.UtcNow;
        Status = OverdueNotificationStatus.Pending;
        RetryCount = 0;
    }

    public void MarkAsSent()
    {
        if (Status == OverdueNotificationStatus.Sent)
            throw new NotificationException("Notification has already been sent.");

        Status = OverdueNotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
        NextRetryAt = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = OverdueNotificationStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
        
        // Exponential backoff: retry after 1, 2, 4, 8, 16 minutes, then every hour
        var delayMinutes = RetryCount <= 5 ? Math.Pow(2, RetryCount - 1) : 60;
        NextRetryAt = DateTime.UtcNow.AddMinutes(delayMinutes);
    }

    public bool ShouldRetry()
    {
        return Status == OverdueNotificationStatus.Failed 
               && NextRetryAt.HasValue 
               && DateTime.UtcNow >= NextRetryAt.Value
               && RetryCount < 10; // Max 10 retries
    }

    public void ResetForRetry()
    {
        if (!ShouldRetry())
            throw new NotificationException("Notification is not eligible for retry.");

        Status = OverdueNotificationStatus.Pending;
        ErrorMessage = null;
        NextRetryAt = null;
    }
}

public enum OverdueNotificationStatus
{
    Pending = 0,
    Sent = 1,
    Failed = 2
}
