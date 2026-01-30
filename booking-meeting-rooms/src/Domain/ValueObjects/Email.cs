using System.Text.RegularExpressions;
using BookingMeetingRooms.Domain.Common;

namespace BookingMeetingRooms.Domain.ValueObjects;

public class Email : ValueObject
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public string Value { get; private set; } = string.Empty;

    private Email() { }

    public Email(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email cannot be empty", nameof(email));

        if (!EmailRegex.IsMatch(email))
            throw new ArgumentException("Invalid email format", nameof(email));

        Value = email.ToLowerInvariant();
    }

    public static Email Create(string email) => new(email);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;
}
