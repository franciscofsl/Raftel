using System.IO.Compression;
using ProtoBuf.Grpc.Server;
using Raftel.Demo.Server;
using Raftel.Server;
using Raftel.Shared.Modules;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCodeFirstGrpc(config => { config.ResponseCompressionLevel = CompressionLevel.Optimal; });

builder.Services.AddRaftelApplication<DemoApplication>(builder.Configuration);

var app = builder.Build();

// Map GRPC Services using MapGrpcService<>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.ConfigureRaftelWebApp();

app.UseHttpsRedirection();

app.UseAuthorization();
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI(c => { c.SwaggerEndpoint("/swagger/v1/swagger.json", "Blazor API V1"); });

app.Run();