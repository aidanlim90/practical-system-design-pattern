namespace UrlShortener.Services.Write.Infrastructure.Redis;

public class RedisSettings
{
    public const string SectionName = "Redis";

    public string PrimaryHost { get; set; } = string.Empty;

    public string? ReaderHost { get; set; }

    public int Port { get; set; }

    public string AuthToken { get; set; } = string.Empty;

    public bool UseSsl { get; set; }

    public string? SslHost { get; set; }
}
