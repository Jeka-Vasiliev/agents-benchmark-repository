using LibraryLending.Application.DTOs;
using MediatR;

namespace LibraryLending.Application.UseCases.Books.AddBook;

public record AddBookCommand(
    string Isbn,
    string Title,
    string Author,
    int TotalCopies) : IRequest<BookDto>;