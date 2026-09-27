using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Commands;
using Raftel.Application.Queries;
using Raftel.Demo.Application.Pirates;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Domain.Abstractions;
using Raftel.Infrastructure.Tests;
using Shouldly;

namespace Raftel.Application.IntegrationTests.Middlewares;

[Collection(IntegrationSqlServerTestCollection.Name)]
public class PermissionAuthorizationMiddlewareTests : IntegrationTestBase
{
    public PermissionAuthorizationMiddlewareTests(SqlServerTestContainerFixture fixture) : base(fixture)
    {
    }

    [Fact]
    public async Task CreatePirateCommand_WhenUserHasManagementPermission_ShouldSucceed()
    {
        CurrentUser.AddPermission(PiratesPermissions.Management);
        
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetRequiredService<ICommandDispatcher>();
            var command = new CreatePirateCommand("Zoro", 320000000);
            var result = await commandDispatcher.DispatchAsync<CreatePirateCommand, Guid>(command);
            
            result.IsSuccess.ShouldBeTrue();
        });
    }
    
    [Fact]
    public async Task CreatePirateCommand_WhenUserDoesNotHaveManagementPermission_ShouldReturnForbiddenResult()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetRequiredService<ICommandDispatcher>();

            var result = await commandDispatcher.DispatchAsync<CreatePirateCommand, Guid>(
                new CreatePirateCommand("Zoro", 320000000));

            result.IsFailure.ShouldBeTrue();
            result.Error.Type.ShouldBe(ErrorType.Forbidden);
        });
    }
    
    [Fact]
    public async Task GetPirateByFilterQuery_WhenUserHasViewPermission_ShouldSucceed()
    {
        CurrentUser.AddPermission(PiratesPermissions.View);
        
        await ExecuteScopedAsync(async sp =>
        {
            var queryDispatcher = sp.GetRequiredService<IQueryDispatcher>();
            var query = new GetPirateByFilterQuery(string.Empty, null);
            var result = await queryDispatcher.DispatchAsync<GetPirateByFilterQuery, GetPirateByFilterResponse>(query);
            
            result.IsSuccess.ShouldBeTrue();
        });
    }
    
    [Fact]
    public async Task GetPirateByFilterQuery_WhenUserDoesNotHaveViewPermission_ShouldReturnForbiddenResult()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var queryDispatcher = sp.GetRequiredService<IQueryDispatcher>();

            var result = await queryDispatcher.DispatchAsync<GetPirateByFilterQuery, GetPirateByFilterResponse>(
                new GetPirateByFilterQuery(string.Empty, null));

            result.IsFailure.ShouldBeTrue();
            result.Error.Type.ShouldBe(ErrorType.Forbidden);
        });
    }
    
    [Fact]
    public async Task MultiplePermissions_WhenUserHasAllRequiredPermissions_ShouldSucceed()
    {
        CurrentUser.AddPermission(PiratesPermissions.Management);
        CurrentUser.AddPermission(PiratesPermissions.View);
        
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetRequiredService<ICommandDispatcher>();
            var queryDispatcher = sp.GetRequiredService<IQueryDispatcher>();
            
            var createCommand = new CreatePirateCommand("Luffy", 1500000000);
            var createResult = await commandDispatcher.DispatchAsync<CreatePirateCommand, Guid>(createCommand);
            
            var queryResult = await queryDispatcher.DispatchAsync<GetPirateByFilterQuery, GetPirateByFilterResponse>(
                new GetPirateByFilterQuery("Luffy", null));
            
            createResult.IsSuccess.ShouldBeTrue();
            queryResult.IsSuccess.ShouldBeTrue();
            queryResult.Value.Pirates.ShouldContain(p => p.Name == "Luffy");
        });
    }
} 