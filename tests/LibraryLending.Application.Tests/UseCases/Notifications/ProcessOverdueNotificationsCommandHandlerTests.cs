using LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.Services;
using LibraryLending.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraryLending.Application.Tests.UseCases.Notifications;

public class ProcessOverdueNotificationsCommandHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepositoryMock;
    private readonly Mock<IOverdueNotificationRepository> _notificationRepositoryMock;
    private readonly Mock<INotificationService> _notificationServiceMock;
    private readonly Mock<ILogger<ProcessOverdueNotificationsCommandHandler>> _loggerMock;
    private readonly ProcessOverdueNotificationsCommandHandler _handler;

    public ProcessOverdueNotificationsCommandHandlerTests()
    {
        _loanRepositoryMock = new Mock<ILoanRepository>();
        _notificationRepositoryMock = new Mock<IOverdueNotificationRepository>();
        _notificationServiceMock = new Mock<INotificationService>();
        _loggerMock = new Mock<ILogger<ProcessOverdueNotificationsCommandHandler>>();

        _handler = new ProcessOverdueNotificationsCommandHandler(
            _loanRepositoryMock.Object,
            _notificationRepositoryMock.Object,
            _notificationServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task HandleAsync_WithNewOverdueLoan_CreatesNotificationAndSendsIt()
    {
        // Arrange
        var loanId = Guid.NewGuid();
        var patronId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var patron = new Patron("John Doe", Email.Create("john@example.com"));
        var book = new Book(Isbn.Create("9780134685991"), "Test Book", "Test Author", 1);
        var overdueLoan = new Loan(bookId, patronId, DateTime.UtcNow.AddDays(-20));

        // Use reflection to set navigation properties for testing
        var loanPatronProperty = typeof(Loan).GetProperty("Patron");
        loanPatronProperty?.SetValue(overdueLoan, patron);
        var loanBookProperty = typeof(Loan).GetProperty("Book");
        loanBookProperty?.SetValue(overdueLoan, book);

        var notification = new OverdueNotification(loanId, patronId);
        var notificationPatronProperty = typeof(OverdueNotification).GetProperty("Patron");
        notificationPatronProperty?.SetValue(notification, patron);
        var notificationLoanProperty = typeof(OverdueNotification).GetProperty("Loan");
        notificationLoanProperty?.SetValue(notification, overdueLoan);

        _loanRepositoryMock.Setup(r => r.GetOverdueLoansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan> { overdueLoan });

        _notificationRepositoryMock.Setup(r => r.ExistsForLoanAsync(loanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _notificationRepositoryMock.Setup(r => r.GetPendingNotificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification> { notification });

        _notificationRepositoryMock.Setup(r => r.GetFailedNotificationsForRetryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification>());

        _notificationServiceMock.Setup(s => s.SendOverdueNotificationAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(new ProcessOverdueNotificationsCommand());

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.SentCount);
        Assert.Equal(0, result.FailedCount);

        _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationServiceMock.Verify(s => s.SendOverdueNotificationAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        _notificationRepositoryMock.Verify(r => r.UpdateAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task HandleAsync_WithExistingNotification_DoesNotCreateDuplicate()
    {
        // Arrange
        var loanId = Guid.NewGuid();
        var patronId = Guid.NewGuid();
        var bookId = Guid.NewGuid();

        var overdueLoan = new Loan(bookId, patronId, DateTime.UtcNow.AddDays(-20));

        _loanRepositoryMock.Setup(r => r.GetOverdueLoansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan> { overdueLoan });

        _notificationRepositoryMock.Setup(r => r.ExistsForLoanAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _notificationRepositoryMock.Setup(r => r.GetPendingNotificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification>());

        _notificationRepositoryMock.Setup(r => r.GetFailedNotificationsForRetryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification>());

        // Act
        var result = await _handler.HandleAsync(new ProcessOverdueNotificationsCommand());

        // Assert
        Assert.Equal(0, result.ProcessedCount);
        Assert.Equal(0, result.SentCount);
        Assert.Equal(0, result.FailedCount);

        _notificationRepositoryMock.Verify(r => r.AddAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task HandleAsync_WithFailedNotification_RetriesAndSucceeds()
    {
        // Arrange
        var loanId = Guid.NewGuid();
        var patronId = Guid.NewGuid();

        var patron = new Patron("John Doe", Email.Create("john@example.com"));
        var book = new Book(Isbn.Create("9780134685991"), "Test Book", "Test Author", 1);
        var overdueLoan = new Loan(Guid.NewGuid(), patronId, DateTime.UtcNow.AddDays(-20));

        var loanPatronProperty = typeof(Loan).GetProperty("Patron");
        loanPatronProperty?.SetValue(overdueLoan, patron);
        var loanBookProperty = typeof(Loan).GetProperty("Book");
        loanBookProperty?.SetValue(overdueLoan, book);

        var failedNotification = new OverdueNotification(loanId, patronId);
        failedNotification.MarkAsFailed("Previous failure");

        var notificationPatronProperty = typeof(OverdueNotification).GetProperty("Patron");
        notificationPatronProperty?.SetValue(failedNotification, patron);
        var notificationLoanProperty = typeof(OverdueNotification).GetProperty("Loan");
        notificationLoanProperty?.SetValue(failedNotification, overdueLoan);

        _loanRepositoryMock.Setup(r => r.GetOverdueLoansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan>());

        _notificationRepositoryMock.Setup(r => r.GetPendingNotificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification>());

        _notificationRepositoryMock.Setup(r => r.GetFailedNotificationsForRetryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification> { failedNotification });

        _notificationServiceMock.Setup(s => s.SendOverdueNotificationAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.HandleAsync(new ProcessOverdueNotificationsCommand());

        // Assert
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.SentCount);
        Assert.Equal(0, result.FailedCount);

        _notificationServiceMock.Verify(s => s.SendOverdueNotificationAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()), Times.Once);
    }
}
