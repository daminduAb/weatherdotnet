using System.Collections.Concurrent;
using FidenzComfortIndex.Models;
using Microsoft.Extensions.Caching.Memory;

namespace FidenzComfortIndex.Services
{
    public interface IWeatherCacheService
    {
        bool TryGetRaw(int cityId, out WeatherApiResponse? weather);
        void SetRaw(int cityId, WeatherApiResponse weather);

        bool TryGetProcessed(int cityId, out CityComfortResult? result);
        void SetProcessed(int cityId, CityComfortResult result);

        CacheStatusResult GetStatus(int cityId);
    }

    public class WeatherCacheService : IWeatherCacheService
    {
        private readonly IMemoryCache _cache;
        private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

        // ConcurrentDictionary: SetRaw is called from parallel tasks in GetRankedCitiesAsync,
        // so this needs to be safe for concurrent writes.
        private readonly ConcurrentDictionary<int, DateTimeOffset> _rawCachedAt = new();

        public WeatherCacheService(IMemoryCache cache)
        {
            _cache = cache;
        }

        private static string RawKey(int cityId) => $"weather:raw:{cityId}";
        private static string ProcessedKey(int cityId) => $"weather:processed:{cityId}";

        public bool TryGetRaw(int cityId, out WeatherApiResponse? weather)
        {
            return _cache.TryGetValue(RawKey(cityId), out weather);
        }

        public void SetRaw(int cityId, WeatherApiResponse weather)
        {
            _cache.Set(RawKey(cityId), weather, CacheDuration);
            _rawCachedAt[cityId] = DateTimeOffset.UtcNow; // now thread-safe
        }

        public bool TryGetProcessed(int cityId, out CityComfortResult? result)
        {
            return _cache.TryGetValue(ProcessedKey(cityId), out result);
        }

        public void SetProcessed(int cityId, CityComfortResult result)
        {
            _cache.Set(ProcessedKey(cityId), result, CacheDuration);
        }

        public CacheStatusResult GetStatus(int cityId)
        {
            bool hit = _cache.TryGetValue(RawKey(cityId), out WeatherApiResponse? _);
            _rawCachedAt.TryGetValue(cityId, out var cachedAt);

            return new CacheStatusResult
            {
                CityId = cityId,
                Status = hit ? "HIT" : "MISS",
                CachedAt = hit ? cachedAt : null,
                ExpiresAt = hit ? cachedAt.Add(CacheDuration) : null
            };
        }
    }
}