using Microsoft.EntityFrameworkCore;
using UrlShortener.ReadService;

namespace UrlShortener.ReadService;

public sealed class UrlShortenerDbContext : DbContext
{
    public UrlShortenerDbContext(DbContextOptions<UrlShortenerDbContext> options)
        : base(options) { }

    public DbSet<Url> Urls { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(UrlShortenerDbContext).Assembly);
        modelBuilder.ApplyConfiguration(new UrlConfigurations());
    }
}
