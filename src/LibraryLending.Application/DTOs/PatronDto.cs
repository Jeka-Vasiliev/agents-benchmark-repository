namespace LibraryLending.Application.DTOs;

public record PatronDto(
    Guid Id,
    string FullName,
    string Email,
    bool Active);

public record PatronDetailsDto(
    Guid Id,
    string FullName,
    string Email,
    bool Active,
    IEnumerable<ActiveLoanDto> ActiveLoans);

public record ActiveLoanDto(
    Guid Id,
    Guid BookId,
    string BookTitle,
    string BookAuthor,
    DateTime LoanedAt,
    DateTime DueAt,
    bool IsOverdue);