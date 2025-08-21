using LibraryLending.Domain.Entities;
using LibraryLending.Domain.ValueObjects;

namespace LibraryLending.Domain.Repositories;

public interface IPatronRepository
{
    Task<Patron?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Patron?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default);
    Task AddAsync(Patron patron, CancellationToken cancellationToken = default);
}