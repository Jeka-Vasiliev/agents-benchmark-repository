using MediatR;

namespace LibraryLending.Application.UseCases.Loans.ReturnBook;

public record ReturnBookCommand(Guid LoanId) : IRequest;