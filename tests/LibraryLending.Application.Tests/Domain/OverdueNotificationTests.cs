using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Exceptions;
using Xunit;

namespace LibraryLending.Application.Tests.Domain;

public class OverdueNotificationTests
{
    [Fact]
    public void Constructor_ShouldCreateNotificationWithPendingStatus()
    {
        // Arrange
        var loanId = Guid.NewGuid();
        var patronId = Guid.NewGuid();

        // Act
        var notification = new OverdueNotification(loanId, patronId);

        // Assert
        Assert.Equal(loanId, notification.LoanId);
        Assert.Equal(patronId, notification.PatronId);
        Assert.Equal(OverdueNotificationStatus.Pending, notification.Status);
        Assert.Equal(0, notification.RetryCount);
        Assert.Null(notification.SentAt);
        Assert.Null(notification.NextRetryAt);
    }

    [Fact]
    public void MarkAsSent_ShouldUpdateStatusAndSetSentAt()
    {
        // Arrange
        var notification = new OverdueNotification(Guid.NewGuid(), Guid.NewGuid());

        // Act
        notification.MarkAsSent();

        // Assert
        Assert.Equal(OverdueNotificationStatus.Sent, notification.Status);
        Assert.NotNull(notification.SentAt);
        Assert.Null(notification.ErrorMessage);
        Assert.Null(notification.NextRetryAt);
    }

    [Fact]
    public void MarkAsSent_WhenAlreadySent_ShouldThrowException()
    {
        // Arrange
        var notification = new OverdueNotification(Guid.NewGuid(), Guid.NewGuid());
        notification.MarkAsSent();

        // Act & Assert
        Assert.Throws<NotificationException>(() => notification.MarkAsSent());
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndScheduleRetry()
    {
        // Arrange
        var notification = new OverdueNotification(Guid.NewGuid(), Guid.NewGuid());
        var errorMessage = "Email service unavailable";

        // Act
        notification.MarkAsFailed(errorMessage);

        // Assert
        Assert.Equal(OverdueNotificationStatus.Failed, notification.Status);
        Assert.Equal(errorMessage, notification.ErrorMessage);
        Assert.Equal(1, notification.RetryCount);
        Assert.NotNull(notification.NextRetryAt);
        Assert.True(notification.NextRetryAt > DateTime.UtcNow);
    }

    [Fact]
    public void MarkAsFailed_MultipleFailures_ShouldIncreaseRetryCountAndDelay()
    {
        // Arrange
        var notification = new OverdueNotification(Guid.NewGuid(), Guid.NewGuid());

        // Act - Первый сбой
        notification.MarkAsFailed("First failure");
        var firstRetryTime = notification.NextRetryAt;
        var firstRetryCount = notification.RetryCount;

        // Act - Второй сбой
        notification.MarkAsFailed("Second failure");
        var secondRetryTime = notification.NextRetryAt;
        var secondRetryCount = notification.RetryCount;

        // Assert
        Assert.Equal(1, firstRetryCount);
        Assert.Equal(2, secondRetryCount);
        Assert.True(secondRetryTime > firstRetryTime); // Задержка увеличивается
    }

    [Fact]
    public void ShouldRetry_WhenFailedAndTimeHasPassed_ShouldReturnTrue()
    {
        // Arrange
        var notification = new OverdueNotification(Guid.NewGuid(), Guid.NewGuid());
        notification.MarkAsFailed("Test failure");
        
        // Устанавливаем время повтора в прошлое
        typeof(OverdueNotification).GetProperty("NextRetryAt")!
            .SetValue(notification, DateTime.UtcNow.AddMinutes(-1));

        // Act & Assert
        Assert.True(notification.ShouldRetry());
    }

    [Fact]
    public void ShouldRetry_WhenMaxRetriesReached_ShouldReturnFalse()
    {
        // Arrange
        var notification = new OverdueNotification(Guid.NewGuid(), Guid.NewGuid());
        
        // Симулируем максимальное количество попыток
        for (int i = 0; i < 10; i++)
        {
            notification.MarkAsFailed($"Failure {i + 1}");
        }

        // Act & Assert
        Assert.False(notification.ShouldRetry());
    }

    [Fact]
    public void ShouldRetry_WhenTimeHasNotPassed_ShouldReturnFalse()
    {
        // Arrange
        var notification = new OverdueNotification(Guid.NewGuid(), Guid.NewGuid());
        notification.MarkAsFailed("Test failure");

        // Act & Assert
        Assert.False(notification.ShouldRetry()); // Время еще не наступило
    }

    [Fact]
    public void ResetForRetry_WhenEligible_ShouldResetToPendingStatus()
    {
        // Arrange
        var notification = new OverdueNotification(Guid.NewGuid(), Guid.NewGuid());
        notification.MarkAsFailed("Test failure");
        
        // Устанавливаем время повтора в прошлое
        typeof(OverdueNotification).GetProperty("NextRetryAt")!
            .SetValue(notification, DateTime.UtcNow.AddMinutes(-1));

        // Act
        notification.ResetForRetry();

        // Assert
        Assert.Equal(OverdueNotificationStatus.Pending, notification.Status);
        Assert.Null(notification.ErrorMessage);
        Assert.Null(notification.NextRetryAt);
    }

    [Fact]
    public void ResetForRetry_WhenNotEligible_ShouldThrowException()
    {
        // Arrange
        var notification = new OverdueNotification(Guid.NewGuid(), Guid.NewGuid());
        notification.MarkAsFailed("Test failure");
        // Время повтора еще не наступило

        // Act & Assert
        Assert.Throws<NotificationException>(() => notification.ResetForRetry());
    }
}
