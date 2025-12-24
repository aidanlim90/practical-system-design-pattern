using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

namespace UrlShortener.ReadService;

public interface IUrlService
{
    Task<string?> GetLongUrlAsync(string shortUrl);
}

public sealed class UrlService : IUrlService
{
    private readonly IDatabase _redisDatabase;
    private readonly UrlShortenerDbContext _dbContext;

    public UrlService(IConnectionMultiplexer connectionMultiplexer, UrlShortenerDbContext dbContext)
    {
        _redisDatabase = connectionMultiplexer.GetDatabase();
        _dbContext = dbContext;
    }

    public async Task<string?> GetLongUrlAsync(string shortUrl)
    {
        var longUrl = await _redisDatabase.StringGetAsync(shortUrl);
        if (!longUrl.IsNullOrEmpty)
        {
            return longUrl.ToString();
        }

        var url = await _dbContext.Urls.AsNoTracking().FirstOrDefaultAsync(urls => urls.ShortUrl == shortUrl);
        if (url is null || url.ExpiredAt < DateTimeOffset.UtcNow)
        {
            return null;
        }

        await _redisDatabase.StringAppendAsync(shortUrl, url.LongUrl);
        TimeSpan? expiry = url.ExpiredAt - DateTimeOffset.UtcNow;
        if (expiry.HasValue && expiry.Value > TimeSpan.Zero)
        {
            await _redisDatabase.KeyExpireAsync(shortUrl, expiry.Value);
        }
        return url.LongUrl;

    }
}
