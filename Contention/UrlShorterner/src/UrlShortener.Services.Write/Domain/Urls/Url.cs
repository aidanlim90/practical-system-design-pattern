namespace UrlShortener.Services.Write.Domain.Urls;

public sealed class Url
{
    private Url(long id, string shortUrl, string longUrl)
    {
        Id = id;
        ShortUrl = shortUrl;
        LongUrl = longUrl;
        ExpiredAt = DateTimeOffset.UtcNow.AddDays(7);
    }

    public long Id { get; private set; }

    public string ShortUrl { get; private set; }

    public string LongUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset ExpiredAt { get; private set; }

    public static Url Create(long id, string shortUrl, string longUrl)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(id, nameof(id));
        ArgumentException.ThrowIfNullOrWhiteSpace(shortUrl, nameof(shortUrl));
        ArgumentException.ThrowIfNullOrWhiteSpace(longUrl, nameof(longUrl));

        return new Url(id, shortUrl, longUrl);
    }
}
