namespace Raftel.Domain.BaseTypes;

/// <summary>
/// Base class for identifiers that use <see cref="Guid"/> as the backing type.
/// </summary>
public abstract record TypedGuidId : TypedId<Guid>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TypedGuidId"/> class.
    /// </summary>
    /// <param name="value">The GUID value.</param>
    protected TypedGuidId(Guid value) : base(value) { }

    /// <summary>
    /// Generates a new GUID using version 7.
    /// </summary>
    public static Guid NewGuid() => Guid.CreateVersion7();
}