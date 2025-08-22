using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.Services;
using Microsoft.Extensions.Logging;

namespace LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;

public class ProcessOverdueNotificationsCommandHandler
{
    private readonly ILoanRepository _loanRepository;
    private readonly IOverdueNotificationRepository _notificationRepository;
    private readonly INotificationService _notificationService;
    private readonly ILogger<ProcessOverdueNotificationsCommandHandler> _logger;

    public ProcessOverdueNotificationsCommandHandler(
        ILoanRepository loanRepository,
        IOverdueNotificationRepository notificationRepository,
        INotificationService notificationService,
        ILogger<ProcessOverdueNotificationsCommandHandler> logger)
    {
        _loanRepository = loanRepository;
        _notificationRepository = notificationRepository;
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task<ProcessOverdueNotificationsResult> HandleAsync(
        ProcessOverdueNotificationsCommand command, 
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting overdue notifications processing");

        var processedCount = 0;
        var sentCount = 0;
        var failedCount = 0;

        try
        {
            // Step 1: Create notifications for new overdue loans
            await CreateNotificationsForOverdueLoansAsync(cancellationToken);

            // Step 2: Process pending notifications
            var pendingNotifications = await _notificationRepository.GetPendingNotificationsAsync(cancellationToken);
            
            foreach (var notification in pendingNotifications)
            {
                processedCount++;
                var success = await ProcessNotificationAsync(notification, cancellationToken);
                
                if (success)
                    sentCount++;
                else
                    failedCount++;
            }

            // Step 3: Retry failed notifications
            var failedNotifications = await _notificationRepository.GetFailedNotificationsForRetryAsync(cancellationToken);
            
            foreach (var notification in failedNotifications)
            {
                processedCount++;
                notification.ResetForRetry();
                
                var success = await ProcessNotificationAsync(notification, cancellationToken);
                
                if (success)
                    sentCount++;
                else
                    failedCount++;
            }

            _logger.LogInformation(
                "Completed overdue notifications processing. Processed: {ProcessedCount}, Sent: {SentCount}, Failed: {FailedCount}",
                processedCount, sentCount, failedCount);

            return new ProcessOverdueNotificationsResult(processedCount, sentCount, failedCount);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing overdue notifications");
            throw;
        }
    }

    private async Task CreateNotificationsForOverdueLoansAsync(CancellationToken cancellationToken)
    {
        // Get all overdue loans that don't have notifications yet
        var overdueLoans = await _loanRepository.GetOverdueLoansAsync(cancellationToken);
        
        foreach (var loan in overdueLoans)
        {
            var notificationExists = await _notificationRepository.ExistsForLoanAsync(loan.Id, cancellationToken);
            
            if (!notificationExists)
            {
                var notification = new OverdueNotification(loan.Id, loan.PatronId);
                await _notificationRepository.AddAsync(notification, cancellationToken);
                
                _logger.LogInformation("Created notification for overdue loan {LoanId}", loan.Id);
            }
        }
    }

    private async Task<bool> ProcessNotificationAsync(OverdueNotification notification, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _notificationService.SendOverdueNotificationAsync(notification, cancellationToken);
            
            if (success)
            {
                notification.MarkAsSent();
                _logger.LogInformation("Successfully sent notification {NotificationId} for loan {LoanId}", 
                    notification.Id, notification.LoanId);
            }
            else
            {
                notification.MarkAsFailed("Failed to send notification");
                _logger.LogWarning("Failed to send notification {NotificationId} for loan {LoanId}", 
                    notification.Id, notification.LoanId);
            }
            
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
            return success;
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed($"Exception occurred: {ex.Message}");
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
            
            _logger.LogError(ex, "Exception occurred while sending notification {NotificationId} for loan {LoanId}", 
                notification.Id, notification.LoanId);
            
            return false;
        }
    }
}
