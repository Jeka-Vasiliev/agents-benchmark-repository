using LibraryLending.Application.DTOs;
using MediatR;

namespace LibraryLending.Application.UseCases.Loans.LoanBook;

public record LoanBookCommand(
    Guid BookId,
    Guid PatronId) : IRequest<LoanDto>;