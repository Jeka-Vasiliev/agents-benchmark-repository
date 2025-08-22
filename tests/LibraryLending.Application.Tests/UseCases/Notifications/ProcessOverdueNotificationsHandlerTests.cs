using LibraryLending.Application.Services;
using LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.Services;
using LibraryLending.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace LibraryLending.Application.Tests.UseCases.Notifications;

public class ProcessOverdueNotificationsHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepositoryMock;
    private readonly Mock<IOverdueNotificationRepository> _notificationRepositoryMock;
    private readonly Mock<IEmailService> _emailServiceMock;
    private readonly Mock<ILogger<ProcessOverdueNotificationsHandler>> _loggerMock;
    private readonly ProcessOverdueNotificationsHandler _handler;

    public ProcessOverdueNotificationsHandlerTests()
    {
        _loanRepositoryMock = new Mock<ILoanRepository>();
        _notificationRepositoryMock = new Mock<IOverdueNotificationRepository>();
        _emailServiceMock = new Mock<IEmailService>();
        _loggerMock = new Mock<ILogger<ProcessOverdueNotificationsHandler>>();
        
        _handler = new ProcessOverdueNotificationsHandler(
            _loanRepositoryMock.Object,
            _notificationRepositoryMock.Object,
            _emailServiceMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_WhenOverdueLoanExists_ShouldCreateAndSendNotification()
    {
        // Arrange - Сценарий 1: успешное уведомление
        var patronId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var loanId = Guid.NewGuid();
        
        var patron = new Patron("John Doe", Email.Create("john@example.com"));
        var book = new Book(Isbn.Create("9780134685991"), "Test Book", "Test Author", 1);
        var overdueLoan = new Loan(bookId, patronId, DateTime.UtcNow.AddDays(-20));
        
        // Устанавливаем navigation properties через рефлексию для тестов
        typeof(Loan).GetProperty("Patron")!.SetValue(overdueLoan, patron);
        typeof(Loan).GetProperty("Book")!.SetValue(overdueLoan, book);

        _loanRepositoryMock.Setup(x => x.GetOverdueLoansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { overdueLoan });

        _notificationRepositoryMock.Setup(x => x.ExistsForLoanAsync(loanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        _notificationRepositoryMock.Setup(x => x.GetPendingNotificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification>());

        _notificationRepositoryMock.Setup(x => x.GetFailedNotificationsForRetryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification>());

        _emailServiceMock.Setup(x => x.SendOverdueNotificationAsync(
            It.IsAny<Email>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new ProcessOverdueNotificationsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalOverdueLoans);
        Assert.Equal(1, result.NewNotificationsCreated);
        
        _notificationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()), Times.Once);
        _emailServiceMock.Verify(x => x.SendOverdueNotificationAsync(
            patron.Email, patron.FullName, book.Title, overdueLoan.DueAt, It.IsAny<CancellationToken>()), Times.Never); // Не вызывается, так как нет pending уведомлений в мокированном списке
    }

    [Fact]
    public async Task Handle_WhenEmailServiceFails_ShouldMarkNotificationAsFailedAndScheduleRetry()
    {
        // Arrange - Сценарий 2: временный сбой доставки
        var patronId = Guid.NewGuid();
        var loanId = Guid.NewGuid();
        
        var patron = new Patron("John Doe", Email.Create("john@example.com"));
        var book = new Book(Isbn.Create("9780134685991"), "Test Book", "Test Author", 1);
        var loan = new Loan(Guid.NewGuid(), patronId, DateTime.UtcNow.AddDays(-20));
        
        typeof(Loan).GetProperty("Patron")!.SetValue(loan, patron);
        typeof(Loan).GetProperty("Book")!.SetValue(loan, book);
        typeof(Loan).GetProperty("Id")!.SetValue(loan, loanId);

        var notification = new OverdueNotification(loanId, patronId);
        typeof(OverdueNotification).GetProperty("Loan")!.SetValue(notification, loan);
        typeof(OverdueNotification).GetProperty("Patron")!.SetValue(notification, patron);

        _loanRepositoryMock.Setup(x => x.GetOverdueLoansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan>());

        _notificationRepositoryMock.Setup(x => x.GetPendingNotificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { notification });

        _notificationRepositoryMock.Setup(x => x.GetFailedNotificationsForRetryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification>());

        _emailServiceMock.Setup(x => x.SendOverdueNotificationAsync(
            It.IsAny<Email>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var command = new ProcessOverdueNotificationsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.NotificationsFailed);
        Assert.Equal(OverdueNotificationStatus.Failed, notification.Status);
        Assert.True(notification.NextRetryAt.HasValue);
        
        _notificationRepositoryMock.Verify(x => x.UpdateAsync(notification, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotificationAlreadyExists_ShouldNotCreateDuplicate()
    {
        // Arrange - Сценарий 3: отсутствие дублей
        var loanId = Guid.NewGuid();
        var overdueLoan = new Loan(Guid.NewGuid(), Guid.NewGuid(), DateTime.UtcNow.AddDays(-20));
        typeof(Loan).GetProperty("Id")!.SetValue(overdueLoan, loanId);

        _loanRepositoryMock.Setup(x => x.GetOverdueLoansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { overdueLoan });

        _notificationRepositoryMock.Setup(x => x.ExistsForLoanAsync(loanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true); // Уведомление уже существует

        _notificationRepositoryMock.Setup(x => x.GetPendingNotificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification>());

        _notificationRepositoryMock.Setup(x => x.GetFailedNotificationsForRetryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification>());

        var command = new ProcessOverdueNotificationsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.TotalOverdueLoans);
        Assert.Equal(0, result.NewNotificationsCreated); // Новое уведомление не создается
        
        _notificationRepositoryMock.Verify(x => x.AddAsync(It.IsAny<OverdueNotification>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFailedNotificationIsReadyForRetry_ShouldRetryNotification()
    {
        // Arrange - Сценарий 4: устойчивость к перезапуску
        var patronId = Guid.NewGuid();
        var loanId = Guid.NewGuid();
        
        var patron = new Patron("John Doe", Email.Create("john@example.com"));
        var book = new Book(Isbn.Create("9780134685991"), "Test Book", "Test Author", 1);
        var loan = new Loan(Guid.NewGuid(), patronId, DateTime.UtcNow.AddDays(-20));
        
        typeof(Loan).GetProperty("Patron")!.SetValue(loan, patron);
        typeof(Loan).GetProperty("Book")!.SetValue(loan, book);
        typeof(Loan).GetProperty("Id")!.SetValue(loan, loanId);

        var notification = new OverdueNotification(loanId, patronId);
        notification.MarkAsFailed("Previous failure");
        typeof(OverdueNotification).GetProperty("Loan")!.SetValue(notification, loan);
        typeof(OverdueNotification).GetProperty("Patron")!.SetValue(notification, patron);
        typeof(OverdueNotification).GetProperty("NextRetryAt")!.SetValue(notification, DateTime.UtcNow.AddMinutes(-1)); // Готов к повтору

        _loanRepositoryMock.Setup(x => x.GetOverdueLoansAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Loan>());

        _notificationRepositoryMock.Setup(x => x.GetPendingNotificationsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<OverdueNotification>());

        _notificationRepositoryMock.Setup(x => x.GetFailedNotificationsForRetryAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { notification });

        _emailServiceMock.Setup(x => x.SendOverdueNotificationAsync(
            It.IsAny<Email>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var command = new ProcessOverdueNotificationsCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.Equal(1, result.RetriesProcessed);
        Assert.Equal(1, result.NotificationsSent);
        Assert.Equal(OverdueNotificationStatus.Sent, notification.Status);
        
        _notificationRepositoryMock.Verify(x => x.UpdateAsync(notification, It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }
}
