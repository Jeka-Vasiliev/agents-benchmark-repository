using FluentValidation;

namespace LibraryLending.Application.UseCases.Loans.LoanBook;

public class LoanBookValidator : AbstractValidator<LoanBookCommand>
{
    public LoanBookValidator()
    {
        RuleFor(x => x.BookId)
            .NotEmpty()
            .WithMessage("Book ID is required.");

        RuleFor(x => x.PatronId)
            .NotEmpty()
            .WithMessage("Patron ID is required.");
    }
}