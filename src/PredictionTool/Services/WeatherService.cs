using Newtonsoft.Json;

public record WeatherData
{
    public DateTime DateTime { get; set; }
    public double Temperature { get; set; }
    public double Humidity { get; set; }
    // other weather data properties
}

public class OpenWeatherMapApi
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenWeatherMapApi(string apiKey)
    {
        _httpClient = new HttpClient();
        _apiKey = apiKey;
    }

    public async Task<WeatherData> GetWeatherData(string location, DateTime dateTime)
    {
        var unixDateTime = (int)dateTime.Subtract(new DateTime(1970, 1, 1)).TotalSeconds;
        var apiUrl = $"https://api.openweathermap.org/data/2.5/weather?q={location}&dt={unixDateTime}&appid={_apiKey}";

        var response = await _httpClient.GetAsync(apiUrl);
        var content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to get weather data: {content}");
        }

        var weatherData = JsonConvert.DeserializeObject<WeatherData>(content);

        return weatherData;
    }
}