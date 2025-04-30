using System.Diagnostics.CodeAnalysis;

namespace Raftel.Domain.Abstractions;

/// <summary>
/// Represents the result of an operation, encapsulating success or failure information.
/// </summary>
public class Result
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Result"/> class.
    /// </summary>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The error associated with the operation, if any.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the success state and error state are inconsistent.
    /// </exception>
    protected internal Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None)
        {
            throw new InvalidOperationException();
        }

        if (!isSuccess && error == Error.None)
        {
            throw new InvalidOperationException();
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    /// <summary>
    /// Gets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets a value indicating whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Gets the error associated with the operation, if any.
    /// </summary>
    public Error Error { get; }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    /// <returns>A successful result.</returns>
    public static Result Success() => new(true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error associated with the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result Failure(Error error) => new(false, error);

    /// <summary>
    /// Creates a successful result with a value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value returned by the operation.</param>
    /// <returns>A successful result containing the value.</returns>
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="error">The error associated with the failure.</param>
    /// <returns>A failed result.</returns>
    public static Result<TValue> Failure<TValue>(Error error) => new(default, false, error);

    /// <summary>
    /// Creates a result based on the provided value.
    /// </summary>
    /// <typeparam name="TValue">The type of the value.</typeparam>
    /// <param name="value">The value to evaluate.</param>
    /// <returns>
    /// A successful result if the value is not null; otherwise, a failed result with a null value error.
    /// </returns>
    public static Result<TValue> Create<TValue>(TValue? value) =>
        value is not null ? Success(value) : Failure<TValue>(Error.NullValue);
}

/// <summary>
/// Represents the result of an operation with a value.
/// </summary>
/// <typeparam name="TValue">The type of the value.</typeparam>
public class Result<TValue> : Result
{
    private readonly TValue? _value;

    /// <summary>
    /// Initializes a new instance of the <see cref="Result{TValue}"/> class.
    /// </summary>
    /// <param name="value">The value returned by the operation.</param>
    /// <param name="isSuccess">Indicates whether the operation was successful.</param>
    /// <param name="error">The error associated with the operation.</param>
    protected internal Result(TValue value, bool isSuccess, Error error)
        : base(isSuccess, error)
    {
        _value = value;
    }

    /// <summary>
    /// Gets the value returned by the operation, if successful.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the result represents a failure.
    /// </exception>
    [NotNull]
    public TValue Value => IsSuccess
        ? _value!
        : throw new InvalidOperationException("The value of a failure result can not be accessed.");

    /// <summary>
    /// Creates a successful result with the specified value.
    /// </summary>
    /// <param name="value">The value returned by the operation.</param>
    /// <returns>A successful result containing the value.</returns>
    public static Result<TValue> Success(TValue value) => new Result<TValue>(value, true, Error.None);

    /// <summary>
    /// Creates a failed result with the specified error.
    /// </summary>
    /// <param name="error">The error associated with the failure.</param>
    /// <returns>A failed result.</returns>
    public new static Result<TValue> Failure(Error error) => new Result<TValue>(default, false, error);

    /// <summary>
    /// Implicitly creates a result from the provided value.
    /// </summary>
    /// <param name="value">The value to create the result from.</param>
    public static implicit operator Result<TValue>(TValue value) => Create(value);
}