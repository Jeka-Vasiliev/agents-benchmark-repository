using FluentValidation;

namespace LibraryLending.Application.UseCases.Books.AddBook;

public class AddBookValidator : AbstractValidator<AddBookCommand>
{
    public AddBookValidator()
    {
        RuleFor(x => x.Isbn)
            .NotEmpty()
            .WithMessage("ISBN is required.")
            .Length(10, 17)
            .WithMessage("ISBN must be between 10 and 17 characters.");

        RuleFor(x => x.Title)
            .NotEmpty()
            .WithMessage("Title is required.")
            .MaximumLength(500)
            .WithMessage("Title cannot exceed 500 characters.");

        RuleFor(x => x.Author)
            .NotEmpty()
            .WithMessage("Author is required.")
            .MaximumLength(200)
            .WithMessage("Author cannot exceed 200 characters.");

        RuleFor(x => x.TotalCopies)
            .GreaterThan(0)
            .WithMessage("Total copies must be at least 1.")
            .LessThanOrEqualTo(1000)
            .WithMessage("Total copies cannot exceed 1000.");
    }
}