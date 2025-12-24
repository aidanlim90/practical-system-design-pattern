namespace UrlShortener.Services.Write.Domain.Common.Cache.Interfaces;

public interface ICounterCommandRepository
{
    Task<long> GetNextIdAsync(CancellationToken cancellationToken = default);

    Task<long> UpdateCounterAsync(long newCounter);

    long GetNextBatchStartId(long latestDbId);
}
