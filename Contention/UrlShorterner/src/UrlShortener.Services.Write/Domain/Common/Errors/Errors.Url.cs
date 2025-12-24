using ErrorOr;

namespace UrlShortener.Services.Write.Domain.Common.Errors;

public static partial class Errors
{
    public static class Url
    {
        public static Error DuplicateUrl => Error.Conflict(
            "Urls.Duplicate",
            "A URL with the same title and description was submitted within the last 24 hours.");
    }
}
