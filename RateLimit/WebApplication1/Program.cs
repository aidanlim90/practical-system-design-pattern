
using System.Threading.RateLimiting;
using StackExchange.Redis;

namespace WebApplication1;

public class Program
{
    public class RedisSettings
    {
        public const string SectionName = "Redis";
        public string Host { get; set; }
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public string AuthToken { get; set; }
    }

    private static ConfigurationOptions ConfigureRedis(RedisSettings settings, string host, int port)
    {
        var cfg = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            EndPoints = { { host, port } },
            Ssl = settings.UseSsl,
            Password = settings.AuthToken,
            KeepAlive = 180,
        };

        return cfg;
    }

    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add services to the container.
        builder.Services.AddAuthorization();

        // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
        builder.Services.AddOpenApi();
        var redisSection = builder.Configuration.GetSection(RedisSettings.SectionName);
        builder.Services.Configure<RedisSettings>(redisSection);
        var redisSettings = redisSection.Get<RedisSettings>()!;
        var redisConfig = ConfigureRedis(redisSettings, redisSettings.Host, redisSettings.Port);
        builder.Services.AddSingleton<IConnectionMultiplexer>(ConnectionMultiplexer.Connect(redisConfig));
        builder.Services.AddSingleton<TokenBucketRateLimiter>(provider => {
            var redis = provider.GetRequiredService<IConnectionMultiplexer>();
            var logger = provider.GetRequiredService<ILogger<TokenBucketRateLimiter>>();
            return new TokenBucketRateLimiter(redis, logger, 10, 1);
        });
        var app = builder.Build();

        // Configure the HTTP request pipeline.
        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
        }

        //app.UseHttpsRedirection();

        app.UseAuthorization();

        var summaries = new[]
        {
            "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
        };

        app.Use(async (context, next) =>
        {
            var rateLimiter = context.RequestServices.GetRequiredService<TokenBucketRateLimiter>();

            // Use client IP or user ID
            var clientKey = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";

            if (!await rateLimiter.IsAllowedAsync(clientKey))
            {
                context.Response.StatusCode = 429;
                context.Response.Headers["Retry-After"] = "60"; // Optional
                await context.Response.WriteAsync("Too many requests. Try again later.");
                return;
            }

            await next();
        });

        app.MapGet("/weatherforecast", (HttpContext httpContext) =>
        {
            var forecast = Enumerable.Range(1, 5).Select(index =>
                new WeatherForecast
                {
                    Date = DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
                    TemperatureC = Random.Shared.Next(-20, 55),
                    Summary = summaries[Random.Shared.Next(summaries.Length)]
                })
                .ToArray();
            return forecast;
        })
        .WithName("GetWeatherForecast");

        app.Run();
    }
}

public class TokenBucketRateLimiter
{
    private readonly IDatabase _redis;
    private readonly ILogger<TokenBucketRateLimiter> _logger;
    private readonly LuaScript _luaScript;
    private readonly int _bucketCapacity;
    private readonly double _refillRatePerSecond;

    public TokenBucketRateLimiter(
    IConnectionMultiplexer connectionMultiplexer,
    ILogger<TokenBucketRateLimiter> logger,
    int bucketCapacity,
    double refillRatePerSecond)
    {
        _redis = connectionMultiplexer.GetDatabase();
        _logger = logger;
        _bucketCapacity = bucketCapacity;
        _refillRatePerSecond = refillRatePerSecond;

        // Lua script:
        // KEYS[1] = token key
        // KEYS[2] = timestamp key
        // ARGV[1] = current timestamp (ms)
        // ARGV[2] = bucket capacity
        // ARGV[3] = refill rate per second
        _luaScript = LuaScript.Prepare(@"
        local tokens_key = KEYS[1]
        local timestamp_key = KEYS[2]
        local now = tonumber(ARGV[1])
        local capacity = tonumber(ARGV[2])
        local refill_rate = tonumber(ARGV[3])
 
        local last_tokens = tonumber(redis.call('GET', tokens_key) or capacity)
        local last_refill = tonumber(redis.call('GET', timestamp_key) or now)
 
        local elapsed = now - last_refill
        local refill = math.floor(elapsed * refill_rate / 1000)
        local tokens = math.min(capacity, last_tokens + refill)
 
        if tokens <= 0 then
            return 0
        else
            tokens = tokens - 1
            redis.call('SET', tokens_key, tokens)
            redis.call('SET', timestamp_key, now)
            redis.call('PEXPIRE', tokens_key, 60000)
            redis.call('PEXPIRE', timestamp_key, 60000)
            return 1
        end
    ");
    }

    public async Task<bool> IsAllowedAsync(string key)
    {
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var redisKeys = new RedisKey[]
        {
        new RedisKey($"token_bucket:{key}:tokens"),
        new RedisKey($"token_bucket:{key}:timestamp")
        };
        var redisArgs = new RedisValue[]
        {
        now,
        _bucketCapacity,
        _refillRatePerSecond
        };

        try
        {
            var result = (int)await _redis.ScriptEvaluateAsync(_luaScript.OriginalScript, redisKeys, redisArgs);
            return result == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "TokenBucketRateLimiter failed for key {Key}", key);
            return true; // fail-open strategy
        }
    }
}