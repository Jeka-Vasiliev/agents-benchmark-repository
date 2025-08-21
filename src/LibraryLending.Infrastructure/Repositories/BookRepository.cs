using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryLending.Infrastructure.Repositories;

public class BookRepository : IBookRepository
{
    private readonly LibraryDbContext _context;

    public BookRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<Book?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Books.FindAsync([id], cancellationToken);
    }

    public async Task<IEnumerable<Book>> GetAllAsync(int page, int pageSize, string? query = null, CancellationToken cancellationToken = default)
    {
        var queryable = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var searchTerm = query.Trim().ToLower();
            queryable = queryable.Where(b => 
                b.Title.ToLower().Contains(searchTerm) ||
                b.Author.ToLower().Contains(searchTerm) ||
                b.Isbn.Value.ToLower().Contains(searchTerm));
        }

        return await queryable
            .OrderBy(b => b.Title)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> GetTotalCountAsync(string? query = null, CancellationToken cancellationToken = default)
    {
        var queryable = _context.Books.AsQueryable();

        if (!string.IsNullOrWhiteSpace(query))
        {
            var searchTerm = query.Trim().ToLower();
            queryable = queryable.Where(b => 
                b.Title.ToLower().Contains(searchTerm) ||
                b.Author.ToLower().Contains(searchTerm) ||
                b.Isbn.Value.ToLower().Contains(searchTerm));
        }

        return await queryable.CountAsync(cancellationToken);
    }

    public async Task AddAsync(Book book, CancellationToken cancellationToken = default)
    {
        _context.Books.Add(book);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(Book book, CancellationToken cancellationToken = default)
    {
        _context.Books.Update(book);
        await _context.SaveChangesAsync(cancellationToken);
    }
}