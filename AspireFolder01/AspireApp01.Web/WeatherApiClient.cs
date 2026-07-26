namespace AspireApp01.Web;

public class WeatherApiClient(HttpClient httpClient)
{
    public async Task<WeatherForecast[]> GetWeatherAsync(int maxItems = 10, CancellationToken cancellationToken = default)
    {
        List<WeatherForecast>? forecasts = null;

        await foreach (var forecast in httpClient.GetFromJsonAsAsyncEnumerable<WeatherForecast>("/weatherforecast", cancellationToken))
        {
            if (forecasts?.Count >= maxItems)
            {
                break;
            }
            if (forecast is not null)
            {
                forecasts ??= [];
                forecasts.Add(forecast);
            }
        }

        return forecasts?.ToArray() ?? [];
    }
}

// Second typed client: reuses the same logic as WeatherApiClient,
// but receives an HttpClient configured towards "apiservice02".
public class WeatherApiClient2(HttpClient httpClient) : WeatherApiClient(httpClient);

// Third typed client: reuses the same logic as WeatherApiClient,
// but receives an HttpClient configured towards "apiservice03".
public class WeatherApiClientFromPython(HttpClient httpClient) : WeatherApiClient(httpClient);

public record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
