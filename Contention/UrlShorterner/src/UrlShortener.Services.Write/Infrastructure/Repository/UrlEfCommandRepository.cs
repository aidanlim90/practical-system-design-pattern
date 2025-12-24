using Microsoft.EntityFrameworkCore;
using UrlShortener.Services.Write.Domain.Common.Persistence.Interfaces;
using UrlShortener.Services.Write.Domain.Urls;
using UrlShortener.Services.Write.Infrastructure.Persistence;

namespace UrlShortener.Services.Write.Infrastructure.Repository;

public sealed class UrlEfCommandRepository : IUrlCommandRepository
{
    private readonly UrlShortenerWriteDbContext _dbContext;

    public UrlEfCommandRepository(UrlShortenerWriteDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Url url, CancellationToken cancellationToken = default)
    {
        await _dbContext.Urls.AddAsync(url, cancellationToken).ConfigureAwait(false);
        await _dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public void ClearChangeTracker() => _dbContext.ChangeTracker.Clear();

    public async Task<long> GetLastIdAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Urls.MaxAsync(url => url.Id, cancellationToken).ConfigureAwait(false);
}
