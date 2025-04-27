using Raftel.Api.Integration.Tests.Api;
using Raftel.Api.Integration.Tests.Api.Application.Pirates.GetPirateByFilter;
using Raftel.Api.Integration.Tests.Api.Application.Pirates.GetPirateById;
using Raftel.Api.Server.AutoEndpoints;
using Raftel.Application;
using Raftel.Application.Abstractions.Middlewares;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();

builder.Services.AddRaftelApplication(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(WeatherForecast).Assembly);
    cfg.AddGlobalMiddleware(typeof(ValidationMiddleware<,>));
    // cfg.AddCommandMiddleware(typeof(UnitOfWorkMiddleware<>));
});

// builder.Services.AddInfrastructure(builder.Configuration);
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

namespace Raftel.Api.Integration.Tests.Api
{
    public partial class Program
    {
    }
}