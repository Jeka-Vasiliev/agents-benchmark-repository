using LibraryLending.Domain.Repositories;
using MediatR;

namespace LibraryLending.Application.UseCases.Loans.ReturnBook;

public class ReturnBookHandler : IRequestHandler<ReturnBookCommand>
{
    private readonly ILoanRepository _loanRepository;
    private readonly IBookRepository _bookRepository;

    public ReturnBookHandler(ILoanRepository loanRepository, IBookRepository bookRepository)
    {
        _loanRepository = loanRepository;
        _bookRepository = bookRepository;
    }

    public async Task<Unit> Handle(ReturnBookCommand request, CancellationToken cancellationToken)
    {
        var loan = await _loanRepository.GetByIdAsync(request.LoanId, cancellationToken)
            ?? throw new ArgumentException($"Loan with ID {request.LoanId} not found.");

        var book = await _bookRepository.GetByIdAsync(loan.BookId, cancellationToken)
            ?? throw new InvalidOperationException($"Book with ID {loan.BookId} not found.");

        // This will throw LoanAlreadyReturnedException if already returned
        loan.Return(DateTime.UtcNow);
        book.ReturnCopy();

        await _loanRepository.UpdateAsync(loan, cancellationToken);
        await _bookRepository.UpdateAsync(book, cancellationToken);
        
        return Unit.Value;
    }
}