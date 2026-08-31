# Comfort Index — Weather Analytics Dashboard

A full-stack weather analytics app that fetches live weather data, scores each city with a custom Comfort Index (0–100), and ranks cities from most to least comfortable. Built with **Angular** (frontend) and **ASP.NET Core** (backend), secured with **Auth0** (login, MFA, whitelisted access).

---

## Tech Stack

- **Frontend:** Angular (standalone components), Tailwind CSS, `@auth0/auth0-angular`
- **Backend:** ASP.NET Core Web API (.NET 8), `IMemoryCache`, JWT Bearer authentication
- **Auth:** Auth0 (Authorization Code + PKCE flow, email MFA, whitelisted signups)
- **Weather data:** OpenWeatherMap Current Weather API

---

## Setup Instructions

### Prerequisites
- Node.js (LTS) + Angular CLI (`npm install -g @angular/cli`)
- .NET 8 SDK
- An Auth0 tenant (free tier is fine)
- An OpenWeatherMap API key (free tier — note new keys can take up to ~2 hours to activate)

### Backend (`server/`)

1. `cd server`
2. Add your credentials to `appsettings.Development.json` (not committed — create it yourself):
   ```json
   {
     "OpenWeatherMap": {
       "ApiKey": "YOUR_OPENWEATHERMAP_KEY"
     },
     "Auth0": {
       "Domain": "your-tenant.us.auth0.com",
       "Audience": "https://fidenz-comfort-index-api"
     }
   }
   ```
3. Make sure `Data/cities.json` exists with at least 10 cities (see format below).
4. `dotnet restore`
5. `dotnet run`

### Frontend (`client/`)

1. `cd client`
2. `npm install`
3. `npm install -D tailwindcss postcss autoprefixer`
4. `npm install @auth0/auth0-angular`
5. Update `domain`, `clientId`, and `audience` in `src/app/app.config.ts` with your Auth0 values.
6. Create `client/proxy.conf.json` so API calls reach the .NET backend during development:
   ```json
   {
     "/api": {
       "target": "http://localhost:5248",
       "secure": false,
       "changeOrigin": true
     }
   }
   ```
   (adjust the port to match whatever `dotnet run` prints)
7. `ng serve` → visit `http://localhost:4200/dashboard`

### `cities.json` format

```json
[
  { "CityCode": 1248991, "Name": "Colombo" },
  { "CityCode": 1850147, "Name": "Tokyo" },
  { "CityCode": 2988507, "Name": "Paris" },
  { "CityCode": 2147714, "Name": "Sydney" },
  { "CityCode": 4930956, "Name": "Boston" },
  { "CityCode": 1796236, "Name": "Shanghai" },
  { "CityCode": 3143244, "Name": "Oslo" },
  { "CityCode": 292223,  "Name": "Dubai" },
  { "CityCode": 2172797, "Name": "Cairns" },
  { "CityCode": 2644210, "Name": "Liverpool" }
]
```
City IDs come from OpenWeatherMap's public city list, searchable at openweathermap.org.

### Auth0 tenant setup

1. **Applications → Create Application** → Single Page Web Application. Copy its Domain and Client ID into `app.config.ts`.
2. Set **Allowed Callback URLs**, **Allowed Logout URLs**, and **Allowed Web Origins** to `http://localhost:4200`.
3. **Applications → APIs → Create API** with identifier `https://fidenz-comfort-index-api` (must match exactly on both frontend and backend). Under **Application Access**, grant your SPA access to this API.
4. **Security → Multi-Factor Auth** → enable **Email**, policy set to "Always Require".
5. **Authentication → Database** → your connection → **Settings** → enable **Disable Sign Ups**.
6. **User Management → Users → Create User** to add the whitelisted test account:
   - Email: `careers@fidenz.com`
   - Password: `Pass#fidenz`
   - (Password Policy on the connection must allow this — set it to "Fair" or lower, not "Good"/"Excellent", which require 11+ characters.)

---

## Comfort Index Formula

```
Score = 100 − TemperaturePenalty − HumidityPenalty − WindPenalty − CloudinessPenalty
```
Clamped to the range [0, 100].

