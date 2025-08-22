using LibraryLending.Domain.Entities;
using LibraryLending.Domain.ValueObjects;

namespace LibraryLending.Domain.Repositories;

public interface IOverdueNotificationRepository
{
    Task<OverdueNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<OverdueNotification?> GetByLoanIdAsync(Guid loanId, CancellationToken cancellationToken = default);
    Task<IEnumerable<OverdueNotification>> GetPendingNotificationsAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<OverdueNotification>> GetFailedNotificationsForRetryAsync(CancellationToken cancellationToken = default);
    Task AddAsync(OverdueNotification notification, CancellationToken cancellationToken = default);
    Task UpdateAsync(OverdueNotification notification, CancellationToken cancellationToken = default);
    Task<bool> ExistsForLoanAsync(Guid loanId, CancellationToken cancellationToken = default);
}
