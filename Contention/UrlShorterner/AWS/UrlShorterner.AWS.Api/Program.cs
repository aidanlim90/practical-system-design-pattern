using System.Data.Common;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StackExchange.Redis;
using UrlShortener.AWS.Api;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddDbContext<UrlShortenerDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("PostgreSqlConnection")));

// Bind typed Redis settings (configuration layering handles env-specific values via appsettings.* and env vars)
builder.Services.Configure<RedisSettings>(builder.Configuration.GetSection("Redis"));
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var settings = sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RedisSettings>>().Value;
    // Single fallback for local development if nothing provided (kept minimal and deterministic)
    var primary = string.IsNullOrWhiteSpace(settings.PrimaryEndpoint) ? "localhost:6379" : settings.PrimaryEndpoint;
    var useTls = settings.UseTls ?? false; // default false unless explicitly set
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

builder.Services.AddSingleton<ICounterService, RedisCounterService>();
builder.Services.AddScoped<IUrlService, UrlService>();
var app = builder.Build();

app.MapPost("/urls", async ([FromBody] CreateUrlRequest request, IUrlService urlService, UrlShortenerDbContext dbContext, ICounterService counterService) =>
{
    try
    {
        var shortUrl = await urlService.CreateShortUrlAsync(request.LongUrl);
        return Results.Created($"/urls/{shortUrl}", shortUrl);
    }
    catch (DbUpdateException ex) when (ex.InnerException is PostgresException pgEx && pgEx.SqlState == PostgresErrorCodes.UniqueViolation)
    {
        dbContext.ChangeTracker.Clear();
        long latestDbId = await dbContext.Urls.MaxAsync(urlEntity => urlEntity.Id).ConfigureAwait(false);
        int newId = (int)(Math.Ceiling(latestDbId / 1000.0) * 1000);
        await counterService.UpdateCounterAsync(newId).ConfigureAwait(false);
        var shortUrl = await urlService.CreateShortUrlAsync(request.LongUrl).ConfigureAwait(false);
        return Results.Created($"/urls/{shortUrl}", shortUrl);
    }
});

app.MapGet("/", () => Results.Ok("hello"));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// app.UseHttpsRedirection();

app.Run();
