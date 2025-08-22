using LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.Services;
using LibraryLending.Domain.ValueObjects;
using LibraryLending.Infrastructure.Data;
using LibraryLending.Infrastructure.Repositories;
using LibraryLending.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraryLending.Application.Tests.Integration;

public class OverdueNotificationEndToEndTests : IDisposable
{
    private readonly LibraryDbContext _context;
    private readonly ILoanRepository _loanRepository;
    private readonly IOverdueNotificationRepository _notificationRepository;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly ProcessOverdueNotificationsCommandHandler _handler;

    public OverdueNotificationEndToEndTests()
    {
        var options = new DbContextOptionsBuilder<LibraryDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new LibraryDbContext(options);
        _loanRepository = new LoanRepository(_context);
        _notificationRepository = new OverdueNotificationRepository(_context);
        _notificationServiceMock = new Mock<INotificationService>();

        var loggerMock = new Mock<ILogger<ProcessOverdueNotificationsCommandHandler>>();
        _handler = new ProcessOverdueNotificationsCommandHandler(
            _loanRepository,
            _notificationRepository,
            _notificationServiceMock.Object,
            loggerMock.Object);
    }

    [Fact]
    public async Task OverdueNotificationWorkflow_CompleteScenario_WorksCorrectly()
    {
        // Arrange: Create test data
        var patron = new Patron("Test User", Email.Create("test@example.com"));
        var book = new Book(Isbn.Create("9780134685991"), "Test Book", "Test Author", 1);
        
        await _context.Patrons.AddAsync(patron);
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();

        // Create an overdue loan (20 days ago, overdue by 6 days since loan period is 14 days)
        var overdueLoan = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-20));
        await _context.Loans.AddAsync(overdueLoan);
        await _context.SaveChangesAsync();

        // Mock successful notification sending
        _notificationServiceMock
            .Setup(s => s.SendOverdueNotificationAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act: Process overdue notifications
        var result = await _handler.HandleAsync(new ProcessOverdueNotificationsCommand());

        // Assert: Verify the workflow
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.SentCount);
        Assert.Equal(0, result.FailedCount);

        // Verify notification was created and marked as sent
        var notification = await _notificationRepository.GetByLoanIdAsync(overdueLoan.Id);
        Assert.NotNull(notification);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.NotNull(notification.SentAt);

        // Verify no duplicate notifications are created on subsequent runs
        var secondResult = await _handler.HandleAsync(new ProcessOverdueNotificationsCommand());
        Assert.Equal(0, secondResult.ProcessedCount);
        Assert.Equal(0, secondResult.SentCount);
        Assert.Equal(0, secondResult.FailedCount);
    }

    [Fact]
    public async Task OverdueNotificationWorkflow_WithFailureAndRetry_WorksCorrectly()
    {
        // Arrange: Create test data
        var patron = new Patron("Retry User", Email.Create("retry@example.com"));
        var book = new Book(Isbn.Create("9780135166307"), "Retry Book", "Retry Author", 1);
        
        await _context.Patrons.AddAsync(patron);
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();

        var overdueLoan = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-25));
        await _context.Loans.AddAsync(overdueLoan);
        await _context.SaveChangesAsync();

        // Mock initial failure then success
        _notificationServiceMock.SetupSequence(s => s.SendOverdueNotificationAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false) // First attempt fails
            .ReturnsAsync(true); // Second attempt succeeds

        // Act: First processing attempt (should fail)
        var firstResult = await _handler.HandleAsync(new ProcessOverdueNotificationsCommand());

        // Assert: First attempt results
        Assert.Equal(1, firstResult.ProcessedCount);
        Assert.Equal(0, firstResult.SentCount);
        Assert.Equal(1, firstResult.FailedCount);

        var notification = await _notificationRepository.GetByLoanIdAsync(overdueLoan.Id);
        Assert.NotNull(notification);
        Assert.Equal(NotificationStatus.Failed, notification.Status);
        Assert.Equal(1, notification.RetryCount);

        // Act: Second processing attempt (should retry and succeed)
        var secondResult = await _handler.HandleAsync(new ProcessOverdueNotificationsCommand());

        // Assert: Second attempt results
        Assert.Equal(1, secondResult.ProcessedCount);
        Assert.Equal(1, secondResult.SentCount);
        Assert.Equal(0, secondResult.FailedCount);

        // Refresh notification from database
        notification = await _notificationRepository.GetByLoanIdAsync(overdueLoan.Id);
        Assert.NotNull(notification);
        Assert.Equal(NotificationStatus.Sent, notification.Status);
        Assert.NotNull(notification.SentAt);
    }

    [Fact]
    public async Task OverdueNotificationWorkflow_WithReturnedBook_DoesNotCreateNotification()
    {
        // Arrange: Create test data with returned book
        var patron = new Patron("Returned User", Email.Create("returned@example.com"));
        var book = new Book(Isbn.Create("9781234567890"), "Returned Book", "Returned Author", 1);
        
        await _context.Patrons.AddAsync(patron);
        await _context.Books.AddAsync(book);
        await _context.SaveChangesAsync();

        var loan = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-20));
        loan.Return(DateTime.UtcNow.AddDays(-1)); // Book was returned yesterday
        
        await _context.Loans.AddAsync(loan);
        await _context.SaveChangesAsync();

        // Act: Process overdue notifications
        var result = await _handler.HandleAsync(new ProcessOverdueNotificationsCommand());

        // Assert: No notifications should be processed for returned books
        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(0, result.SentCount);
        Assert.Equal(0, result.FailedCount);

        var notification = await _notificationRepository.GetByLoanIdAsync(loan.Id);
        Assert.Null(notification);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}
