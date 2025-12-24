using Microsoft.Extensions.Options;
using StackExchange.Redis;
using UrlShortener.Services.Write.Application.Common.Cache.Interfaces;

namespace UrlShortener.Services.Write.Infrastructure.Redis;

public static class DependencyInjectionRegister
{
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration configuration)
    {
        var redisSection = configuration.GetSection(RedisSettings.SectionName);
        services.Configure<RedisSettings>(redisSection);

        var redisSettings = services
            .BuildServiceProvider()
            .GetRequiredService<IOptions<RedisSettings>>()
            .Value;
        ConfigurePrimaryRedisConnection(services, redisSettings);
        if (!string.IsNullOrEmpty(redisSettings.ReaderHost))
        {
            ConfigureReadRedisConnection(services, redisSettings);
        }

        return services;
    }

    private static void ConfigureReadRedisConnection(IServiceCollection services, RedisSettings redisSettings)
    {
        services.AddSingleton<IReadCacheConnection>(sp =>
        {
            var cfg = ConfigureRedis(redisSettings, redisSettings.ReaderHost!, redisSettings.Port);

            var connection = ConnectionMultiplexer.Connect(cfg);
            return new ReadRedisConnection(connection);
        });
    }

    private static void ConfigurePrimaryRedisConnection(IServiceCollection services, RedisSettings redisSettings)
    {
        services.AddSingleton<IPrimaryCacheConnection>(sp =>
        {
            var cfg = ConfigureRedis(redisSettings, redisSettings.PrimaryHost, redisSettings.Port);

            var connection = ConnectionMultiplexer.Connect(cfg);
            return new PrimaryRedisConnection(connection);
        });
    }

    private static ConfigurationOptions ConfigureRedis(RedisSettings settings, string host, int port)
    {
        var cfg = new ConfigurationOptions
        {
            AbortOnConnectFail = false,
            EndPoints = { { host, port } },
            Password = settings.AuthToken,
            Ssl = settings.UseSsl,
            KeepAlive = 180,
        };

        if (!string.IsNullOrEmpty(settings.SslHost))
        {
            cfg.SslHost = settings.SslHost;
        }

        return cfg;
    }
}
