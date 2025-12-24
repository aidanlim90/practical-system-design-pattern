namespace UrlShortener.AWS.Api;

public interface ICounterService
{
    Task<long> GetNextIdAsync();

    Task<long> UpdateCounterAsync(long newCounter);
}
