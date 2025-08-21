namespace LibraryLending.Application.DTOs;

public record LoanDto(
    Guid Id,
    Guid BookId,
    Guid PatronId,
    DateTime LoanedAt,
    DateTime DueAt,
    DateTime? ReturnedAt,
    bool IsReturned,
    bool IsOverdue);