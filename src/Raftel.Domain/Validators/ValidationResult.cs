using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Validators;

/// <summary>
/// Represents the result of a validation process, containing any validation errors found.
/// </summary>
public class ValidationResult
{
    private readonly List<Error> _errors = new();

    /// <summary>
    /// Gets the collection of validation errors.
    /// </summary>
    public IReadOnlyList<Error> Errors => _errors;

    /// <summary>
    /// Gets a value indicating whether the validation passed without any errors.
    /// </summary>
    public bool IsValid => !_errors.Any();

    /// <summary>
    /// Private constructor to enforce static creation methods.
    /// </summary>
    private ValidationResult() { }

    /// <summary>
    /// Creates a successful <see cref="ValidationResult"/> instance with no errors.
    /// </summary>
    /// <returns>A result representing a valid state.</returns>
    public static ValidationResult Success() => new();

    /// <summary>
    /// Creates a failed <see cref="ValidationResult"/> instance with the specified errors.
    /// </summary>
    /// <param name="errors">A collection of <see cref="Error"/> instances representing the validation failures.</param>
    /// <returns>A result representing a failed state with associated errors.</returns>
    public static ValidationResult Failure(IEnumerable<Error> errors)
    {
        var result = new ValidationResult();
        result._errors.AddRange(errors);
        return result;
    }
}