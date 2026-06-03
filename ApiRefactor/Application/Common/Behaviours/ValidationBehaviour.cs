using FluentValidation;
using MediatR;

namespace ApiRefactor.Application.Common.Behaviours;

/// <summary>
/// MediatR pipeline behaviour that runs FluentValidation before every command/query.
/// Validation failures are surfaced as <see cref="ValidationException"/> which the
/// global exception middleware converts to a 422 response.
/// </summary>
public sealed class ValidationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    private readonly ILogger<ValidationBehaviour<TRequest, TResponse>> _logger;

    public ValidationBehaviour(
        IEnumerable<IValidator<TRequest>> validators,
        ILogger<ValidationBehaviour<TRequest, TResponse>> logger)
    {
        _validators = validators;
        _logger = logger;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (!_validators.Any())
            return await next();

        var context = new ValidationContext<TRequest>(request);

        var failures = _validators
            .Select(v => v.Validate(context))
            .SelectMany(r => r.Errors)
            .Where(e => e is not null)
            .ToList();

        if (failures.Count != 0)
        {
            _logger.LogWarning(
                "Validation failed for {RequestType} with {ErrorCount} error(s)",
                typeof(TRequest).Name,
                failures.Count);

            throw new ValidationException(failures);
        }

        return await next();
    }
}
