using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using UrlShortener.Services.Write.Domain.Urls;

namespace UrlShortener.Services.Write.Infrastructure.Persistence.Configurations;

public sealed class UrlConfigurations : IEntityTypeConfiguration<Url>
{
    public const string UrlTable = "Urls";

    public void Configure(EntityTypeBuilder<Url> builder)
    {
        ConfigureUrlTable(builder);
    }

    private static void ConfigureUrlTable(EntityTypeBuilder<Url> builder)
    {
        builder.ToTable(UrlTable);

        builder.HasKey(url => url.Id);

        builder
            .Property(url => url.Id)
            .ValueGeneratedNever()
            .HasMaxLength(10);

        builder
            .HasIndex(url => url.ShortUrl)
            .HasDatabaseName("IX_ShortUrl_Hash")
            .HasMethod("hash"); // hash index with o(1) lookup

        builder
            .Property(url => url.ShortUrl)
            .HasMaxLength(10);

        builder
            .Property(url => url.LongUrl)
            .IsRequired()
            .HasMaxLength(2048);
    }
}
