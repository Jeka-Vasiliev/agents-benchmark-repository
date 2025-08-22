using FluentValidation;
using LibraryLending.Application.UseCases.Notifications.ProcessOverdueNotifications;
using LibraryLending.Application.UseCases.Patrons.RegisterPatron;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.Services;
using LibraryLending.Infrastructure.Data;
using LibraryLending.Infrastructure.Repositories;
using LibraryLending.Infrastructure.Services;
using LibraryLending.WebApi.Middleware;
using MediatR;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add MediatR
builder.Services.AddMediatR(typeof(RegisterPatronCommand).Assembly);

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(RegisterPatronValidator).Assembly);

// Add Entity Framework
builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseInMemoryDatabase("LibraryLendingDb"));

// Add repositories
builder.Services.AddScoped<IBookRepository, BookRepository>();
builder.Services.AddScoped<IPatronRepository, PatronRepository>();
builder.Services.AddScoped<ILoanRepository, LoanRepository>();
builder.Services.AddScoped<IOverdueNotificationRepository, OverdueNotificationRepository>();

// Add services
builder.Services.AddScoped<INotificationService, EmailNotificationService>();
builder.Services.AddScoped<ProcessOverdueNotificationsCommandHandler>();

// Add background service
builder.Services.AddHostedService<OverdueNotificationBackgroundService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    
    // Seed data
    await SeedDataAsync(app);
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseHttpsRedirection();
app.MapControllers();

app.Run();

static async Task SeedDataAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    
    if (!context.Books.Any())
    {
        var book1 = new LibraryLending.Domain.Entities.Book(
            LibraryLending.Domain.ValueObjects.Isbn.Create("9780134685991"),
            "Effective Java",
            "Joshua Bloch",
            3);
            
        var book2 = new LibraryLending.Domain.Entities.Book(
            LibraryLending.Domain.ValueObjects.Isbn.Create("9780135166307"),
            "Clean Code",
            "Robert C. Martin",
            2);
            
        context.Books.AddRange(book1, book2);
    }
    
    if (!context.Patrons.Any())
    {
        var patron1 = new LibraryLending.Domain.Entities.Patron(
            "John Doe",
            LibraryLending.Domain.ValueObjects.Email.Create("john.doe@example.com"));
            
        var patron2 = new LibraryLending.Domain.Entities.Patron(
            "Jane Smith", 
            LibraryLending.Domain.ValueObjects.Email.Create("jane.smith@example.com"));
            
        context.Patrons.AddRange(patron1, patron2);
        await context.SaveChangesAsync();
        
        // Create some overdue loans for testing
        var book = context.Books.First();
        var overdueDate = DateTime.UtcNow.AddDays(-20); // 20 days ago, overdue by 6 days
        
        var overdueLoan = new LibraryLending.Domain.Entities.Loan(
            book.Id, 
            patron1.Id, 
            overdueDate);
            
        context.Loans.Add(overdueLoan);
    }
    
    await context.SaveChangesAsync();
}
