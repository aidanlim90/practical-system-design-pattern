using Microsoft.EntityFrameworkCore;

namespace UrlShortener.Services.Write.Infrastructure.Persistence;

public static class DependencyInjectionRegister
{
    public static IServiceCollection AddNpgSqlEntityFramework(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<UrlShortenerWriteDbContext>(options =>
        {
            var connectionString = configuration.GetConnectionString(UrlShortenerWriteDbContext.ConnectionStringName);
            options.UseNpgsql(connectionString);
        });

        return services;
    }

    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using var scope = app.ApplicationServices.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<UrlShortenerWriteDbContext>();
        db.Database.Migrate();
    }
}
