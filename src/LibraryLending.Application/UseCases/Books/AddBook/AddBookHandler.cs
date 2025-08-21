using LibraryLending.Application.DTOs;
using LibraryLending.Domain.Entities;
using LibraryLending.Domain.Repositories;
using LibraryLending.Domain.ValueObjects;
using MediatR;

namespace LibraryLending.Application.UseCases.Books.AddBook;

public class AddBookHandler : IRequestHandler<AddBookCommand, BookDto>
{
    private readonly IBookRepository _bookRepository;

    public AddBookHandler(IBookRepository bookRepository)
    {
        _bookRepository = bookRepository;
    }

    public async Task<BookDto> Handle(AddBookCommand request, CancellationToken cancellationToken)
    {
        var isbn = Isbn.Create(request.Isbn);
        var book = new Book(isbn, request.Title, request.Author, request.TotalCopies);
        
        await _bookRepository.AddAsync(book, cancellationToken);

        return new BookDto(
            book.Id,
            book.Isbn,
            book.Title,
            book.Author,
            book.TotalCopies,
            book.AvailableCopies,
            book.IsAvailable);
    }
}