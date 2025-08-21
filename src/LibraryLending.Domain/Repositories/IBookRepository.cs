using LibraryLending.Domain.Entities;

namespace LibraryLending.Domain.Repositories;

public interface IBookRepository
{
    Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<Book>> GetAllAsync(int page, int pageSize, string? query = null, CancellationToken cancellationToken = default);
    Task<int> GetTotalCountAsync(string? query = null, CancellationToken cancellationToken = default);
    Task AddAsync(Book book, CancellationToken cancellationToken = default);
    Task UpdateAsync(Book book, CancellationToken cancellationToken = default);
}