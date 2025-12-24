using UrlShortener.Services.Write.Domain.Common.Cache.Interfaces;
using UrlShortener.Services.Write.Domain.Common.Persistence.Interfaces;
using UrlShortener.Services.Write.Infrastructure.Persistence;
using UrlShortener.Services.Write.Infrastructure.Redis;
using UrlShortener.Services.Write.Infrastructure.Repository;

namespace UrlShortener.Services.Write.Infrastructure;

public static class DependencyInjectionRegister
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddNpgSqlEntityFramework(configuration)
            .AddRedisCache(configuration)
            .AddScoped<IUrlCommandRepository, UrlEfCommandRepository>()
            .AddSingleton<ICounterCommandRepository, CounterRedisCommandRepository>();

        return services;
    }
}
