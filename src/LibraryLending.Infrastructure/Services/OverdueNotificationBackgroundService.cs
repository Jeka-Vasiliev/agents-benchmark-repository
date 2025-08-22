using LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LibraryLending.Infrastructure.Services;

public class OverdueNotificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OverdueNotificationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1); // Check every hour

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
                var handler = scope.ServiceProvider.GetRequiredService<ProcessOverdueNotificationsCommandHandler>();

                var result = await handler.HandleAsync(new ProcessOverdueNotificationsCommand(), stoppingToken);

                _logger.LogInformation(
                    "Overdue notifications processed. Processed: {ProcessedCount}, Sent: {SentCount}, Failed: {FailedCount}",
                    result.ProcessedCount, result.SentCount, result.FailedCount);

                await Task.Delay(_checkInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Overdue notification background service is stopping");
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred in overdue notification background service");
                
                // Wait a shorter time before retrying on error
                await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
            }
        }
    }
}
