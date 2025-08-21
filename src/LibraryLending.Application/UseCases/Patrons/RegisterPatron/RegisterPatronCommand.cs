using LibraryLending.Application.DTOs;
using MediatR;

namespace LibraryLending.Application.UseCases.Patrons.RegisterPatron;

public record RegisterPatronCommand(
    string FullName,
    string Email) : IRequest<PatronDto>;