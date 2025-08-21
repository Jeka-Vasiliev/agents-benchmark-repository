using LibraryLending.Application.DTOs;
using MediatR;

namespace LibraryLending.Application.UseCases.Books.GetBooks;

public record GetBooksQuery(
    int Page = 1,
    int PageSize = 20,
    string? Query = null) : IRequest<BookListDto>;