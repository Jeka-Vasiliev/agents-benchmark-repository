using FluentValidation;
using LibraryLending.Application.DTOs;
using LibraryLending.Application.UseCases.Books.AddBook;
using LibraryLending.Application.UseCases.Books.GetBooks;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace LibraryLending.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class BooksController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<AddBookCommand> _addBookValidator;

    public BooksController(IMediator mediator, IValidator<AddBookCommand> addBookValidator)
    {
        _mediator = mediator;
        _addBookValidator = addBookValidator;
    }

    /// <summary>
    /// Add a new book to the library
    /// </summary>
    /// <param name="request">Book details</param>
    /// <returns>Created book information</returns>
    [HttpPost]
    [ProducesResponseType<BookDto>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<BookDto>> AddBook([FromBody] AddBookRequest request)
    {
        var command = new AddBookCommand(request.Isbn, request.Title, request.Author, request.TotalCopies);
        
        var validationResult = await _addBookValidator.ValidateAsync(command);
        if (!validationResult.IsValid)
            throw new ValidationException(validationResult.Errors);

        var result = await _mediator.Send(command);
        return CreatedAtAction(nameof(GetBooks), new { }, result);
    }

    /// <summary>
    /// Get books with pagination and search
    /// </summary>
    /// <param name="page">Page number (default: 1)</param>
    /// <param name="pageSize">Page size (default: 20, max: 100)</param>
    /// <param name="query">Search query for title, author, or ISBN</param>
    /// <returns>Paginated list of books</returns>
    [HttpGet]
    [ProducesResponseType<BookListDto>(StatusCodes.Status200OK)]
    public async Task<ActionResult<BookListDto>> GetBooks(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? query = null)
    {
        var getBooksQuery = new GetBooksQuery(page, pageSize, query);
        var result = await _mediator.Send(getBooksQuery);
        return Ok(result);
    }
}

public record AddBookRequest(string Isbn, string Title, string Author, int TotalCopies);