using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.ValueObjects;
using LibraryLending.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace LibraryLending.Infrastructure.Repositories;

public class PatronRepository : IPatronRepository
{
    private readonly LibraryDbContext _context;

    public PatronRepository(LibraryDbContext context)
    {
        _context = context;
    }

    public async Task<Patron?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _context.Patrons.FindAsync([id], cancellationToken);
    }

    public async Task<Patron?> GetByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Patrons
            .FirstOrDefaultAsync(p => p.Email == email, cancellationToken);
    }

    public async Task<bool> ExistsByEmailAsync(Email email, CancellationToken cancellationToken = default)
    {
        return await _context.Patrons
            .AnyAsync(p => p.Email == email, cancellationToken);
    }

    public async Task AddAsync(Patron patron, CancellationToken cancellationToken = default)
    {
        _context.Patrons.Add(patron);
        await _context.SaveChangesAsync(cancellationToken);
    }
}