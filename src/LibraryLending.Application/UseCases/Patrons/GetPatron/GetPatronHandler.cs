using LibraryLending.Application.DTOs;
using LibraryLending.Domain.Repositories;
using MediatR;

namespace LibraryLending.Application.UseCases.Patrons.GetPatron;

public class GetPatronHandler : IRequestHandler<GetPatronQuery, PatronDetailsDto?>
{
    private readonly IPatronRepository _patronRepository;
    private readonly ILoanRepository _loanRepository;

    public GetPatronHandler(IPatronRepository patronRepository, ILoanRepository loanRepository)
    {
        _patronRepository = patronRepository;
        _loanRepository = loanRepository;
    }

    public async Task<PatronDetailsDto?> Handle(GetPatronQuery request, CancellationToken cancellationToken)
    {
        var patron = await _patronRepository.GetByIdAsync(request.PatronId, cancellationToken);
        if (patron == null)
            return null;

        var activeLoans = await _loanRepository.GetActiveLoansForPatronAsync(request.PatronId, cancellationToken);
        
        var activeLoanDtos = activeLoans.Select(loan => new ActiveLoanDto(
            loan.Id,
            loan.BookId,
            loan.Book.Title,
            loan.Book.Author,
            loan.LoanedAt,
            loan.DueAt,
            loan.IsOverdue));

        return new PatronDetailsDto(
            patron.Id,
            patron.FullName,
            patron.Email,
            patron.Active,
            activeLoanDtos);
    }
}