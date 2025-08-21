using LibraryLending.Application.DTOs;
using MediatR;

namespace LibraryLending.Application.UseCases.Patrons.GetPatron;

public record GetPatronQuery(Guid PatronId) : IRequest<PatronDetailsDto?>;