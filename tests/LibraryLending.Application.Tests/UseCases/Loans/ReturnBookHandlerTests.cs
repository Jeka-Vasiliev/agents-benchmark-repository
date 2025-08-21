using FluentAssertions;
using LibraryLending.Application.UseCases.Loans.ReturnBook;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Exceptions;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.ValueObjects;
using Moq;

namespace LibraryLending.Application.Tests.UseCases.Loans;

public class ReturnBookHandlerTests
{
    private readonly Mock<ILoanRepository> _loanRepositoryMock;
    private readonly Mock<IBookRepository> _bookRepositoryMock;
    private readonly ReturnBookHandler _handler;

    public ReturnBookHandlerTests()
    {
        _loanRepositoryMock = new Mock<ILoanRepository>();
        _bookRepositoryMock = new Mock<IBookRepository>();
        _handler = new ReturnBookHandler(_loanRepositoryMock.Object, _bookRepositoryMock.Object);
    }

    [Fact]
    public async Task Handle_WhenLoanExists_ShouldReturnBookAndIncreaseAvailableCopies()
    {
        // Arrange
        var loanId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var patronId = Guid.NewGuid();
        var isbn = Isbn.Create("9780134685991");
        
        var book = new Book(isbn, "Test Book", "Test Author", 2);
        book.LoanCopy(); // Decrease available copies to 1
        
        var loan = new Loan(bookId, patronId, DateTime.UtcNow.AddDays(-7));
        var command = new ReturnBookCommand(loanId);

        _loanRepositoryMock.Setup(x => x.GetByIdAsync(loanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);
        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        loan.IsReturned.Should().BeTrue();
        loan.ReturnedAt.Should().NotBeNull();
        book.AvailableCopies.Should().Be(2); // Increased back to 2
        
        _loanRepositoryMock.Verify(x => x.UpdateAsync(loan, It.IsAny<CancellationToken>()), Times.Once);
        _bookRepositoryMock.Verify(x => x.UpdateAsync(book, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenLoanAlreadyReturned_ShouldThrowLoanAlreadyReturnedException()
    {
        // Arrange
        var loanId = Guid.NewGuid();
        var bookId = Guid.NewGuid();
        var patronId = Guid.NewGuid();
        var isbn = Isbn.Create("9780134685991");
        
        var book = new Book(isbn, "Test Book", "Test Author", 2);
        var loan = new Loan(bookId, patronId, DateTime.UtcNow.AddDays(-7));
        loan.Return(DateTime.UtcNow.AddDays(-1)); // Already returned
        
        var command = new ReturnBookCommand(loanId);

        _loanRepositoryMock.Setup(x => x.GetByIdAsync(loanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(loan);
        _bookRepositoryMock.Setup(x => x.GetByIdAsync(bookId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(book);

        // Act & Assert
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<LoanAlreadyReturnedException>();
        
        _loanRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Loan>(), It.IsAny<CancellationToken>()), Times.Never);
        _bookRepositoryMock.Verify(x => x.UpdateAsync(It.IsAny<Book>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLoanNotFound_ShouldThrowArgumentException()
    {
        // Arrange
        var loanId = Guid.NewGuid();
        var command = new ReturnBookCommand(loanId);

        _loanRepositoryMock.Setup(x => x.GetByIdAsync(loanId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Loan?)null);

        // Act & Assert
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage($"Loan with ID {loanId} not found.");
    }
}