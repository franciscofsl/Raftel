using Raftel.Api.FunctionalTests.DemoApi;
using Raftel.Api.FunctionalTests.DemoApi.TestSupport;
using Raftel.Api.Server.AutoEndpoints;
using Raftel.Api.Server.Features.Tenants;
using Raftel.Api.Server.Features.Users;
using Raftel.Api.Server.Health;
using Raftel.Api.Server.Middlewares;
using Raftel.Application;
using Raftel.Application.Exceptions;
using Raftel.Application.Features.Users.RegisterUser;
using Raftel.Application.Middlewares;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Demo.Application.Pirates.GetPirateById;
using Raftel.Demo.Application.Pirates.ListPirates;
using Raftel.Demo.Infrastructure;
using Raftel.Demo.Infrastructure.Data;
using Raftel.Domain.Abstractions;
using Raftel.Infrastructure;
using Raftel.Infrastructure.Health;
using Raftel.Infrastructure.Multitenancy.Middleware;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddRaftelApplication(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(CreateTestResourceCommand).Assembly);
    cfg.AddGlobalMiddleware(typeof(LoggingMiddleware<,>));
    cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
    cfg.AddGlobalMiddleware(typeof(AuditLogMiddleware<,>));
    cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
    cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<,>));
});

builder.Services.AddSampleInfrastructure(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddRaftelHealthChecks(options => options.EnableTenantResolutionCheck = true);
builder.Services.AddSingleton<DatabaseFailureSwitch>();
builder.Services.AddScoped<IDatabaseProbe>(sp =>
{
    var inner = new DatabaseProbe<TestingRaftelDbContext>(sp.GetRequiredService<TestingRaftelDbContext>());
    return new SwitchableDatabaseProbe(inner, sp.GetRequiredService<DatabaseFailureSwitch>());
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCorrelationId();
app.UseRaftelExceptionHandling();
app.UseHttpsRedirection();
app.UseRouting();

app.UseTenantMiddleware();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.AddRaftelTenants();
app.AddRaftelUsers();
app.AddEndpointGroup(group =>
    {
        group.Name = "Pirates";
        group.BaseUri = "/api/pirates";
        group.AddQuery<GetPirateByIdQuery, GetPirateByIdResponse>("{id}", HttpMethod.Get);
        group.AddQuery<GetPirateByFilterQuery, GetPirateByFilterResponse>("", HttpMethod.Get);
        group.AddQuery<ListPiratesQuery, PagedResult<PirateSummary>>("paged", HttpMethod.Get);
        group.AddCommand<CreatePirateCommand, Guid>("", HttpMethod.Post, createdRouteName: "GET_GetPirateByIdQuery");
    }
);

app.AddEndpointGroup(group =>
    {
        group.Name = "TestResources";
        group.BaseUri = "/api/test/resources";
        group.AddCommand<CreateTestResourceCommand, Guid>("", HttpMethod.Post);
    }
);

app.AddEndpointGroup(group =>
    {
        group.Name = "TestCancellation";
        group.BaseUri = "/api/test/cancellation";
        group.AddCommand<AwaitCancellationCommand>("", HttpMethod.Post);
    }
);

app.MapGet("/api/test/throw", () => { throw new InvalidOperationException("Test unhandled exception."); });

app.MapGet("/api/test/throw/validation",
    () => { throw new ValidationException([Error.Validation("Test.Field", "Field is required")]); });

app.MapGet("/api/test/throw/unauthorized",
    () => { throw new UnauthorizedException("User does not have the required permission: test.permission"); });

app.MapGet("/api/test/error/{type}", (string type) => ErrorResults.ToProblem(type switch
{
    "not-found" => Error.NotFound("Test.NotFound", "Resource not found"),
    "conflict" => Error.Conflict("Test.Conflict", "Conflict occurred"),
    "validation" => Error.Validation("Test.Validation", "Validation failed"),
    "forbidden" => Error.Forbidden("Test.Forbidden", "Forbidden"),
    "unauthorized" => Error.Unauthorized("Test.Unauthorized", "Unauthorized"),
    "failure" => Error.Failure("Test.Failure", "Unclassified failure"),
    _ => throw new NotSupportedException($"Unknown error type '{type}'")
}));

app.MapPost("/api/test/database-failure/enable", (DatabaseFailureSwitch databaseFailureSwitch) =>
{
    databaseFailureSwitch.Enable();
    return Results.NoContent();
});

app.MapPost("/api/test/database-failure/disable", (DatabaseFailureSwitch databaseFailureSwitch) =>
{
    databaseFailureSwitch.Disable();
    return Results.NoContent();
});

app.MapRaftelHealthChecks();

using var scope = app.Services.CreateScope();
await SeedData.InitializeAsync(scope.ServiceProvider);

app.Run();

namespace Raftel.Api.FunctionalTests.DemoApi
{
    public partial class Program
    {
    }
}