using System.Text.RegularExpressions;
using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Users;

public sealed record Email
{
    private static readonly Regex EmailRegex = new(
        @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private readonly string _value;

    private Email(string value)
    {
        _value = value;
    }

    public static Error InvalidFormatError => new("Email.Format", "Invalid email format");

    internal static Result<Email> Create(string value)
    {
        if (!EmailRegex.IsMatch(value))
        {
            return Result.Failure<Email>(InvalidFormatError);
        }

        return new Email(value);
    }

    public override string ToString() => _value;

    public static implicit operator Email(string email) => Create(email).Value;
    public static implicit operator string(Email email) => email.ToString();

    public static bool IsEmail(string value) => EmailRegex.IsMatch(value);
}