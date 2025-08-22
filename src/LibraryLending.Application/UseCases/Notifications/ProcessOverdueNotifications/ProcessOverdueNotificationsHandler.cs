using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;

namespace LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;

public class ProcessOverdueNotificationsHandler : IRequestHandler<ProcessOverdueNotificationsCommand, ProcessOverdueNotificationsResult>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IOverdueNotificationRepository _notificationRepository;
    private readonly IEmailService _emailService;
    private readonly ILogger<ProcessOverdueNotificationsHandler> _logger;

    public ProcessOverdueNotificationsHandler(
        ILoanRepository loanRepository,
        IOverdueNotificationRepository notificationRepository,
        IEmailService emailService,
        ILogger<ProcessOverdueNotificationsHandler> logger)
    {
        _loanRepository = loanRepository;
        _notificationRepository = notificationRepository;
        _emailService = emailService;
        _logger = logger;
    }

    public async Task<ProcessOverdueNotificationsResult> Handle(ProcessOverdueNotificationsCommand request, CancellationToken cancellationToken)
    {
        var result = new ProcessOverdueNotificationsResult();

        try
        {
            // 1. Обработка новых просроченных займов
            await ProcessNewOverdueLoans(result, cancellationToken);

            // 2. Обработка pending уведомлений
            await ProcessPendingNotifications(result, cancellationToken);

            // 3. Обработка failed уведомлений для повторной отправки
            await ProcessFailedNotifications(result, cancellationToken);

            _logger.LogInformation("Processed overdue notifications: {NewNotifications} new, {Sent} sent, {Failed} failed, {Retries} retries",
                result.NewNotificationsCreated, result.NotificationsSent, result.NotificationsFailed, result.RetriesProcessed);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing overdue notifications");
            throw;
        }
    }

    private async Task ProcessNewOverdueLoans(ProcessOverdueNotificationsResult result, CancellationToken cancellationToken)
    {
        var overdueLoans = await _loanRepository.GetOverdueLoansAsync(cancellationToken);
        result.TotalOverdueLoans = overdueLoans.Count();

        foreach (var loan in overdueLoans)
        {
            // Проверяем, нет ли уже уведомления для этого займа
            var existingNotification = await _notificationRepository.ExistsForLoanAsync(loan.Id, cancellationToken);
            if (!existingNotification)
            {
                var notification = new OverdueNotification(loan.Id, loan.PatronId);
                await _notificationRepository.AddAsync(notification, cancellationToken);
                result.NewNotificationsCreated++;
                
                _logger.LogDebug("Created notification for overdue loan {LoanId}", loan.Id);
            }
        }
    }

    private async Task ProcessPendingNotifications(ProcessOverdueNotificationsResult result, CancellationToken cancellationToken)
    {
        var pendingNotifications = await _notificationRepository.GetPendingNotificationsAsync(cancellationToken);

        foreach (var notification in pendingNotifications)
        {
            await SendNotification(notification, result, cancellationToken);
        }
    }

    private async Task ProcessFailedNotifications(ProcessOverdueNotificationsResult result, CancellationToken cancellationToken)
    {
        var failedNotifications = await _notificationRepository.GetFailedNotificationsForRetryAsync(cancellationToken);

        foreach (var notification in failedNotifications)
        {
            if (notification.ShouldRetry())
            {
                notification.ResetForRetry();
                await SendNotification(notification, result, cancellationToken);
                result.RetriesProcessed++;
            }
        }
    }

    private async Task SendNotification(OverdueNotification notification, ProcessOverdueNotificationsResult result, CancellationToken cancellationToken)
    {
        try
        {
            var success = await _emailService.SendOverdueNotificationAsync(
                notification.Patron.Email,
                notification.Patron.FullName,
                notification.Loan.Book.Title,
                notification.Loan.DueAt,
                cancellationToken);

            if (success)
            {
                notification.MarkAsSent();
                result.NotificationsSent++;
                _logger.LogDebug("Successfully sent notification {NotificationId} for loan {LoanId}", 
                    notification.Id, notification.LoanId);
            }
            else
            {
                notification.MarkAsFailed("Email service returned false");
                result.NotificationsFailed++;
                _logger.LogWarning("Failed to send notification {NotificationId} for loan {LoanId}: Email service returned false", 
                    notification.Id, notification.LoanId);
            }

            await _notificationRepository.UpdateAsync(notification, cancellationToken);
        }
        catch (Exception ex)
        {
            notification.MarkAsFailed(ex.Message);
            result.NotificationsFailed++;
            await _notificationRepository.UpdateAsync(notification, cancellationToken);
            
            _logger.LogError(ex, "Error sending notification {NotificationId} for loan {LoanId}", 
                notification.Id, notification.LoanId);
        }
    }
}
