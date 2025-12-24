using FluentValidation;

namespace UrlShortener.Services.Write.Application.Urls.Commands.CreateShortUrl;

public class CreateShortUrlValidator : AbstractValidator<CreateShortUrlCommand>
{
    public CreateShortUrlValidator()
    {
        RuleFor(command => command.LongUrl)
            .NotEmpty().WithErrorCode("LongUrl.Empty").WithMessage("LongUrl is required")
            .MaximumLength(2048).WithErrorCode("LongUrl.Exceeded").WithMessage("LongUrl cannot exceed 2048 characters");
    }
}
