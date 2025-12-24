namespace UrlShortener.ReadService;

/// <summary>
/// Redis connectivity settings bound from configuration / environment variables.
/// </summary>
public sealed class RedisSettings
{
    public string? PrimaryEndpoint { get; set; }
    public string? ReaderEndpoint { get; set; }
    public string? Password { get; set; }
    public bool? UseTls { get; set; }
}
