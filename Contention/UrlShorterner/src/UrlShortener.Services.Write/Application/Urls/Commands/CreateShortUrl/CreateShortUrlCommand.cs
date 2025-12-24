using ErrorOr;
using Mediator;

namespace UrlShortener.Services.Write.Application.Urls.Commands.CreateShortUrl;

public sealed record CreateShortUrlCommand(string LongUrl) : IRequest<ErrorOr<string>>;
