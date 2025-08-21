using LibraryLending.Domain.Exceptions;

namespace LibraryLending.Domain.Entities;

public class Loan
{
    public Guid Id { get; private set; }
    public Guid BookId { get; private set; }
    public Guid PatronId { get; private set; }
    public DateTime LoanedAt { get; private set; }
    public DateTime DueAt { get; private set; }
    public DateTime? ReturnedAt { get; private set; }

    // Navigation properties
    public Book Book { get; private set; } = null!;
    public Patron Patron { get; private set; } = null!;

    // EF Core constructor
    private Loan() { }

    public Loan(Guid bookId, Guid patronId, DateTime loanedAt)
    {
        Id = Guid.NewGuid();
        BookId = bookId;
        PatronId = patronId;
        LoanedAt = loanedAt;
        DueAt = loanedAt.AddDays(14); // 14 days loan period
        ReturnedAt = null;
    }

    public void Return(DateTime returnedAt)
    {
        if (IsReturned)
            throw new LoanAlreadyReturnedException(Id);

        ReturnedAt = returnedAt;
    }

    public bool IsReturned => ReturnedAt.HasValue;
    public bool IsOverdue => !IsReturned && DateTime.UtcNow > DueAt;
}