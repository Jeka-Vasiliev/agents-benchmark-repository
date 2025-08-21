using FluentValidation;
using LibraryLending.Application.DTOs;
using LibraryLending.Application.UseCases.Patrons.GetPatron;
using LibraryLending.Application.UseCases.Patrons.RegisterPatron;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LibraryLending.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PatronsController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<RegisterPatronCommand> _registerPatronValidator;

    public PatronsController(IMediator mediator, IValidator<RegisterPatronCommand> registerPatronValidator)
    {
        _mediator = mediator;
        _registerPatronValidator = registerPatronValidator;
    }

    /// <summary>
    /// Register a new patron
    /// </summary>
    /// <param name="request">Patron registration details</param>
    /// <returns>Created patron information</returns>
    [HttpPost]
    [ProducesResponseType<PatronDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PatronDto>> RegisterPatron([FromBody] RegisterPatronRequest request)
    {
        var command = new RegisterPatronCommand(request.FullName, request.Email);
        
        var validationResult = await _registerPatronValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetPatron), new { id = result.Id }, result);
    }

    /// <summary>
    /// Get patron details with active loans
    /// </summary>
    /// <param name="id">Patron ID</param>
    /// <returns>Patron details with active loans</returns>
    [HttpGet("{id:guid}")]
    [ProducesResponseType<PatronDetailsDto>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PatronDetailsDto>> GetPatron(Guid id)
    {
        var query = new GetPatronQuery(id);
        var result = await _mediator.Send(query);
        
        if (result == null)
            return NotFound();
            
        return Ok(result);
    }
}

public record RegisterPatronRequest(string FullName, string Email);