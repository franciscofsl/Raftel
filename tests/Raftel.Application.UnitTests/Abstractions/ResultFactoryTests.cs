using System.Collections.Concurrent;
using System.Reflection;
using Raftel.Application.Abstractions;
using Raftel.Domain.Abstractions;
using Shouldly;

namespace Raftel.Application.UnitTests.Abstractions;

public class ResultFactoryTests
{
    private static readonly Error TestError = Error.Failure("Test.Failure", "Something went wrong.");

    [Fact]
    public void CreateFailure_ForResult_ReturnsFailedResultWithError()
    {
        var result = ResultFactory.CreateFailure<Result>(TestError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TestError);
    }

    [Fact]
    public void CreateFailure_ForResultOfGuid_ReturnsFailedResultWithError()
    {
        var result = ResultFactory.CreateFailure<Result<Guid>>(TestError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TestError);
    }

    [Fact]
    public void CreateFailure_ForResultOfString_ReturnsFailedResultWithError()
    {
        var result = ResultFactory.CreateFailure<Result<string>>(TestError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TestError);
    }

    [Fact]
    public void CreateFailure_ForResultOfRecord_ReturnsFailedResultWithError()
    {
        var result = ResultFactory.CreateFailure<Result<TestRecord>>(TestError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TestError);
    }

    [Fact]
    public void CreateFailure_ForResultOfGenericNestedType_ReturnsFailedResultWithError()
    {
        var result = ResultFactory.CreateFailure<Result<List<TestRecord>>>(TestError);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(TestError);
    }

    [Fact]
    public void CreateFailure_CalledTwiceForSameResponseType_ReusesCachedDelegate()
    {
        ResultFactory.CreateFailure<Result<Guid>>(TestError);
        var cacheSizeAfterFirstCall = GetCache().Count;

        ResultFactory.CreateFailure<Result<Guid>>(TestError);
        var cacheSizeAfterSecondCall = GetCache().Count;

        cacheSizeAfterFirstCall.ShouldBe(cacheSizeAfterSecondCall);
        GetCache().ShouldContainKey(typeof(Result<Guid>));
    }

    private static ConcurrentDictionary<Type, Func<Error, object>> GetCache()
    {
        var field = typeof(ResultFactory).GetField("Cache", BindingFlags.NonPublic | BindingFlags.Static)!;
        return (ConcurrentDictionary<Type, Func<Error, object>>)field.GetValue(null)!;
    }

    private sealed record TestRecord(string Name);
}
