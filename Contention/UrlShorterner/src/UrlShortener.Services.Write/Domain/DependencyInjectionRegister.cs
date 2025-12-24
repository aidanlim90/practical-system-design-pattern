using UrlShortener.Services.Write.Domain.Urls.Services;

namespace UrlShortener.Services.Write.Domain;

public static class DependencyInjectionRegister
{
    public static IServiceCollection AddDomain(this IServiceCollection services)
    {
        services
            .AddScoped<IUrlDomainService, UrlDomainService>();

        return services;
    }
}
