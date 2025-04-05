using System.Diagnostics.CodeAnalysis;

namespace Raftel.Inkventory.Core.Customers;

public sealed class FirstLastName : IEquatable<FirstLastName>
{
    private readonly string _value;

    [ExcludeFromCodeCoverage]
    public FirstLastName()
    {
        /* For ORM Purpose  */
    }

    public FirstLastName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("First last name cannot be empty.", nameof(value));

        _value = value;
    }

    public override string ToString() => _value;

    public bool Equals(FirstLastName? other)
    {
        if (other is null) return false;
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        if (obj is string objAsString)
        {
            return objAsString == _value;
        }

        return obj is FirstLastName other && Equals(other);
    }

    public override int GetHashCode() => _value.GetHashCode();

    public static implicit operator string(FirstLastName lastName) => lastName._value;
}