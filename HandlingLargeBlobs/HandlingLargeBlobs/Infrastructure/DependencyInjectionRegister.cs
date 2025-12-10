using Amazon;
using Amazon.S3;
using Microsoft.Extensions.Options;

namespace HandlingLargeBlobs.Infrastructure;

/// <summary>
/// Provides extension methods for registering infrastructure dependencies (e.g. S3 settings).
/// </summary>
public static class DependencyInjectionRegister
{
    /// <summary>
    /// Registers infrastructure dependencies.
    /// </summary>
    /// <param name="services">The service collection to add the settings to.</param>
    /// <returns>The same IServiceCollection instance for chaining.</returns>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<IAmazonS3>(static sp =>
        {
            var s3Settings = sp.GetRequiredService<IOptions<S3Settings>>().Value;
            var config = new AmazonS3Config
            {
                RegionEndpoint = RegionEndpoint.GetBySystemName(s3Settings.Region),
            };

            return new AmazonS3Client(config);
        });
        return services;
    }
}
