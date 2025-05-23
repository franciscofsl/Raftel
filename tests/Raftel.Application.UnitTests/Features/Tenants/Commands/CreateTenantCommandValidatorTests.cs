using Raftel.Application.Features.Tenants.CreateTenant;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Tenants.Commands;

public sealed class CreateTenantCommandValidatorTests
{
    private readonly CreateTenantCommandValidator _validator;

    public CreateTenantCommandValidatorTests()
    {
        _validator = new CreateTenantCommandValidator();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData(null)]
    public void Validate_Should_ReturnError_WhenNameIsNullOrWhiteSpace(string name)
    {
        var command = new CreateTenantCommand(name, "TEST", "Description");
        var result = _validator.Validate(command);
        result.Errors.ShouldContain(TenantErrors.NameRequired);
    }

    [Fact]
    public void Validate_Should_ReturnSuccess_WhenAllPropertiesAreValid()
    {
        var command = new CreateTenantCommand("Test Tenant", "TEST", "Description");
        var result = _validator.Validate(command);
        result.IsValid.ShouldBeTrue();
    }
} 