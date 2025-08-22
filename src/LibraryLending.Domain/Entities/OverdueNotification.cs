using LibraryLending.Domain.ValueObjects;

namespace LibraryLending.Domain.Entities;

public class OverdueNotification
{
    public Guid Id { get; private set; }
    public Guid LoanId { get; private set; }
    public Guid PatronId { get; private set; }
    public NotificationStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? SentAt { get; private set; }
    public int RetryCount { get; private set; }
    public string? ErrorMessage { get; private set; }

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
        Status = NotificationStatus.Pending;
        CreatedAt = DateTime.UtcNow;
        RetryCount = 0;
    }

    public void MarkAsSent()
    {
        Status = NotificationStatus.Sent;
        SentAt = DateTime.UtcNow;
        ErrorMessage = null;
    }

    public void MarkAsFailed(string errorMessage)
    {
        Status = NotificationStatus.Failed;
        ErrorMessage = errorMessage;
        RetryCount++;
    }

    public void ResetForRetry()
    {
        Status = NotificationStatus.Pending;
        ErrorMessage = null;
    }

    public bool CanRetry => Status == NotificationStatus.Failed && RetryCount < 5;
}
