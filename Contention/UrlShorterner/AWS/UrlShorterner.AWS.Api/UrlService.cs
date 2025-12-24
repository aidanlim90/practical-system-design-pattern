using UrlShortener.AWS.Api;

namespace UrlShortener.AWS.Api;
public sealed class UrlService : IUrlService
{
    private readonly ICounterService _counterService;
    private readonly UrlShortenerDbContext _dbContext;

    public UrlService(
        ICounterService counterService,
        UrlShortenerDbContext dbContext)
    {
        _counterService = counterService;
        _dbContext = dbContext;
    }

    public async Task<string> CreateShortUrlAsync(string longUrl)
    {
        var uniqueId = await _counterService.GetNextIdAsync();
        var shortUrl = Base62Converter.Encode(uniqueId);
        var url = Url.Create(uniqueId, shortUrl, longUrl);
        _dbContext.Urls.Add(url);
        await _dbContext.SaveChangesAsync().ConfigureAwait(false);
        return shortUrl;
    }
}
