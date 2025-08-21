using FluentValidation;
using LibraryLending.Application.DTOs;
using LibraryLending.Application.UseCases.Loans.LoanBook;
using LibraryLending.Application.UseCases.Loans.ReturnBook;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LibraryLending.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LoansController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<LoanBookCommand> _loanBookValidator;
    private readonly IValidator<ReturnBookCommand> _returnBookValidator;

    public LoansController(
        IMediator mediator,
        IValidator<LoanBookCommand> loanBookValidator,
        IValidator<ReturnBookCommand> returnBookValidator)
    {
        _mediator = mediator;
        _loanBookValidator = loanBookValidator;
        _returnBookValidator = returnBookValidator;
    }

    /// <summary>
    /// Loan a book to a patron
    /// </summary>
    /// <param name="request">Loan request details</param>
    /// <returns>Created loan information</returns>
    [HttpPost]
    [ProducesResponseType<LoanDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LoanDto>> LoanBook([FromBody] LoanBookRequest request)
    {
        var command = new LoanBookCommand(request.BookId, request.PatronId);
        
        var validationResult = await _loanBookValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(LoanBook), new { }, result);
    }

    /// <summary>
    /// Return a loaned book
    /// </summary>
    /// <param name="loanId">Loan ID</param>
    /// <returns>No content on success</returns>
    [HttpPost("{loanId:guid}/return")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> ReturnBook(Guid loanId)
    {
        var command = new ReturnBookCommand(loanId);
        
        var validationResult = await _returnBookValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        await _mediator.Send(command);
        return NoContent();
    }
}

public record LoanBookRequest(Guid BookId, Guid PatronId);