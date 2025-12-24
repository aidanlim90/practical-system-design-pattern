using Microsoft.EntityFrameworkCore;
using UrlShortener.Services.Write.Domain.Urls;

namespace UrlShortener.Services.Write.Infrastructure.Persistence;

public sealed class UrlShortenerWriteDbContext : DbContext
{
    public const string ConnectionStringName = "NgpSqlWriteConnection";

    public UrlShortenerWriteDbContext(DbContextOptions<UrlShortenerWriteDbContext> options)
        : base(options)
    {
    }

    public DbSet<Url> Urls { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UrlShortenerWriteDbContext).Assembly);
    }
}
