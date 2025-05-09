using Raftel.Api.Server.AutoEndpoints;
using Raftel.Application;
using Raftel.Application.Middlewares;
using Raftel.Demo.Application.Pirates.CreatePirate;
using Raftel.Demo.Application.Pirates.GetPirateByFilter;
using Raftel.Demo.Application.Pirates.GetPirateById;
using Raftel.Demo.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddRaftelApplication(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(CreatePirateCommand).Assembly);
    cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
    // cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
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

app.UseAuthorization();

app.MapControllers();

app.AddQueryEndpoint<GetPirateByIdQuery, GetPirateByIdResponse>("/api/pirates/{id}", HttpMethod.Get);
app.AddQueryEndpoint<GetPirateByFilterQuery, GetPirateByFilterResponse>("/api/pirates/", HttpMethod.Get);

app.Run();

namespace Raftel.Api.FunctionalTests.DemoApi
{
    public partial class Program
    {
    }
}