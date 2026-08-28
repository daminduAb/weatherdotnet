using System.Text.Json.Serialization;

namespace FidenzComfortIndex.Models
{
    // Maps directly onto OpenWeatherMap's /data/2.5/weather response shape.
    // Only the fields we actually use for the Comfort Index or display are included.
    public class WeatherApiResponse
    {
        [JsonPropertyName("id")]
        public int CityId { get; set; }

        [JsonPropertyName("name")]
        public string CityName { get; set; } = string.Empty;

        [JsonPropertyName("weather")]
        public List<WeatherDescription> Weather { get; set; } = new();

        [JsonPropertyName("main")]
        public MainWeatherData Main { get; set; } = new();

        [JsonPropertyName("wind")]
        public WindData Wind { get; set; } = new();

        [JsonPropertyName("clouds")]
        public CloudData Clouds { get; set; } = new();

        [JsonPropertyName("visibility")]
        public int Visibility { get; set; }

        [JsonPropertyName("sys")]
        public SysData Sys { get; set; } = new();
    }

    public class WeatherDescription
    {
        [JsonPropertyName("main")]
        public string Main { get; set; } = string.Empty;

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("icon")]
        public string Icon { get; set; } = string.Empty;
    }

    public class MainWeatherData
    {
        [JsonPropertyName("temp")]
        public double Temp { get; set; } // Kelvin, as returned by OWM

        [JsonPropertyName("feels_like")]
        public double FeelsLike { get; set; }

        [JsonPropertyName("humidity")]
        public int Humidity { get; set; } // %

        [JsonPropertyName("pressure")]
        public int Pressure { get; set; } // hPa
    }

    public class WindData
    {
        [JsonPropertyName("speed")]
        public double Speed { get; set; } // m/s
    }

    public class CloudData
    {
        [JsonPropertyName("all")]
        public int All { get; set; } // % cloudiness
    }

    public class SysData
    {
        [JsonPropertyName("country")]
        public string Country { get; set; } = string.Empty;
    }

    // What cities.json entries look like — trim/extend to match the real file.
    public class CityEntry
    {
        public int CityCode { get; set; }
        public string? Name { get; set; }
    }

    // The final shape returned to the Angular client for one city.
    public class CityComfortResult
    {
        public int CityId { get; set; }
        public string CityName { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string WeatherDescription { get; set; } = string.Empty;
        public string WeatherIcon { get; set; } = string.Empty;
        public double TemperatureCelsius { get; set; }
        public int Humidity { get; set; }
        public double WindSpeed { get; set; }
        public int Cloudiness { get; set; }
        public double ComfortScore { get; set; }
        public int Rank { get; set; }
    }

    public class CacheStatusResult
    {
        public int CityId { get; set; }
        public string Status { get; set; } = "MISS"; // HIT | MISS
        public DateTimeOffset? CachedAt { get; set; }
        public DateTimeOffset? ExpiresAt { get; set; }
    }
}