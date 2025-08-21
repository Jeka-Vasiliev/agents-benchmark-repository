using LibraryLending.Application.DTOs;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using MediatR;

namespace LibraryLending.Application.UseCases.Loans.LoanBook;

public class LoanBookHandler : IRequestHandler<LoanBookCommand, LoanDto>
{
    private readonly IBookRepository _bookRepository;
    private readonly IPatronRepository _patronRepository;
    private readonly ILoanRepository _loanRepository;

    public LoanBookHandler(
        IBookRepository bookRepository,
        IPatronRepository patronRepository,
        ILoanRepository loanRepository)
    {
        _bookRepository = bookRepository;
        _patronRepository = patronRepository;
        _loanRepository = loanRepository;
    }

    public async Task<LoanDto> Handle(LoanBookCommand request, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(request.BookId, cancellationToken)
            ?? throw new ArgumentException($"Book with ID {request.BookId} not found.");

        var patron = await _patronRepository.GetByIdAsync(request.PatronId, cancellationToken)
            ?? throw new ArgumentException($"Patron with ID {request.PatronId} not found.");

        if (!patron.Active)
            throw new InvalidOperationException("Cannot loan book to inactive patron.");

        // This will throw BookUnavailableException if no copies available
        book.LoanCopy();

        var loanedAt = DateTime.UtcNow;
        var loan = new Loan(request.BookId, request.PatronId, loanedAt);

        await _bookRepository.UpdateAsync(book, cancellationToken);
        await _loanRepository.AddAsync(loan, cancellationToken);

        return new LoanDto(
            loan.Id,
            loan.BookId,
            loan.PatronId,
            loan.LoanedAt,
            loan.DueAt,
            loan.ReturnedAt,
            loan.IsReturned,
            loan.IsOverdue);
    }
}