using FluentValidation;

namespace LibraryLending.Application.UseCases.Patrons.RegisterPatron;

public class RegisterPatronValidator : AbstractValidator<RegisterPatronCommand>
{
    public RegisterPatronValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty()
            .WithMessage("Full name is required.")
            .MaximumLength(200)
            .WithMessage("Full name cannot exceed 200 characters.");

        RuleFor(x => x.Email)
            .NotEmpty()
            .WithMessage("Email is required.")
            .EmailAddress()
            .WithMessage("Invalid email format.")
            .MaximumLength(320)
            .WithMessage("Email cannot exceed 320 characters.");
    }
}