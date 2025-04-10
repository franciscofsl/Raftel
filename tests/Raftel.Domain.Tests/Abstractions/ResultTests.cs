using System;
using Raftel.Domain.Abstractions;
using Shouldly;
using Xunit;

namespace Raftel.Domain.Tests.Abstractions;

public class ResultTests
{
    [Fact]
    public void Success_Should_Create_Successful_Result()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.IsFailure.ShouldBeFalse();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Failure_Should_Create_Failed_Result()
    {
        var error = new Error("ERR001", "Invalid data");
        var result = Result.Failure(error);

        result.IsSuccess.ShouldBeFalse();
        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void SuccessT_Should_Store_Value()
    {
        var result = Result.Success("One Piece");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("One Piece");
    }

    [Fact]
    public void FailureT_Should_Throw_When_Accessing_Value()
    {
        var result = Result.Failure<string>(Error.NullValue);

        result.IsFailure.ShouldBeTrue();

        Should.Throw<InvalidOperationException>(() =>
        {
            var _ = result.Value;
        });
    }

    [Fact]
    public void Create_With_Null_Value_Should_Return_Failure()
    {
        var result = Result.Create<string>(null);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(Error.NullValue);
    }

    [Fact]
    public void Create_With_NonNull_Value_Should_Return_Success()
    {
        var result = Result.Create("Luffy");

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Luffy");
    }

    [Fact]
    public void Implicit_Conversion_Should_Create_Successful_Result()
    {
        Result<string> result = "Zoro";

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBe("Zoro");
    }
}