namespace Raftel.Domain.Abstractions;

/// <summary>
/// Aggregates every failed <see cref="Validators.Validator{TModel}"/> rule for a single request into
/// one <see cref="Error"/>, without losing per-rule detail.
/// </summary>
/// <param name="Errors">The individual errors that failed validation, in the order they were evaluated.</param>
public sealed record ValidationError(IReadOnlyList<Error> Errors)
    : Error("Validation.General", "One or more validation errors occurred.", ErrorType.Validation)
{
    /// <summary>
    /// Builds a <see cref="ValidationError"/> aggregating the given errors.
    /// </summary>
    /// <param name="errors">The errors to aggregate.</param>
    public static ValidationError FromErrors(IEnumerable<Error> errors) => new(errors.ToList());

    /// <summary>
    /// Compares by value, including a structural (not reference) comparison of <see cref="Errors"/> —
    /// the compiler-synthesized record equality would otherwise compare the <see cref="IReadOnlyList{T}"/>
    /// by reference, since <see cref="List{T}"/> does not itself implement value equality.
    /// </summary>
    public bool Equals(ValidationError? other) =>
        other is not null && base.Equals(other) && Errors.SequenceEqual(other.Errors);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(base.GetHashCode());
        foreach (var error in Errors)
        {
            hash.Add(error);
        }

        return hash.ToHashCode();
    }
}
