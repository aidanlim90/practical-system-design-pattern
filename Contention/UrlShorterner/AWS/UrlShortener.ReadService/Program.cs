using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;
using UrlShortener.ReadService;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddDbContext<UrlShortenerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSqlConnection")));

builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisSettings>>().Value;
    var primary = string.IsNullOrWhiteSpace(settings.PrimaryEndpoint) ? "localhost:6379" : settings.PrimaryEndpoint;
    var useTls = settings.UseTls ?? false;
    var cfg = new ConfigurationOptions
    {
        AbortOnConnectFail = false,
        ConnectRetry = 5,
        ConnectTimeout = 5000,
        KeepAlive = 180,
        ResolveDns = true,
        Ssl = useTls,
    };
    cfg.EndPoints.Add(primary);
    if (!string.IsNullOrWhiteSpace(settings.Password))
    {
        cfg.Password = settings.Password;
    }
    return ConnectionMultiplexer.Connect(cfg);
});

builder.Services.AddScoped<IUrlService, UrlService>();
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/urls/{shortUrl}", async (string shortUrl, IUrlService urlService) =>
{
    var longUrl = await urlService.GetLongUrlAsync(shortUrl).ConfigureAwait(false);
    return !string.IsNullOrEmpty(longUrl) ? Results.Redirect(longUrl, permanent: false) : Results.NotFound();
})
.WithName("GetLongUrl");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
