using StackExchange.Redis;

namespace UrlShortener.Services.Write.Application.Common.Cache.Interfaces;

public interface IReadCacheConnection
{
    IConnectionMultiplexer Connection { get; }
}
