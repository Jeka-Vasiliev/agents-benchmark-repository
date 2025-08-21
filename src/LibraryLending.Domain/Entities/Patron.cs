using LibraryLending.Domain.ValueObjects;

namespace LibraryLending.Domain.Entities;

public class Patron
{
    public Guid Id { get; private set; }
    public string FullName { get; private set; } = string.Empty;
    public Email Email { get; private set; } = null!;
    public bool Active { get; private set; }

    // EF Core constructor
    private Patron() { }

    public Patron(string fullName, Email email)
    {
        if (string.IsNullOrWhiteSpace(fullName))
            throw new ArgumentException("Full name cannot be null or empty.", nameof(fullName));

        Id = Guid.NewGuid();
        FullName = fullName.Trim();
        Email = email;
        Active = true;
    }

    public void Deactivate()
    {
        Active = false;
    }

    public void Activate()
    {
        Active = true;
    }
}