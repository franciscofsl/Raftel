using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Validators;

/// <summary>
/// Represents a base class for defining validation logic for a given model type.
/// Allows registering validation rules through the <see cref="EnsureThat"/> method,
/// and executing them via the <see cref="Validate"/> method.
/// </summary>
/// <remarks>
/// Convention: give each rule's <see cref="Error"/> a <c>Code</c> prefixed with the name of the
/// field it validates, e.g. <c>"Email.Invalid"</c>. <c>ValidationMiddleware</c> and
/// <c>ErrorResults.ToProblem</c> split the code on its first <c>.</c> to group failures by field in
/// the HTTP response; a code with no <c>.</c> still works but is grouped under an empty field key.
/// </remarks>
/// <typeparam name="TModel">The type of the model to validate.</typeparam>
public abstract class Validator<TModel>
{
    private readonly List<ValidationRule<TModel>> _rules = new();

    /// <summary>
    /// Validates the specified model against all registered rules.
    /// </summary>
    /// <param name="model">The model instance to validate.</param>
    /// <returns>
    /// A <see cref="ValidationResult"/> representing the outcome of the validation.
    /// Returns success if no errors are found, otherwise returns a failure result with collected errors.
    /// </returns>
    public ValidationResult Validate(TModel model)
    {
        var errors = new List<Error>();

        _rules.ForEach(rule =>
        {
            if (!rule.Validation(model))
            {
                errors.Add(rule.Error);
            }
        });

        return errors.Any()
            ? ValidationResult.Failure(errors)
            : ValidationResult.Success();
    }

    /// <summary>
    /// Registers a validation rule for the model.
    /// The rule is executed when <see cref="Validate"/> is called.
    /// </summary>
    /// <param name="func">A predicate that should return true if the model is valid.</param>
    /// <param name="error">The error to associate with the rule if the predicate returns false.</param>
    protected void EnsureThat(Func<TModel, bool> func, Error error)
    {
        _rules.Add(new ValidationRule<TModel>(func, error));
    }
}