using LibraryLending.Domain.ValueObjects;

namespace LibraryLending.Domain.Services;

public interface IEmailService
{
    Task<bool> SendOverdueNotificationAsync(Email recipientEmail, string patronName, string bookTitle, DateTime dueDate, CancellationToken cancellationToken = default);
}
