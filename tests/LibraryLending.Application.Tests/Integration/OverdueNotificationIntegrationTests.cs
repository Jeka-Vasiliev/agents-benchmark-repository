using LibraryLending.Domain.Entities;
using LibraryLending.Domain.ValueObjects;
using LibraryLending.Infrastructure.Data;
using LibraryLending.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace LibraryLending.Application.Tests.Integration;

public class OverdueNotificationIntegrationTests : IDisposable
{
    private readonly LibraryDbContext _context;
    private readonly OverdueNotificationRepository _repository;

    public OverdueNotificationIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LibraryDbContext(options);
        _repository = new OverdueNotificationRepository(_context);
    }

    [Fact]
    public async Task CreateAndRetrieveOverdueNotification_WorksCorrectly()
    {
        // Arrange
        var patron = new Patron("Jane Doe", Email.Create("jane@example.com"));
        var book = new Book(Isbn.Create("9780134685991"), "Test Book", "Test Author", 1);
        
        await _context.Patrons.AddAsync(patron);
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();

        var loan = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-20));
        await _context.Loans.AddAsync(loan);
        await _context.SaveChangesAsync();

        var notification = new OverdueNotification(loan.Id, patron.Id);

        // Act
        await _repository.AddAsync(notification);
        var retrievedNotification = await _repository.GetByLoanIdAsync(loan.Id);

        // Assert
        Assert.NotNull(retrievedNotification);
        Assert.Equal(notification.Id, retrievedNotification.Id);
        Assert.Equal(loan.Id, retrievedNotification.LoanId);
        Assert.Equal(patron.Id, retrievedNotification.PatronId);
        Assert.Equal(NotificationStatus.Pending, retrievedNotification.Status);
    }

    [Fact]
    public async Task GetPendingNotifications_ReturnsOnlyPendingNotifications()
    {
        // Arrange
        var patron = new Patron("John Smith", Email.Create("john@example.com"));
        var book = new Book(Isbn.Create("9780135166307"), "Another Book", "Another Author", 1);
        
        await _context.Patrons.AddAsync(patron);
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();

        var loan1 = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-15));
        var loan2 = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-10));
        
        _context.Loans.AddRange(loan1, loan2);
        await _context.SaveChangesAsync();

        var pendingNotification = new OverdueNotification(loan1.Id, patron.Id);
        var sentNotification = new OverdueNotification(loan2.Id, patron.Id);
        sentNotification.MarkAsSent();

        await _repository.AddAsync(pendingNotification);
        await _repository.AddAsync(sentNotification);

        // Act
        var pendingNotifications = await _repository.GetPendingNotificationsAsync();

        // Assert
        Assert.Single(pendingNotifications);
        Assert.Equal(pendingNotification.Id, pendingNotifications.First().Id);
    }

    [Fact]
    public async Task ExistsForLoan_DetectsDuplicateNotifications()
    {
        // Arrange
        var patron = new Patron("Alice Johnson", Email.Create("alice@example.com"));
        var book = new Book(Isbn.Create("9781234567890"), "Unique Book", "Unique Author", 1);
        
        await _context.Patrons.AddAsync(patron);
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();

        var loan = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-25));
        await _context.Loans.AddAsync(loan);
        await _context.SaveChangesAsync();

        var notification = new OverdueNotification(loan.Id, patron.Id);
        await _repository.AddAsync(notification);

        // Act
        var exists = await _repository.ExistsForLoanAsync(loan.Id);
        var notExists = await _repository.ExistsForLoanAsync(Guid.NewGuid());

        // Assert
        Assert.True(exists);
        Assert.False(notExists);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
