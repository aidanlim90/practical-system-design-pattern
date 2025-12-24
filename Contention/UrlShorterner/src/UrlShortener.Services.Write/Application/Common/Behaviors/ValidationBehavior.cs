using ErrorOr;
using FluentValidation;
using Mediator;

namespace UrlShortener.Services.Write.Application.Common.Behaviors;

public sealed class ValidationBehavior<TRequest, TResponse> :
    IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
        where TResponse : IErrorOr
{
    private readonly IValidator<TRequest>? _validator;

    public ValidationBehavior(IValidator<TRequest>? validator = null)
    {
        _validator = validator;
    }

    public async ValueTask<TResponse> Handle(TRequest request, MessageHandlerDelegate<TRequest, TResponse> next, CancellationToken cancellationToken)
    {
        if (_validator is null)
        {
            return await next(request, cancellationToken).ConfigureAwait(false);
        }

        var validationResult = await _validator.ValidateAsync(request, cancellationToken).ConfigureAwait(false);
        if (validationResult.IsValid)
        {
            return await next(request, cancellationToken).ConfigureAwait(false);
        }

        var errors = validationResult.Errors
            .Select(validationFailure => Error.Validation(validationFailure.ErrorCode, validationFailure.ErrorMessage))
            .ToList();
        return (dynamic)errors;
    }
}
