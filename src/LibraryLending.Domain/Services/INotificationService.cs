using LibraryLending.Domain.Entities;

namespace LibraryLending.Domain.Services;

public interface INotificationService
{
    Task<bool> SendOverdueNotificationAsync(OverdueNotification notification, CancellationToken cancellationToken = default);
}
