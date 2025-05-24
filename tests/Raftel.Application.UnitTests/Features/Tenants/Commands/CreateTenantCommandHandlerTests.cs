using Raftel.Application.Features.Tenants.CreateTenant;
using Raftel.Domain.Abstractions;
using Raftel.Domain.Features.Tenants;
using Raftel.Domain.ValueObjects;
using Shouldly;

namespace Raftel.Application.UnitTests.Features.Tenants.Commands;

public sealed class CreateTenantCommandHandlerTests
{
    private readonly ITenantsRepository _tenantsRepository = Substitute.For<ITenantsRepository>();
    private readonly CreateTenantCommandHandler _handler;

    public CreateTenantCommandHandlerTests()
    {
        _handler = new CreateTenantCommandHandler(_tenantsRepository);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenCodeIsInvalid()
    {
        var command = new CreateTenantCommand("Test Tenant", "", "Test Description");
        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.Code.ShouldBe("Code.Required");
        await _tenantsRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnFailure_WhenCodeIsNotUnique()
    {
        var command = new CreateTenantCommand("Test Tenant", "TEST", "Test Description");
        var code = Code.Create(command.Code);
        _tenantsRepository.CodeIsUniqueAsync(code.Value, Arg.Any<CancellationToken>())
            .Returns(false);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldBe(TenantErrors.DuplicatedCode);
        await _tenantsRepository.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task HandleAsync_Should_ReturnSuccess_And_AddTenant_WhenCodeIsValid()
    {
        var command = new CreateTenantCommand("Test Tenant", "TEST", "Test Description");
        var code = Code.Create(command.Code);
        _tenantsRepository.CodeIsUniqueAsync(code.Value, Arg.Any<CancellationToken>())
            .Returns(true);

        var result = await _handler.HandleAsync(command, CancellationToken.None);

        result.IsSuccess.ShouldBeTrue();
        await _tenantsRepository.Received(1)
            .AddAsync(Arg.Is<Tenant>(t => 
                t.Name == command.Name && 
                t.Code == command.Code && 
                t.Description == command.Description), 
            Arg.Any<CancellationToken>());
    }
} 