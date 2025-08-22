namespace LibraryLending.Application.DTOs;

public class OverdueNotificationDto
{
    public Guid Id { get; set; }
    public Guid LoanId { get; set; }
    public Guid PatronId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? SentAt { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }
    public int RetryCount { get; set; }
    public DateTime? NextRetryAt { get; set; }
}
