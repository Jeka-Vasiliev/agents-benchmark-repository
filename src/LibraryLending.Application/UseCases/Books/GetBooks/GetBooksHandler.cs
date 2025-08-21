using LibraryLending.Application.DTOs;
using LibraryLending.Domain.Repositories;
using MediatR;

namespace LibraryLending.Application.UseCases.Books.GetBooks;

public class GetBooksHandler : IRequestHandler<GetBooksQuery, BookListDto>
{
    private readonly IBookRepository _bookRepository;

    public GetBooksHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookListDto> Handle(GetBooksQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        var books = await _bookRepository.GetAllAsync(page, pageSize, request.Query, cancellationToken);
        var totalCount = await _bookRepository.GetTotalCountAsync(request.Query, cancellationToken);
        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var bookDtos = books.Select(book => new BookDto(
            book.Id,
            book.Isbn,
            book.Title,
            book.Author,
            book.TotalCopies,
            book.AvailableCopies,
            book.IsAvailable));

        return new BookListDto(
            bookDtos,
            totalCount,
            page,
            pageSize,
            totalPages);
    }
}