using LibraryLending.Domain.Exceptions;
using LibraryLending.Domain.ValueObjects;

namespace LibraryLending.Domain.Entities;

public class Book
{
    public Guid Id { get; private set; }
    public Isbn Isbn { get; private set; } = null!;
    public string Title { get; private set; } = string.Empty;
    public string Author { get; private set; } = string.Empty;
    public int TotalCopies { get; private set; }
    public int AvailableCopies { get; private set; }
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    // EF Core constructor
    private Book() { }

    public Book(Isbn isbn, string title, string author, int totalCopies)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new ArgumentException("Title cannot be null or empty.", nameof(title));
        
        if (string.IsNullOrWhiteSpace(author))
            throw new ArgumentException("Author cannot be null or empty.", nameof(author));
        
        if (totalCopies < 1)
            throw new ArgumentException("Total copies must be at least 1.", nameof(totalCopies));

        Id = Guid.NewGuid();
        Isbn = isbn;
        Title = title.Trim();
        Author = author.Trim();
        TotalCopies = totalCopies;
        AvailableCopies = totalCopies;
    }

    public void LoanCopy()
    {
        if (AvailableCopies <= 0)
            throw new BookUnavailableException(Id);

        AvailableCopies--;
    }

    public void ReturnCopy()
    {
        if (AvailableCopies >= TotalCopies)
            throw new InvalidOperationException("Cannot return more copies than total copies.");

        AvailableCopies++;
    }

    public bool IsAvailable => AvailableCopies > 0;
}