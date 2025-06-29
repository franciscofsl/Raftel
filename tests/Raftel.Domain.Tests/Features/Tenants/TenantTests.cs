using Raftel.Domain.Features.Tenants;
using Shouldly;
using Xunit;

namespace Raftel.Domain.Tests.Features.Tenants;

public class TenantTests
{
    [Fact]
    public void Create_With_Valid_Parameters_Should_Return_Success()
    {
        // Arrange
        const string name = "Test Tenant";
        const string code = "TEST_TENANT";
        const string description = "A test tenant";

        // Act
        var result = Tenant.Create(name, code, description);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe(name);
        // Check the string value using implicit conversion
        string codeValue = result.Value.Code;
        codeValue.ShouldBe(code);
        result.Value.Description.ShouldBe(description);
        result.Value.ConnectionString.ShouldNotBeNull();
        result.Value.GetConnectionString().ShouldBeNull(); // No connection string provided
    }

    [Fact]
    public void Create_With_ConnectionString_Should_Store_Encrypted_Value()
    {
        // Arrange
        const string name = "Test Tenant";
        const string code = "TEST_TENANT";
        const string description = "A test tenant";
        const string connectionString = "Server=localhost;Database=TenantDb;User Id=sa;Password=securepassword;";

        // Act
        var result = Tenant.Create(name, code, description, connectionString);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ConnectionString.ShouldNotBeNull();
        result.Value.ConnectionString.HasValue().ShouldBeTrue();
        result.Value.GetConnectionString().ShouldBe(connectionString);
        
        // Verify that the stored value is encrypted (different from plaintext)
        string encryptedValue = result.Value.ConnectionString;
        encryptedValue.ShouldNotBe(connectionString);
    }

    [Fact]
    public void Create_With_Empty_ConnectionString_Should_Return_Success()
    {
        // Arrange
        const string name = "Test Tenant";
        const string code = "TEST_TENANT";
        const string description = "A test tenant";

        // Act
        var result = Tenant.Create(name, code, description, string.Empty);

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.ConnectionString.ShouldNotBeNull();
        result.Value.ConnectionString.HasValue().ShouldBeFalse();
        result.Value.GetConnectionString().ShouldBeNull();
    }

    [Fact]
    public void Create_With_Invalid_Code_Should_Return_Failure()
    {
        // Arrange
        const string name = "Test Tenant";
        const string invalidCode = ""; // Invalid code
        const string description = "A test tenant";

        // Act
        var result = Tenant.Create(name, invalidCode, description);

        // Assert
        result.IsFailure.ShouldBeTrue();
        result.Error.Code.ShouldBe("Code.Required");
    }

    [Fact]
    public void GetConnectionString_Should_Return_Null_When_No_ConnectionString_Provided()
    {
        // Arrange
        var tenant = Tenant.Create("Test", "TEST", "Description").Value;

        // Act
        var connectionString = tenant.GetConnectionString();

        // Assert
        connectionString.ShouldBeNull();
    }

    [Fact]
    public void GetConnectionString_Should_Return_Decrypted_Value_When_ConnectionString_Provided()
    {
        // Arrange
        const string originalConnectionString = "Server=localhost;Database=MyTenantDb;User Id=admin;Password=mypassword;";
        var tenant = Tenant.Create("Test", "TEST", "Description", originalConnectionString).Value;

        // Act
        var connectionString = tenant.GetConnectionString();

        // Assert
        connectionString.ShouldBe(originalConnectionString);
    }

    [Fact]
    public void ConnectionString_Should_Be_Stored_Encrypted()
    {
        // Arrange
        const string plainConnectionString = "Server=localhost;Database=MyTenantDb;User Id=admin;Password=mypassword;";
        var tenant = Tenant.Create("Test", "TEST", "Description", plainConnectionString).Value;

        // Act
        string storedValue = tenant.ConnectionString; // Implicit conversion to encrypted value

        // Assert
        storedValue.ShouldNotBe(plainConnectionString);
        storedValue.ShouldNotBeNullOrEmpty();
        tenant.GetConnectionString().ShouldBe(plainConnectionString); // But decrypted value should match
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_With_Invalid_Name_Should_Still_Succeed(string name)
    {
        // Note: The current implementation doesn't validate name, only code is validated
        // This test documents the current behavior
        
        // Act
        var result = Tenant.Create(name, "VALID_CODE", "Description");

        // Assert
        result.IsSuccess.ShouldBeTrue();
        result.Value.Name.ShouldBe(name);
    }
}