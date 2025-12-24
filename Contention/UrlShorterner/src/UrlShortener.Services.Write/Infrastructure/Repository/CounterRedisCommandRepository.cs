using StackExchange.Redis;
using UrlShortener.Services.Write.Application.Common.Cache.Interfaces;
using UrlShortener.Services.Write.Domain.Common.Cache.Interfaces;

namespace UrlShortener.Services.Write.Infrastructure.Repository;

public sealed class CounterRedisCommandRepository : ICounterCommandRepository, IDisposable
{
    private const int BatchSize = 1000;
    private const string RedisCounterKey = "short-url-counter";
    private readonly SemaphoreSlim _lock = new (1, 1);
    private readonly IDatabase _redisDatabase;
    private long _current;
    private long _end = -1;
    private bool _disposed;

    public CounterRedisCommandRepository(IPrimaryCacheConnection primaryCacheConnection)
    {
        _redisDatabase = primaryCacheConnection.Connection.GetDatabase();
    }

    public async Task<long> GetNextIdAsync(CancellationToken cancellationToken = default)
    {
        long current = Interlocked.Read(ref _current);
        long end = Interlocked.Read(ref _end);

        if (current <= end)
        {
            long id = Interlocked.Increment(ref _current) - 1;
            return id;
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            current = Interlocked.Read(ref _current);
            end = Interlocked.Read(ref _end);

            // Prevent multiple request calling this function before current and end set
            if (current <= end)
            {
                long id = Interlocked.Increment(ref _current) - 1;
                return id;
            }

            // Fetch new batch
            long newMax = await _redisDatabase.StringIncrementAsync(RedisCounterKey, BatchSize).ConfigureAwait(false);
            long newStart = newMax - BatchSize + 1;

            Interlocked.Exchange(ref _current, newStart + 1);
            Interlocked.Exchange(ref _end, newMax);
            return newStart;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<long> UpdateCounterAsync(long newCounter)
    {
        long newMax = await _redisDatabase.StringIncrementAsync(RedisCounterKey, newCounter).ConfigureAwait(false);
        long newStart = newMax - BatchSize + 1;

        Interlocked.Exchange(ref _current, newStart + 1);
        Interlocked.Exchange(ref _end, newMax);
        return newStart;
    }

    public long GetNextBatchStartId(long latestDbId)
    {
        var newId = (long)(Math.Ceiling(latestDbId / (double)BatchSize) * BatchSize);
        return newId;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _lock.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
