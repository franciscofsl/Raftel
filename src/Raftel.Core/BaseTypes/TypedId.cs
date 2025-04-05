namespace Raftel.Core.BaseTypes;

public abstract record TypedId<T> where T : notnull
{
    private readonly T _value;

    protected TypedId(T value)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value), "ID value cannot be null.");
        }

        if (value is Guid guid && guid == Guid.Empty)
        {
            throw new ArgumentException("Id Value cannot be empty.", nameof(value));
        }

        _value = value;
    }

    public override string? ToString() => _value.ToString();

    public static implicit operator T(TypedId<T> id) => id._value;

    public virtual bool Equals(TypedId<T>? other)
    {
        if (other is null || GetType() != other.GetType())
        {
            return false;
        }

        return EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    public override int GetHashCode() => HashCode.Combine(_value);
}