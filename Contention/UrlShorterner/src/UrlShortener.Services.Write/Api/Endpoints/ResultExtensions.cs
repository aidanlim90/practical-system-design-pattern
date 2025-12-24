using System.Diagnostics;
using ErrorOr;

namespace UrlShortener.Services.Write.Api.Endpoints;

public static class ResultExtensions
{
    public static IResult ToProblemDetails(this IReadOnlyList<Error> errors, HttpContext httpContext)
    {
        if (errors.Count == 0)
        {
            return Problem(httpContext, StatusCodes.Status500InternalServerError, "An unexpected error occurred");
        }

        if (errors.All(error => error.Type == ErrorType.Validation))
        {
            return ValidationProblem(errors, httpContext);
        }

        var error = errors[0];
        var statusCode = error.Type switch
        {
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            _ => StatusCodes.Status500InternalServerError
        };

        return Problem(httpContext, statusCode, error.Description, new[] { error.Code });
    }

    private static IResult Problem(
        HttpContext httpContext,
        int statusCode,
        string? detail = null,
        string[]? errorCodes = null)
    {
        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["traceId"] = traceId,
        };

        if (errorCodes is not null)
        {
            extensions["errorCodes"] = errorCodes;
        }

        // Optionally you can map status codes to default titles/links (similar to ApiBehaviorOptions)
        var title = statusCode switch
        {
            StatusCodes.Status400BadRequest => "Bad Request",
            StatusCodes.Status404NotFound => "Not Found",
            StatusCodes.Status409Conflict => "Conflict",
            StatusCodes.Status500InternalServerError => "Internal Server Error",
            _ => "Error"
        };

        return Results.Problem(
            detail,
            null,
            statusCode,
            title,
            null,
            extensions);
    }

    private static IResult ValidationProblem(IReadOnlyCollection<Error> errors, HttpContext httpContext)
    {
        var errorDictionary = errors
            .GroupBy(error => error.Code, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray(),
                StringComparer.Ordinal);

        var traceId = Activity.Current?.Id ?? httpContext.TraceIdentifier;

        var extensions = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["traceId"] = traceId,
            ["errorCodes"] = errors.Select(error => error.Code).ToArray(),
        };

        return Results.ValidationProblem(
            errorDictionary,
            statusCode: StatusCodes.Status400BadRequest,
            extensions: extensions);
    }
}
