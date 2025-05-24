using Raftel.Domain.Abstractions;

namespace Raftel.Domain.ValueObjects;

public sealed record Code
{
    private const int MaxLength = 50;
    private readonly string _value;

    private Code(string value)
    {
        _value = value;
    }

    public static Result<Code> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return Result.Failure<Code>(new Error("Code.Required", "Code is required"));
        }

        if (value.Length > MaxLength)
        {
            return Result.Failure<Code>(new Error("Code.TooLong", $"Code must be less than {MaxLength} characters"));
        }

        return new Code(value);
    }

    public static implicit operator string(Code code) => code._value;

    public static explicit operator Code(string value) => 
        Create(value).IsSuccess ? Create(value).Value : throw new InvalidOperationException($"Invalid code: {value}");
} 