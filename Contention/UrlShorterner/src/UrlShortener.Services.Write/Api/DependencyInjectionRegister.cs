using Microsoft.AspNetCore.Mvc.Infrastructure;
using UrlShortener.Services.Write.Api.Errors;

namespace UrlShortener.Services.Write.Api;

public static class DependencyInjectionRegister
{
    public static IServiceCollection AddApi(this IServiceCollection services)
    {
        services
            .AddSingleton<ProblemDetailsFactory, UrlShortenerProblemDetailsFactory>()
            .AddExceptionHandler<GlobalExceptionHandler>()
            .AddProblemDetails();

        return services;
    }
}
