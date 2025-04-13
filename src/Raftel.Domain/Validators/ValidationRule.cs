using Raftel.Domain.Abstractions;

namespace Raftel.Domain.Validators;

/// <summary>
/// Represents a validation rule for a specific model type.
/// Each rule consists of a condition to evaluate and an error to return if the condition fails.
/// </summary>
/// <typeparam name="TModel">The type of the model being validated.</typeparam>
/// <param name="Validation">A predicate that determines whether the model passes the rule.</param>
/// <param name="Error">The <see cref="Error"/> to return if the validation fails.</param>
internal record ValidationRule<TModel>(Func<TModel, bool> Validation, Error Error);