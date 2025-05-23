using Raftel.Api.FunctionalTests.DemoApi;
using Raftel.Api.Server.AutoEndpoints;
using Raftel.Api.Server.Features.Tenants;
using Raftel.Api.Server.Features.Users;
using Raftel.Application;
using Raftel.Application.Features.Users.RegisterUser;
using Raftel.Application.Middlewares;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Demo.Application.Pirates.GetPirateById;
using Raftel.Demo.Infrastructure;
using Raftel.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddRaftelApplication(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly);
    cfg.RegisterServicesFromAssembly(typeof(RegisterUserCommand).Assembly);
    cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
    cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
});

builder.Services.AddSampleInfrastructure(builder.Configuration.GetConnectionString("Default")!);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();

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
        group.AddCommand<CreatePirateCommand>("", HttpMethod.Post);
    }
);

using var scope = app.Services.CreateScope();
await SeedData.InitializeAsync(scope.ServiceProvider);

app.Run();

namespace Raftel.Api.FunctionalTests.DemoApi
{
    public partial class Program
    {
    }
}