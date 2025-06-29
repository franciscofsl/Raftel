using Raftel.Domain.Abstractions;
using Raftel.Domain.ValueObjects;
using Shouldly;
using Xunit;

namespace Raftel.Domain.Tests.ValueObjects;

public class EncryptedStringTests
{
    [Fact]
    public void Create_With_Valid_PlainText_Should_Return_Success()
    {
        // Arrange
        const string plainText = "Server=localhost;Database=MyDb;User Id=sa;Password=mypassword;";

        // Act
        var result = EncryptedString.Create(plainText);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldNotBeNull();
        result.Value.HasValue().ShouldBeTrue();
    }

    [Fact]
    public void Create_With_Null_PlainText_Should_Return_Failure()
    {
        // Act
        var result = EncryptedString.Create(null);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("EncryptedString.NullValue");
    }

    [Fact]
    public void Create_With_Empty_PlainText_Should_Return_Success_With_Empty_Value()
    {
        // Act
        var result = EncryptedString.Create(string.Empty);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.HasValue().ShouldBeFalse();
        result.Value.GetDecryptedValue().ShouldBe(string.Empty);
    }

    [Fact]
    public void Create_With_Short_EncryptionKey_Should_Return_Failure()
    {
        // Arrange
        const string plainText = "test";
        const string shortKey = "short";

        // Act
        var result = EncryptedString.Create(plainText, shortKey);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("EncryptedString.InvalidKey");
    }

    [Fact]
    public void GetDecryptedValue_Should_Return_Original_PlainText()
    {
        // Arrange
        const string originalText = "Server=localhost;Database=TenantDb;User Id=sa;Password=securepassword;";
        var encryptedString = EncryptedString.Create(originalText).Value;

        // Act
        var decryptedText = encryptedString.GetDecryptedValue();

        // Assert
        decryptedText.ShouldBe(originalText);
    }

    [Fact]
    public void GetEncryptedValue_Should_Return_Different_Value_Than_PlainText()
    {
        // Arrange
        const string plainText = "Server=localhost;Database=TenantDb;User Id=sa;Password=securepassword;";
        var encryptedString = EncryptedString.Create(plainText).Value;

        // Act
        var encryptedValue = encryptedString.GetEncryptedValue();

        // Assert
        encryptedValue.ShouldNotBe(plainText);
        encryptedValue.ShouldNotBeNullOrEmpty();
    }

    [Fact]
    public void ImplicitOperator_Should_Return_EncryptedValue()
    {
        // Arrange
        const string plainText = "test connection string";
        var encryptedString = EncryptedString.Create(plainText).Value;

        // Act
        string implicitValue = encryptedString;

        // Assert
        implicitValue.ShouldBe(encryptedString.GetEncryptedValue());
        implicitValue.ShouldNotBe(plainText);
    }

    [Fact]
    public void FromEncrypted_Should_Create_EncryptedString_From_Encrypted_Value()
    {
        // Arrange
        const string plainText = "original connection string";
        var originalEncrypted = EncryptedString.Create(plainText).Value;
        var encryptedValue = originalEncrypted.GetEncryptedValue();

        // Act
        var result = EncryptedString.FromEncrypted(encryptedValue);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.GetDecryptedValue().ShouldBe(plainText);
    }

    [Fact]
    public void FromEncrypted_With_Null_Should_Return_Failure()
    {
        // Act
        var result = EncryptedString.FromEncrypted(null);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("EncryptedString.NullValue");
    }

    [Fact]
    public void HasValue_Should_Return_True_For_NonEmpty_EncryptedString()
    {
        // Arrange
        var encryptedString = EncryptedString.Create("test").Value;

        // Act & Assert
        encryptedString.HasValue().ShouldBeTrue();
    }

    [Fact]
    public void HasValue_Should_Return_False_For_Empty_EncryptedString()
    {
        // Arrange
        var encryptedString = EncryptedString.Create(string.Empty).Value;

        // Act & Assert
        encryptedString.HasValue().ShouldBeFalse();
    }

    [Fact]
    public void Multiple_Encryptions_Of_Same_PlainText_Should_Produce_Different_EncryptedValues()
    {
        // Arrange
        const string plainText = "same connection string";

        // Act
        var encrypted1 = EncryptedString.Create(plainText).Value;
        var encrypted2 = EncryptedString.Create(plainText).Value;

        // Assert
        encrypted1.GetEncryptedValue().ShouldNotBe(encrypted2.GetEncryptedValue());
        encrypted1.GetDecryptedValue().ShouldBe(encrypted2.GetDecryptedValue());
    }

    [Fact]
    public void Custom_EncryptionKey_Should_Work_For_Encryption_And_Decryption()
    {
        // Arrange
        const string plainText = "connection string with custom key";
        const string customKey = "MyCustomEncryptionKey123456789012";

        // Act
        var encryptedString = EncryptedString.Create(plainText, customKey).Value;
        var decryptedText = encryptedString.GetDecryptedValue();

        // Assert
        decryptedText.ShouldBe(plainText);
    }

    [Fact]
    public void Long_ConnectionString_Should_Be_Encrypted_Successfully()
    {
        // Arrange
        var longConnectionString = new string('A', 1000) + 
            "Server=localhost;Database=VeryLongDatabaseName;User Id=veryLongUsername;Password=veryLongPassword123456789;";

        // Act
        var result = EncryptedString.Create(longConnectionString);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.GetDecryptedValue().ShouldBe(longConnectionString);
    }
}