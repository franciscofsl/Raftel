using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;
using Raftel.Domain.Abstractions;

namespace Raftel.Application.Abstractions;

/// <summary>
/// Builds a failed <see cref="Result"/> or <see cref="Result{TValue}"/> for an arbitrary closed
/// generic <typeparamref name="TResponse"/> without a <c>case</c> per possible value type.
/// Reflects once per distinct <typeparamref name="TResponse"/> and caches the resulting compiled
/// delegate, so steady-state cost is a dictionary lookup plus a delegate invocation.
/// </summary>
internal static class ResultFactory
{
    private static readonly ConcurrentDictionary<Type, Func<Error, object>> Cache = new();

    /// <summary>
    /// Creates a failed <typeparamref name="TResponse"/> carrying <paramref name="error"/>.
    /// </summary>
    /// <typeparam name="TResponse">Either <see cref="Result"/> or a closed <see cref="Result{TValue}"/>.</typeparam>
    /// <param name="error">The error to attach to the failed result.</param>
    public static TResponse CreateFailure<TResponse>(Error error) where TResponse : Result =>
        (TResponse)Cache.GetOrAdd(typeof(TResponse), BuildFactory)(error);

    private static Func<Error, object> BuildFactory(Type responseType)
    {
        var errorParameter = Expression.Parameter(typeof(Error), "error");
        var failureMethod = ResolveFailureMethod(responseType);
        var call = Expression.Call(failureMethod, errorParameter);
        var convertToObject = Expression.Convert(call, typeof(object));

        return Expression.Lambda<Func<Error, object>>(convertToObject, errorParameter).Compile();
    }

    private static MethodInfo ResolveFailureMethod(Type responseType)
    {
        if (responseType == typeof(Result))
        {
            return typeof(Result).GetMethod(nameof(Result.Failure), genericParameterCount: 0, [typeof(Error)])!;
        }

        var valueType = responseType.GetGenericArguments()[0];

        return typeof(Result)
            .GetMethod(nameof(Result.Failure), genericParameterCount: 1, [typeof(Error)])!
            .MakeGenericMethod(valueType);
    }
}
