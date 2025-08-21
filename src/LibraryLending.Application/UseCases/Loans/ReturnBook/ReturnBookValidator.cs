using FluentValidation;

namespace LibraryLending.Application.UseCases.Loans.ReturnBook;

public class ReturnBookValidator : AbstractValidator<ReturnBookCommand>
{
    public ReturnBookValidator()
    {
        RuleFor(x => x.LoanId)
            .NotEmpty()
            .WithMessage("Loan ID is required.");
    }
}