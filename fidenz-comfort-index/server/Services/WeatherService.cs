using System.Text.Json;
using FidenzComfortIndex.Models;

namespace FidenzComfortIndex.Services
{
    public interface IWeatherService
    {
        Task<List<CityComfortResult>> GetRankedCitiesAsync(IEnumerable<CityEntry> cities);
    }

    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;
        private readonly IComfortIndexService _comfortIndexService;
        private readonly IWeatherCacheService _cacheService;
        private readonly string _apiKey;

        public WeatherService(
            HttpClient httpClient,
            IComfortIndexService comfortIndexService,
            IWeatherCacheService cacheService,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _comfortIndexService = comfortIndexService;
            _cacheService = cacheService;
            _apiKey = configuration["OpenWeatherMap:ApiKey"]
                ?? throw new InvalidOperationException("OpenWeatherMap:ApiKey is not configured.");
        }

        public async Task<List<CityComfortResult>> GetRankedCitiesAsync(IEnumerable<CityEntry> cities)
        {
            var tasks = cities.Select(FetchAndScoreCityAsync);
            var results = await Task.WhenAll(tasks);

            // Rank: highest comfort score = rank 1.
            var ranked = results
                .Where(r => r != null)
                .Select(r => r!)
                .OrderByDescending(r => r.ComfortScore)
                .ToList();

            for (int i = 0; i < ranked.Count; i++)
            {
                ranked[i].Rank = i + 1;
            }

            return ranked;
        }

        private async Task<CityComfortResult?> FetchAndScoreCityAsync(CityEntry city)
        {
            // Processed result cached first — skips recomputation entirely on a hit.
            if (_cacheService.TryGetProcessed(city.CityCode, out var cachedResult))
            {
                return cachedResult;
            }

            WeatherApiResponse? weather;
            if (!_cacheService.TryGetRaw(city.CityCode, out weather))
            {
                var url = $"https://api.openweathermap.org/data/2.5/weather?id={city.CityCode}&appid={_apiKey}";
                var response = await _httpClient.GetAsync(url);

                if (!response.IsSuccessStatusCode)
                {
                    return null; // skip cities that fail to fetch rather than fail the whole batch
                }

                var json = await response.Content.ReadAsStringAsync();
                weather = JsonSerializer.Deserialize<WeatherApiResponse>(json);

                if (weather == null) return null;

                _cacheService.SetRaw(city.CityCode, weather);
            }

            double tempCelsius = weather.Main.Temp - 273.15; // OWM default units are Kelvin

            double score = _comfortIndexService.CalculateScore(
                tempCelsius,
                weather.Main.Humidity,
                weather.Wind.Speed,
                weather.Clouds.All);

            var result = new CityComfortResult
            {
                CityId = weather.CityId,
                CityName = weather.CityName,
                Country = weather.Sys.Country,
                WeatherDescription = weather.Weather.FirstOrDefault()?.Description ?? "",
                WeatherIcon = weather.Weather.FirstOrDefault()?.Icon ?? "",
                TemperatureCelsius = Math.Round(tempCelsius, 1),
                Humidity = weather.Main.Humidity,
                WindSpeed = weather.Wind.Speed,
                Cloudiness = weather.Clouds.All,
                ComfortScore = score
            };

            _cacheService.SetProcessed(city.CityCode, result);

            return result;
        }
    }
}