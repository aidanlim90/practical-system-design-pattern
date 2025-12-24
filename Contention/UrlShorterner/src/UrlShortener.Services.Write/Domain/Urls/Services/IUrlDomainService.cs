namespace UrlShortener.Services.Write.Domain.Urls.Services;

public interface IUrlDomainService
{
    Task<string> CreateShortUrlAsync(string longUrl, CancellationToken cancellationToken = default);

    Task<string> SynchronizeRedisCounterAsync(string longUrl, CancellationToken cancellationToken = default);
}
