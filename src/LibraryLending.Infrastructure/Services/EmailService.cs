using LibraryLending.Domain.Services;
using LibraryLending.Domain.ValueObjects;
using Microsoft.Extensions.Logging;

namespace LibraryLending.Infrastructure.Services;

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;

    public EmailService(ILogger<EmailService> logger)
    {
        _logger = logger;
    }

    public async Task<bool> SendOverdueNotificationAsync(Email recipientEmail, string patronName, string bookTitle, DateTime dueDate, CancellationToken cancellationToken = default)
    {
        try
        {
            // Имитация отправки email
            // В реальном проекте здесь будет интеграция с почтовым сервисом (SendGrid, SMTP и т.д.)
            
            var emailContent = $@"
Уважаемый(ая) {patronName},

Напоминаем, что срок возврата книги ""{bookTitle}"" истёк {dueDate:dd.MM.yyyy}.
Пожалуйста, верните книгу в библиотеку как можно скорее.

С уважением,
Библиотечная система
";

            _logger.LogInformation("Sending overdue notification email to {Email} for book '{BookTitle}' due on {DueDate}", 
                recipientEmail.Value, bookTitle, dueDate);

            // Симуляция времени отправки
            await Task.Delay(100, cancellationToken);

            // Симуляция случайных сбоев (5% вероятность)
            var random = new Random();
            if (random.NextDouble() < 0.05)
            {
                _logger.LogWarning("Simulated email service failure for {Email}", recipientEmail.Value);
                return false;
            }

            _logger.LogInformation("Successfully sent overdue notification email to {Email}. Content: {EmailContent}", 
                recipientEmail.Value, emailContent);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending overdue notification email to {Email}", recipientEmail.Value);
            return false;
        }
    }
}
