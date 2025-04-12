namespace Raftel.Domain.BaseTypes;

/// <summary>
/// Represents a strongly-typed identifier based on a primitive type.
/// </summary>
/// <typeparam name="T">The underlying primitive type (e.g., Guid, int).</typeparam>
public abstract record TypedId<T> where T : notnull
{
    private readonly T _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="TypedId{T}"/> class.
    /// </summary>
    /// <param name="value">The raw value of the identifier.</param>
    /// <exception cref="ArgumentNullException">Thrown when value is null.</exception>
    /// <exception cref="ArgumentException">Thrown when value is Guid.Empty.</exception>
    protected TypedId(T value)
    {
        if (value is null)
            throw new ArgumentNullException(nameof(value), "ID value cannot be null.");

        if (value is Guid guid && guid == Guid.Empty)
            throw new ArgumentException("ID value cannot be empty.", nameof(value));

        _value = value;
    }

    /// <inheritdoc/>
    public override string? ToString() => _value.ToString();

    /// <summary>
    /// Implicitly converts a <see cref="TypedId{T}"/> to its underlying value.
    /// </summary>
    public static implicit operator T(TypedId<T> id) => id._value;

    /// <inheritdoc/>
    public virtual bool Equals(TypedId<T>? other)
    {
        if (other is null || GetType() != other.GetType()) return false;
        return EqualityComparer<T>.Default.Equals(_value, other._value);
    }

    /// <inheritdoc/>
    public override int GetHashCode() => HashCode.Combine(_value);
}