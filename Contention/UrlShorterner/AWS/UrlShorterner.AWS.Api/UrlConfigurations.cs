using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UrlShortener.AWS.Api;

public sealed class UrlConfigurations : IEntityTypeConfiguration<Url>
{
    public const string UrlTable = "Urls";

    public void Configure(EntityTypeBuilder<Url> builder)
    {
        ConfigureUrlTable(builder);
    }

    private void ConfigureUrlTable(EntityTypeBuilder<Url> builder)
    {
        builder.ToTable(UrlTable);

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .ValueGeneratedNever()
            .HasMaxLength(10);

        builder
            .HasIndex(x => x.ShortUrl)
            .HasDatabaseName("IX_ShortUrl_Hash")
            .HasMethod("hash"); //hash index with o(1) lookup

        builder.Property(x => x.ShortUrl)
            .HasMaxLength(10);

        builder.Property(x => x.LongUrl).IsRequired().HasMaxLength(2048);
    }
}
