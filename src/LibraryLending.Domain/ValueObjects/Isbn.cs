using System.Text.RegularExpressions;

namespace LibraryLending.Domain.ValueObjects;

public sealed record Isbn
{
    private static readonly Regex IsbnRegex = new(
        @"^(?:ISBN(?:-1[03])?:? )?(?=[0-9X]{10}$|(?=(?:[0-9]+[- ]){3})[- 0-9X]{13}$|97[89][0-9]{10}$|(?=(?:[0-9]+[- ]){4})[- 0-9]{17}$)(?:97[89][- ]?)?[0-9]{1,5}[- ]?[0-9]+[- ]?[0-9]+[- ]?[0-9X]$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Isbn(string value)
    {
        Value = value;
    }

    public static Isbn Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("ISBN cannot be null or empty.", nameof(value));

        var cleanValue = value.Trim().Replace("-", "").Replace(" ", "");
        
        if (cleanValue.Length < 10 || cleanValue.Length > 13)
            throw new ArgumentException("ISBN must be 10 or 13 characters long.", nameof(value));

        if (!IsbnRegex.IsMatch(value.Trim()))
            throw new ArgumentException("Invalid ISBN format.", nameof(value));

        return new Isbn(cleanValue.ToUpperInvariant());
    }

    public static implicit operator string(Isbn isbn) => isbn.Value;
    public override string ToString() => Value;
}