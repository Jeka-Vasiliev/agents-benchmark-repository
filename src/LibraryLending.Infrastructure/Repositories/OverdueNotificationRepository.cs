using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryLending.Infrastructure.Repositories;

public class OverdueNotificationRepository : IOverdueNotificationRepository
{
    private readonly LibraryDbContext _context;

    public OverdueNotificationRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<OverdueNotification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.OverdueNotifications
            .Include(n => n.Loan)
                .ThenInclude(l => l.Book)
            .Include(n => n.Patron)
            .FirstOrDefaultAsync(n => n.Id == id, cancellationToken);
    }

    public async Task<OverdueNotification?> GetByLoanIdAsync(Guid loanId, CancellationToken cancellationToken = default)
    {
        return await _context.OverdueNotifications
            .Include(n => n.Loan)
                .ThenInclude(l => l.Book)
            .Include(n => n.Patron)
            .FirstOrDefaultAsync(n => n.LoanId == loanId, cancellationToken);
    }

    public async Task<IEnumerable<OverdueNotification>> GetPendingNotificationsAsync(CancellationToken cancellationToken = default)
    {
        return await _context.OverdueNotifications
            .Include(n => n.Loan)
                .ThenInclude(l => l.Book)
            .Include(n => n.Patron)
            .Where(n => n.Status == OverdueNotificationStatus.Pending)
            .OrderBy(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<IEnumerable<OverdueNotification>> GetFailedNotificationsForRetryAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        return await _context.OverdueNotifications
            .Include(n => n.Loan)
                .ThenInclude(l => l.Book)
            .Include(n => n.Patron)
            .Where(n => n.Status == OverdueNotificationStatus.Failed 
                       && n.NextRetryAt.HasValue 
                       && n.NextRetryAt.Value <= now
                       && n.RetryCount < 10)
            .OrderBy(n => n.NextRetryAt)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(OverdueNotification notification, CancellationToken cancellationToken = default)
    {
        _context.OverdueNotifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(OverdueNotification notification, CancellationToken cancellationToken = default)
    {
        _context.OverdueNotifications.Update(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> ExistsForLoanAsync(Guid loanId, CancellationToken cancellationToken = default)
    {
        return await _context.OverdueNotifications
            .AnyAsync(n => n.LoanId == loanId, cancellationToken);
    }
}
