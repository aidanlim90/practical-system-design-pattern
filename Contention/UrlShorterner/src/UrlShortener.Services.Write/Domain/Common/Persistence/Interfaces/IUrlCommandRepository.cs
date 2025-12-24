using UrlShortener.Services.Write.Domain.Urls;

namespace UrlShortener.Services.Write.Domain.Common.Persistence.Interfaces;

public interface IUrlCommandRepository
{
    Task AddAsync(Url url, CancellationToken cancellationToken = default);

    void ClearChangeTracker();

    Task<long> GetLastIdAsync(CancellationToken cancellationToken = default);
}
