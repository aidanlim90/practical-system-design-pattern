namespace UrlShortener.AWS.Api;

public interface IUrlService
{
    Task<string> CreateShortUrlAsync(string longUrl);
}
