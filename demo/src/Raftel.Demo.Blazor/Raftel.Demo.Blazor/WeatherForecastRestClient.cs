namespace Raftel.Demo.Blazor;

public class WeatherForecastRestClient(HttpClient http)
{
    public async Task<WeatherForecastDto[]> GetListAsync() =>
        await http.GetFromJsonAsync<WeatherForecastDto[]>("WeatherForecast") ?? [];
}