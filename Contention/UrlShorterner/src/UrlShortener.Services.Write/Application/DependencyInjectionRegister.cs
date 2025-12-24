using System.Reflection;
using FluentValidation;
using Mediator;
using UrlShortener.Services.Write.Application.Common.Behaviors;

namespace UrlShortener.Services.Write.Application;

public static class DependencyInjectionRegister
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services
            .AddMediator()
            .AddValidatorsFromAssemblyContaining<Program>(ServiceLifetime.Scoped);

        return services;
    }

    private static IServiceCollection AddMediator(this IServiceCollection services)
    {
        services.AddMediator(options =>
        {
            options.Assemblies = [typeof(Program)];
            options.PipelineBehaviors = [typeof(ValidationBehavior<,>)];
            options.ServiceLifetime = ServiceLifetime.Scoped;
        });

        return services;
    }
}