| Parameter | Max Penalty (Weight) | Formula | Reasoning |
|---|---|---|---|
| Temperature | 40 | `min(40, |temp − 22.5°C| × 2.5)` | The dominant driver of perceived comfort — deviation from a 22.5°C midpoint is penalized most steeply. |
| Humidity | 30 | `min(30, max(0, |humidity − 50%| − 10))` | Second most significant — humidity mainly affects how temperature *feels*. A 10-point buffer around 50% avoids penalizing mild swings. |
| Wind Speed | 20 | `min(20, max(0, wind − 3 m/s) × 4)` | Light breeze (under 3 m/s) is penalty-free; stronger wind is penalized steeply since it disrupts comfort disproportionately to its magnitude. |
| Cloudiness | 10 | `min(10, cloudiness% × 0.1)` | Smallest weight — cloud cover affects mood/aesthetics more than physical comfort. |

### Why subtraction with independent caps, not multiplication

An additive model (`100 − penalty₁ − penalty₂ − ...`) with each penalty capped independently was chosen over a multiplicative model (multiplying normalized factors together). With multiplication, one extreme value — a freak wind gust, for example — could crater the entire score even if every other parameter were ideal. Capping each penalty independently guarantees predictability: no single factor can ever cost the city more than its own weight allows, making the score easier to reason about and explain.

### Trade-offs considered

- These weights reflect a reasoned judgment call, not a formally validated thermal-comfort model (e.g. PMV/PPD used in HVAC engineering). For production use, calibrating against real user feedback or an established standard would be the next step.
- Dew point was excluded — it isn't directly available from OpenWeatherMap's current-weather endpoint and would require a separate calculation or API call.

---

## Caching Design

- Backed by ASP.NET Core's `IMemoryCache`, with two namespaces:
  - `weather:raw:{cityId}` — the raw OpenWeatherMap response
  - `weather:processed:{cityId}` — the already-scored, ranking-ready result
- Both entries expire 5 minutes after being written (absolute expiration).
- Lookup order: check processed cache first (skips both the API call and the scoring computation on a hit) → check raw cache (skips only the API call) → fetch from OpenWeatherMap only if both miss.
- **Why two layers:** if the Comfort Index formula changes, only the processed cache needs invalidating — the raw weather data already fetched can still be reused without an extra API call.
- **Debug endpoint:** `GET /api/debug/cache/{cityId}` reports `HIT`/`MISS` plus cache timestamps, using a read-only check that never itself populates the cache (so checking status doesn't distort what it reports).

### Known limitation

`IMemoryCache` is per-process. Running multiple instances behind a load balancer would mean each instance maintains its own separate cache, causing inconsistent HIT/MISS results across instances. A distributed cache (e.g. Redis) would be needed for horizontal scaling.

---

## Authentication & Authorization

- **Flow:** Authorization Code + PKCE via `@auth0/auth0-angular`'s `AuthModule`, `AuthGuard`, and `AuthHttpInterceptor`.
- **Enforcement points:** `canActivate: [AuthGuard]` on the Angular `/dashboard` route (frontend), and `[Authorize]` on `WeatherController` / `DebugController` (backend, validates the JWT's signature and audience via `AddJwtBearer`).
- **MFA:** Email-based, enabled at the Auth0 tenant level — no application code required.
- **Whitelisting:** Public signups disabled on the database connection; only manually created users (the test account above) can log in.

---

## Frontend Features

- Responsive card-grid layout (desktop + mobile) built with Tailwind CSS.
- Client-side sort (by rank, score, temperature, or name) and filter (by city name) — re-orders/hides already-fetched data without additional API calls; the underlying ranking is still computed server-side.
- Dark mode toggle (`darkMode: 'class'` in `tailwind.config.js`), defaults to light mode on load.

---

## Bonus Items Implemented

- ✅ Dark mode toggle
- ✅ Unit tests for the Comfort Index calculation (`ComfortIndexService.Tests.cs`)
- ✅ Frontend sort and filter
- ❌ Temperature-trend graphs (not implemented)

---

## Known Limitations (summary)

- No retry/backoff on OpenWeatherMap failures — a city that fails to fetch is silently excluded from the ranking rather than surfaced as an error to the user.
- Comfort Index weights are a design judgment, not derived from a validated thermal comfort standard.
- `IMemoryCache` does not scale horizontally across multiple server instances.
- Dew point is not included as a parameter (not directly available from the API).
