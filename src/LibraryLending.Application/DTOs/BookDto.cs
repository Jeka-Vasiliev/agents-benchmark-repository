namespace LibraryLending.Application.DTOs;

public record BookDto(
    Guid Id,
    string Isbn,
    string Title,
    string Author,
    int TotalCopies,
    int AvailableCopies,
    bool IsAvailable);

public record BookListDto(
    IEnumerable<BookDto> Books,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages);