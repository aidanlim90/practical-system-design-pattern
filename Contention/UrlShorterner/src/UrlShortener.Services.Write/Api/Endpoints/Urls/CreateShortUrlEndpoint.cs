using Mediator;
using UrlShortener.Services.Write.Application.Urls.Commands.CreateShortUrl;

namespace UrlShortener.Services.Write.Api.Endpoints.Urls;

public static class CreateShortUrlEndpoint
{
    public static void MapShortUrlEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/urls", CreateShortUrlAsync);
    }

    private static async Task<IResult> CreateShortUrlAsync(ISender sender, HttpContext context,  CreateShortUrlRequest request)
    {
        var command = new CreateShortUrlCommand(request.LongUrl);
        var result = await sender.Send(command).ConfigureAwait(false);
        return result.Match(
            success => Results.Ok(success),
            errors => errors.ToProblemDetails(context));
    }
}
