using LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.ValueObjects;
using LibraryLending.Infrastructure.Data;
using LibraryLending.WebApi;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Net.Http.Json;
using Xunit;

namespace LibraryLending.Application.Tests.Integration;

public class OverdueNotificationIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public OverdueNotificationIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task ProcessOverdueNotifications_WithOverdueLoans_ShouldCreateNotifications()
    {
        // Arrange - создаем тестовые данные
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        
        // Очищаем существующие данные
        context.OverdueNotifications.RemoveRange(context.OverdueNotifications);
        context.Loans.RemoveRange(context.Loans);
        context.Books.RemoveRange(context.Books);
        context.Patrons.RemoveRange(context.Patrons);
        await context.SaveChangesAsync();

        // Создаем тестовые данные
        var patron = new Patron("Test User", Email.Create("test@example.com"));
        var book = new Book(Isbn.Create("9780134685991"), "Test Book", "Test Author", 1);
        
        context.Patrons.Add(patron);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        // Создаем просроченный займ
        var overdueLoan = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-20));
        context.Loans.Add(overdueLoan);
        await context.SaveChangesAsync();

        // Act - вызываем API для обработки уведомлений
        var response = await _client.PostAsync("/api/notifications/process-overdue", null);

        // Assert
        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ProcessOverdueNotificationsResult>();
        
        Assert.NotNull(result);
        Assert.Equal(1, result.TotalOverdueLoans);
        Assert.Equal(1, result.NewNotificationsCreated);
        
        // Проверяем, что уведомление создано в базе данных
        var notification = await context.OverdueNotifications.FirstOrDefaultAsync();
        Assert.NotNull(notification);
        Assert.Equal(overdueLoan.Id, notification.LoanId);
        Assert.Equal(patron.Id, notification.PatronId);
    }

    [Fact]
    public async Task ProcessOverdueNotifications_MultipleCalls_ShouldNotCreateDuplicates()
    {
        // Arrange
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        
        // Очищаем данные
        context.OverdueNotifications.RemoveRange(context.OverdueNotifications);
        context.Loans.RemoveRange(context.Loans);
        context.Books.RemoveRange(context.Books);
        context.Patrons.RemoveRange(context.Patrons);
        await context.SaveChangesAsync();

        var patron = new Patron("Test User", Email.Create("test@example.com"));
        var book = new Book(Isbn.Create("9780134685991"), "Test Book", "Test Author", 1);
        
        context.Patrons.Add(patron);
        context.Books.Add(book);
        await context.SaveChangesAsync();

        var overdueLoan = new Loan(book.Id, patron.Id, DateTime.UtcNow.AddDays(-20));
        context.Loans.Add(overdueLoan);
        await context.SaveChangesAsync();

        // Act - вызываем API дважды
        var response1 = await _client.PostAsync("/api/notifications/process-overdue", null);
        var response2 = await _client.PostAsync("/api/notifications/process-overdue", null);

        // Assert
        response1.EnsureSuccessStatusCode();
        response2.EnsureSuccessStatusCode();
        
        var result1 = await response1.Content.ReadFromJsonAsync<ProcessOverdueNotificationsResult>();
        var result2 = await response2.Content.ReadFromJsonAsync<ProcessOverdueNotificationsResult>();
        
        Assert.Equal(1, result1!.NewNotificationsCreated);
        Assert.Equal(0, result2!.NewNotificationsCreated); // Дублирования нет
        
        // Проверяем, что в базе только одно уведомление
        var notificationCount = await context.OverdueNotifications.CountAsync();
        Assert.Equal(1, notificationCount);
    }
}
