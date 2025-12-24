using StackExchange.Redis;
using UrlShortener.Services.Write.Application.Common.Cache.Interfaces;

namespace UrlShortener.Services.Write.Infrastructure.Redis;

public class PrimaryRedisConnection : IPrimaryCacheConnection
{
    public PrimaryRedisConnection(IConnectionMultiplexer connection) => Connection = connection;

    public IConnectionMultiplexer Connection { get; }
}
