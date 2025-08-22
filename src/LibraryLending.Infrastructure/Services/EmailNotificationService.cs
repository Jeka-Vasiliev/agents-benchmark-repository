using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Services;
using Microsoft.Extensions.Logging;

namespace LibraryLending.Infrastructure.Services;

public class EmailNotificationService : INotificationService
{
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(ILogger<EmailNotificationService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendOverdueNotificationAsync(OverdueNotification notification, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Sending overdue notification to {Email} for book {BookTitle}", 
                notification.Patron.Email.Value, notification.Loan.Book.Title);

            // Simulate email sending with potential failure
            await SimulateEmailSendingAsync(cancellationToken);

            _logger.LogInformation("Successfully sent overdue notification to {Email}", 
                notification.Patron.Email.Value);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send overdue notification to {Email}", 
                notification.Patron.Email.Value);
            
            return false;
        }
    }

    private async Task SimulateEmailSendingAsync(CancellationToken cancellationToken)
    {
        // Simulate network delay
        await Task.Delay(100, cancellationToken);

        // Simulate occasional failures (10% failure rate)
        var random = new Random();
        if (random.Next(100) < 10)
        {
            throw new InvalidOperationException("Simulated email service failure");
        }
    }
}
