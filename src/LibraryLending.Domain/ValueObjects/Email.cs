using System.Text.RegularExpressions;

namespace LibraryLending.Domain.ValueObjects;

public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public string Value { get; }

    private Email(string value)
    {
        Value = value;
    }

    public static Email Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Email cannot be null or empty.", nameof(value));

        var trimmedValue = value.Trim();
        if (!EmailRegex.IsMatch(trimmedValue))
            throw new ArgumentException("Invalid email format.", nameof(value));

        return new Email(trimmedValue.ToLowerInvariant());
    }

    public static implicit operator string(Email email) => email.Value;
    public override string ToString() => Value;
}