using LibraryLending.Application.DTOs;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Exceptions;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.ValueObjects;
using MediatR;

namespace LibraryLending.Application.UseCases.Patrons.RegisterPatron;

public class RegisterPatronHandler : IRequestHandler<RegisterPatronCommand, PatronDto>
{
    private readonly IPatronRepository _patronRepository;

    public RegisterPatronHandler(IPatronRepository patronRepository)
    {
        _patronRepository = patronRepository;
    }

    public async Task<PatronDto> Handle(RegisterPatronCommand request, CancellationToken cancellationToken)
    {
        var email = Email.Create(request.Email);

        if (await _patronRepository.ExistsByEmailAsync(email, cancellationToken))
            throw new PatronEmailAlreadyExistsException(request.Email);

        var patron = new Patron(request.FullName, email);
        await _patronRepository.AddAsync(patron, cancellationToken);

        return new PatronDto(
            patron.Id,
            patron.FullName,
            patron.Email,
            patron.Active);
    }
}