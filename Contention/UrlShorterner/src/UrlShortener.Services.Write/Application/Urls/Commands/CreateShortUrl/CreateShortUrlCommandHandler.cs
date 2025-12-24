using ErrorOr;
using Mediator;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using UrlShortener.Services.Write.Domain.Urls.Services;

namespace UrlShortener.Services.Write.Application.Urls.Commands.CreateShortUrl;

public sealed class CreateShortUrlCommandHandler : IRequestHandler<CreateShortUrlCommand, ErrorOr<string>>
{
    private readonly IUrlDomainService _urlDomainService;

    public CreateShortUrlCommandHandler(IUrlDomainService urlDomainService)
    {
        _urlDomainService = urlDomainService;
    }

    public async ValueTask<ErrorOr<string>> Handle(CreateShortUrlCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var shortUrl = await _urlDomainService.CreateShortUrlAsync(command.LongUrl, cancellationToken).ConfigureAwait(false);
            return shortUrl;
        }
        catch (DbUpdateException dbUpdateException)
            when (dbUpdateException.InnerException is PostgresException pgEx &&
                string.Equals(pgEx.SqlState, PostgresErrorCodes.UniqueViolation, StringComparison.Ordinal))
        {
           var shortUrl = await _urlDomainService.SynchronizeRedisCounterAsync(command.LongUrl, cancellationToken).ConfigureAwait(false);
           return shortUrl;
        }
    }
}
