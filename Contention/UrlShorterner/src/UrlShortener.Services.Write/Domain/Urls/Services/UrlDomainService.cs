using UrlShortener.Services.Write.Application.Common.Utils;
using UrlShortener.Services.Write.Domain.Common.Cache.Interfaces;
using UrlShortener.Services.Write.Domain.Common.Persistence.Interfaces;

namespace UrlShortener.Services.Write.Domain.Urls.Services;

public sealed class UrlDomainService : IUrlDomainService
{
    private readonly ICounterCommandRepository _counterCommmandRepository;
    private readonly IUrlCommandRepository _urlCommandRepository;

    public UrlDomainService(
        ICounterCommandRepository counterRepository,
        IUrlCommandRepository urlRepository)
    {
        _counterCommmandRepository = counterRepository;
        _urlCommandRepository = urlRepository;
    }

    public async Task<string> CreateShortUrlAsync(string longUrl, CancellationToken cancellationToken = default)
    {
        long uniqueId = await _counterCommmandRepository.GetNextIdAsync(cancellationToken).ConfigureAwait(false);
        var shortUrl = Base62Converter.Encode(uniqueId);

        var url = Url.Create(uniqueId, shortUrl, longUrl);
        await _urlCommandRepository.AddAsync(url, cancellationToken).ConfigureAwait(false);

        return shortUrl;
    }

    public async Task<string> SynchronizeRedisCounterAsync(string longUrl, CancellationToken cancellationToken = default)
    {
        _urlCommandRepository.ClearChangeTracker();
        var latestDbId = await _urlCommandRepository.GetLastIdAsync(cancellationToken).ConfigureAwait(false);
        var newBatchStartId = _counterCommmandRepository.GetNextBatchStartId(latestDbId);
        await _counterCommmandRepository.UpdateCounterAsync(newBatchStartId).ConfigureAwait(false);
        var shortUrl = await CreateShortUrlAsync(longUrl, cancellationToken).ConfigureAwait(false);
        return shortUrl;
    }
}
