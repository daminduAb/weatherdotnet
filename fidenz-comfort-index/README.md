# Comfort Index — Weather Analytics Dashboard

Angular + ASP.NET Core weather analytics app with a custom Comfort Index score, server-side caching, and Auth0-secured access.

## Setup

### Backend (server/)
1. `cd server`
2. Add your OpenWeatherMap key and Auth0 config to `appsettings.Development.json`:
   ```json
   {
     "OpenWeatherMap": { "ApiKey": "YOUR_KEY" },
     "Auth0": { "Domain": "your-tenant.us.auth0.com", "Audience": "https://fidenz-comfort-index-api" }
   }
   ```
3. `dotnet restore && dotnet run`

### Frontend (client/)
1. `cd client`
2. `npm install`
3. Update Auth0 `domain` / `clientId` in `src/app/app.config.ts`
4. `npm start` → http://localhost:4200

## Comfort Index Formula

`Score = 100 − TempPenalty − HumidityPenalty − WindPenalty − CloudPenalty`, clamped to [0, 100].

| Parameter   | Weight | Reasoning |
|-------------|--------|-----------|
| Temperature | 40%    | Dominant driver of perceived comfort; penalized at 2.5 pts per °C away from an ideal 22.5°C midpoint. |
| Humidity    | 30%    | 10-point comfort buffer around 50%; humidity mainly affects how temperature *feels*, so it's weighted below temperature itself. |
| Wind Speed  | 20%    | Free up to 3 m/s (light breeze cools pleasantly); above that, each m/s costs 4 pts as it starts to feel disruptive. |
| Cloudiness  | 10%    | Smallest weight — affects mood/aesthetics more than physical comfort. |

**Trade-offs considered:** a multiplicative model (factors multiplied instead of subtracted) was rejected because a single bad parameter could crater the whole score; the additive, independently-capped model keeps each factor's worst-case contribution bounded and easier to reason about/explain live.

## Caching Design

- `IMemoryCache`, two namespaces: `weather:raw:{cityId}` (raw OWM response) and `weather:processed:{cityId}` (computed result), each with a 5-minute absolute expiration.
- Processed cache is checked first — a hit skips both the API call and the scoring computation entirely.
- `GET /api/debug/cache/{cityId}` reports HIT/MISS + cache timestamps without populating the cache itself.

## Known Limitations

- Dew point isn't directly available from the current-weather endpoint and isn't used in v1 of the formula (candidate parameter to add live in the screen recording).
- `IMemoryCache` is per-instance — would need a distributed cache (Redis) behind a load balancer.
- No retry/backoff on OpenWeatherMap failures; failed cities are silently excluded from the ranking.
