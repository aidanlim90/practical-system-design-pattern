using StackExchange.Redis;
using UrlShortener.AWS.Api;

/// <summary>
/// ///Service for generating unique, sequential IDs for short URLs using Redis as a backing store.
/// Uses a batching strategy to minimize Redis calls by reserving a range of IDs locally.
/// Thread-safe, using SemaphoreSlim for synchronization and Interlocked for atomic updates.
/// </summary> <summary>
///
/// </summary>
public sealed class RedisCounterService : ICounterService
{
    private readonly IDatabase _redisDatabase;
    private const int BatchSize = 1000;
    private const string RedisCounterKey = "short-url-counter";
    private long _current = 0;
    private long _end = -1;
    private readonly SemaphoreSlim _lock = new(1, 1);

    public RedisCounterService(IConnectionMultiplexer connectionMultiplexer)
    {
        _redisDatabase = connectionMultiplexer.GetDatabase();
    }

    /// <summary>
    /// Asynchronously retrieves the next unique ID for a short URL.
    /// First checks if a cached ID is available in the current batch.
    /// If not, uses a lock to fetch a new batch from Redis, ensuring thread safety.
    /// Returns the next available ID.
    /// </summary>
    /// <returns>Global Unique Id</returns>.
    public async Task<long> GetNextIdAsync()
    {
        long current = Interlocked.Read(ref _current);
        long end = Interlocked.Read(ref _end);

        if (current <= end)
        {
            long id = Interlocked.Increment(ref _current) - 1;
            return id;
        }

        await _lock.WaitAsync();
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
            long newMax = await _redisDatabase.StringIncrementAsync(RedisCounterKey, BatchSize);
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
        long newMax = await _redisDatabase.StringIncrementAsync(RedisCounterKey, newCounter);
        long newStart = newMax - BatchSize + 1;

        Interlocked.Exchange(ref _current, newStart + 1);
        Interlocked.Exchange(ref _end, newMax);
        return newStart;
    }
}
