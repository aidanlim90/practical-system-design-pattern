namespace UrlShortener.AWS.Api;

/// <summary>
/// Redis connectivity settings bound from configuration / environment variables.
/// Environment variable override examples:
///   REDIS__PRIMARYENDPOINT=your-elasticache-primary:6379
///   REDIS__READERENDPOINT=your-elasticache-replica:6379
///   REDIS__PASSWORD=secret
///   REDIS__USETLS=true
/// </summary>
public sealed class RedisSettings
{
    public string? PrimaryEndpoint { get; set; }
    public string? ReaderEndpoint { get; set; }
    public string? Password { get; set; }
    public bool? UseTls { get; set; }
}
