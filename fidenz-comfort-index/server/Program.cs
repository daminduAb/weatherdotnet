using FidenzComfortIndex.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;

var builder = WebApplication.CreateBuilder(args);

// ---- Services ----
builder.Services.AddControllers();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient<IWeatherService, WeatherService>();

builder.Services.AddSingleton<IComfortIndexService, ComfortIndexService>();
builder.Services.AddSingleton<IWeatherCacheService, WeatherCacheService>();
builder.Services.AddScoped<IWeatherService, WeatherService>();

// ---- Auth0 JWT Bearer ----
// appsettings.json needs:
//   "Auth0": { "Domain": "your-tenant.us.auth0.com", "Audience": "https://fidenz-comfort-index-api" }
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://{builder.Configuration["Auth0:Domain"]}/";
        options.Audience = builder.Configuration["Auth0:Audience"];
    });

builder.Services.AddAuthorization();

// ---- CORS for the Angular dev server ----
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularClient", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseHttpsRedirection();
app.UseCors("AngularClient");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();