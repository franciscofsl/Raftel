using Microsoft.Extensions.DependencyInjection;
using Raftel.Application.Commands;
using Raftel.Application.Exceptions;
using Raftel.Application.Queries;
using Raftel.Demo.Application.Pirates;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Shouldly;

namespace Raftel.Application.IntegrationTests.Middlewares;

public class PermissionAuthorizationMiddlewareTests : IntegrationTestBase
{
    [Fact]
    public async Task CreatePirateCommand_WhenUserHasManagementPermission_ShouldSucceed()
    {
        CurrentUser.AddPermission(PiratesPermissions.Management);
        
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetRequiredService<ICommandDispatcher>();
            var command = new CreatePirateCommand("Zoro", 320000000);
            var result = await commandDispatcher.DispatchAsync(command);
            
            result.IsSuccess.ShouldBeTrue();
        });
    }
    
    [Fact]
    public async Task CreatePirateCommand_WhenUserDoesNotHaveManagementPermission_ShouldThrowUnauthorizedException()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var commandDispatcher = sp.GetRequiredService<ICommandDispatcher>();
            
            await Should.ThrowAsync<UnauthorizedException>(async () => 
                await commandDispatcher.DispatchAsync(new CreatePirateCommand("Zoro", 320000000)));
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
    public async Task GetPirateByFilterQuery_WhenUserDoesNotHaveViewPermission_ShouldThrowUnauthorizedException()
    {
        await ExecuteScopedAsync(async sp =>
        {
            var queryDispatcher = sp.GetRequiredService<IQueryDispatcher>();
            
            await Should.ThrowAsync<UnauthorizedException>(async () => 
                await queryDispatcher.DispatchAsync<GetPirateByFilterQuery, GetPirateByFilterResponse>(
                    new GetPirateByFilterQuery(string.Empty, null)));
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
            var createResult = await commandDispatcher.DispatchAsync(createCommand);
            
            var queryResult = await queryDispatcher.DispatchAsync<GetPirateByFilterQuery, GetPirateByFilterResponse>(
                new GetPirateByFilterQuery("Luffy", null));
            
            createResult.IsSuccess.ShouldBeTrue();
            queryResult.IsSuccess.ShouldBeTrue();
            queryResult.Value.Pirates.ShouldContain(p => p.Name == "Luffy");
        });
    }
} 