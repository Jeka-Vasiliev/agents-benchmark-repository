using LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibraryLending.Application.Services;

public class OverdueNotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverdueNotificationBackgroundService> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1); // Проверяем каждый час

    public OverdueNotificationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<OverdueNotificationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Overdue notification background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _serviceProvider.CreateScope();
                var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

                var command = new ProcessOverdueNotificationsCommand();
                var result = await mediator.Send(command, stoppingToken);

                if (result.NewNotificationsCreated > 0 || result.NotificationsSent > 0 || result.RetriesProcessed > 0)
                {
                    _logger.LogInformation("Processed overdue notifications: {NewNotifications} new, {Sent} sent, {Retries} retries", 
                        result.NewNotificationsCreated, result.NotificationsSent, result.RetriesProcessed);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in overdue notification background service");
            }

            await Task.Delay(_interval, stoppingToken);
        }

        _logger.LogInformation("Overdue notification background service stopped");
    }
}
