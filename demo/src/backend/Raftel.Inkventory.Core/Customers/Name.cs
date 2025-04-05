using System.Diagnostics.CodeAnalysis;

namespace Raftel.Inkventory.Core.Customers;

public sealed class Name : IEquatable<Name>
{
    private readonly string _value;

    [ExcludeFromCodeCoverage]
    private Name()
    {
        /* For ORM Purpose  */
    }

    public Name(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException("Name cannot be empty.", nameof(value));

        _value = value;
    }

    public override string ToString() => _value;

    public bool Equals(Name? other)
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

        return obj is Name other && Equals(other);
    }

    public override int GetHashCode() => _value.GetHashCode();

    public static implicit operator string(Name name) => name._value;
}