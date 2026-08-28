using System.Text.Json;
using FidenzComfortIndex.Models;
using FidenzComfortIndex.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FidenzComfortIndex.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize] // Requires a valid Auth0-issued JWT — see Program.cs for JwtBearer config
    public class WeatherController : ControllerBase
    {
        private readonly IWeatherService _weatherService;
        private readonly IWebHostEnvironment _env;

        public WeatherController(IWeatherService weatherService, IWebHostEnvironment env)
        {
            _weatherService = weatherService;
            _env = env;
        }

        // GET /api/weather/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<List<CityComfortResult>>> GetDashboard()
        {
            var cities = LoadCities();
            var results = await _weatherService.GetRankedCitiesAsync(cities);
            return Ok(results);
        }

        private List<CityEntry> LoadCities()
        {
            var path = Path.Combine(_env.ContentRootPath, "Data", "cities.json");
            var json = System.IO.File.ReadAllText(path);
            var cities = JsonSerializer.Deserialize<List<CityEntry>>(json) ?? new();
            return cities;
        }
    }

    [ApiController]
    [Route("api/debug")]
    [Authorize]
    public class DebugController : ControllerBase
    {
        private readonly IWeatherCacheService _cacheService;

        public DebugController(IWeatherCacheService cacheService)
        {
            _cacheService = cacheService;
        }

        // GET /api/debug/cache/{cityId}
        [HttpGet("cache/{cityId}")]
        public ActionResult<CacheStatusResult> GetCacheStatus(int cityId)
        {
            return Ok(_cacheService.GetStatus(cityId));
        }
    }
}