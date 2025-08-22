using LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;
using Microsoft.AspNetCore.Mvc;

namespace LibraryLending.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly ProcessOverdueNotificationsCommandHandler _handler;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        ProcessOverdueNotificationsCommandHandler handler,
        ILogger<NotificationsController> logger)
    {
        _handler = handler;
        _logger = logger;
    }

    /// <summary>
    /// Manually trigger processing of overdue notifications
    /// </summary>
    [HttpPost("process-overdue")]
    public async Task<IActionResult> ProcessOverdueNotifications(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Manual processing of overdue notifications requested");
            
            var result = await _handler.HandleAsync(new ProcessOverdueNotificationsCommand(), cancellationToken);
            
            return Ok(new
            {
                message = "Overdue notifications processed successfully",
                processedCount = result.ProcessedCount,
                sentCount = result.SentCount,
                failedCount = result.FailedCount
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error occurred while processing overdue notifications");
            return StatusCode(500, new { message = "Internal server error occurred" });
        }
    }
}
