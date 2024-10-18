using Raftel.Blazor;
using Raftel.Blazor.Menu;
using Raftel.Blazor.Shared.Localization;
using Raftel.Demo.Blazor;
using Raftel.Demo.Blazor.Navigation;
using _Imports = Raftel.Demo.Blazor.Client._Imports;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

builder.Services.AddScoped(sp =>
    new HttpClient
    {
        BaseAddress = new Uri(builder.Configuration["BackendUrl"] ?? "https://localhost:5002")
    });
builder.Services.AddHttpClient();
builder.Services.AddSingleton<MenuDefinitionProvider, SamplesMenuDefinitionProvider>();

builder.Services.AddTransient<WeatherForecastRestClient>();

builder.AddRaftelBlazor();
builder.Services.AddGrpcService<ILanguageService>();
builder.Services.AddGrpcService<ITextResourceService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<SamplesApp>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(SnComponentBase).Assembly)
    .AddAdditionalAssemblies(typeof(_Imports).Assembly);

app.Run();