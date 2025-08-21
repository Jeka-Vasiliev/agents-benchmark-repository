using FluentAssertions;
using LibraryLending.Application.UseCases.Loans.LoanBook;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Exceptions;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.ValueObjects;
using Moq;

namespace LibraryLending.Application.Tests.UseCases.Loans;

public class LoanBookHandlerTests
{
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly Mock<IPatronRepository> _patronRepositoryMock;
    private readonly Mock<ILoanRepository> _loanRepositoryMock;
    private readonly LoanBookHandler _handler;

    public LoanBookHandlerTests()
    {
        _bookRepositoryMock = new Mock<IBookRepository>();
        _patronRepositoryMock = new Mock<IPatronRepository>();
        _loanRepositoryMock = new Mock<ILoanRepository>();
        _handler = new LoanBookHandler(_bookRepositoryMock.Object, _patronRepositoryMock.Object, _loanRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenBookIsAvailable_ShouldCreateLoanAndDecreaseAvailableCopies()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var patronId = Guid.NewGuid();
        var isbn = Isbn.Create("9780134685991");
        var email = Email.Create("test@example.com");
        
        var book = new Book(isbn, "Test Book", "Test Author", 2);
        var patron = new Patron("Test Patron", email);
        
        var command = new LoanBookCommand(bookId, patronId);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _patronRepositoryMock.Setup(x => x.GetByIdAsync(patronId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patron);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.BookId.Should().Be(bookId);
        result.PatronId.Should().Be(patronId);
        result.IsReturned.Should().BeFalse();
        
        book.AvailableCopies.Should().Be(1); // Decreased from 2 to 1
        
        _bookRepositoryMock.Verify(x => x.UpdateAsync(book, It.IsAny<CancellationToken>()), Times.Once);
        _loanRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBookIsNotAvailable_ShouldThrowBookUnavailableException()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var patronId = Guid.NewGuid();
        var isbn = Isbn.Create("9780134685991");
        var email = Email.Create("test@example.com");
        
        var book = new Book(isbn, "Test Book", "Test Author", 1);
        book.LoanCopy(); // Make book unavailable
        var patron = new Patron("Test Patron", email);
        
        var command = new LoanBookCommand(bookId, patronId);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _patronRepositoryMock.Setup(x => x.GetByIdAsync(patronId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patron);

        // Act & Assert
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<BookUnavailableException>();
        
        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
        _loanRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenBookNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var patronId = Guid.NewGuid();
        var command = new LoanBookCommand(bookId, patronId);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Book?)null);

        // Act & Assert
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Book with ID {bookId} not found.");
    }

    [Fact]
    public async Task Handle_WhenPatronNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var patronId = Guid.NewGuid();
        var isbn = Isbn.Create("9780134685991");
        var book = new Book(isbn, "Test Book", "Test Author", 2);
        var command = new LoanBookCommand(bookId, patronId);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _patronRepositoryMock.Setup(x => x.GetByIdAsync(patronId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Patron?)null);

        // Act & Assert
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Patron with ID {patronId} not found.");
    }

    [Fact]
    public async Task Handle_WhenPatronIsInactive_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var bookId = Guid.NewGuid();
        var patronId = Guid.NewGuid();
        var isbn = Isbn.Create("9780134685991");
        var email = Email.Create("test@example.com");
        
        var book = new Book(isbn, "Test Book", "Test Author", 2);
        var patron = new Patron("Test Patron", email);
        patron.Deactivate(); // Make patron inactive
        
        var command = new LoanBookCommand(bookId, patronId);

        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);
        _patronRepositoryMock.Setup(x => x.GetByIdAsync(patronId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(patron);

        // Act & Assert
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Cannot loan book to inactive patron.");
    }
}