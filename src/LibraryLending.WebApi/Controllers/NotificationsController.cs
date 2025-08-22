using LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LibraryLending.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IMediator _mediator;

    public NotificationsController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Запускает процесс обработки уведомлений о просроченных книгах
    /// </summary>
    /// <returns>Результат обработки уведомлений</returns>
    [HttpPost("process-overdue")]
    public async Task<ActionResult<ProcessOverdueNotificationsResult>> ProcessOverdueNotifications()
    {
        var command = new ProcessOverdueNotificationsCommand();
        var result = await _mediator.Send(command);
        return Ok(result);
    }
}
