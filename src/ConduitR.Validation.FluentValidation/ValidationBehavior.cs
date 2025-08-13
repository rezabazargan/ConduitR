using ConduitR.Abstractions;
using FluentValidation;
using FluentValidation.Results;

namespace ConduitR.Validation.FluentValidation;

public sealed class ValidationBehavior<TRequest,TResponse> : IPipelineBehavior<TRequest,TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;
    public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
        => _validators = validators ?? Enumerable.Empty<IValidator<TRequest>>();

    public async ValueTask<TResponse> Handle(TRequest request, CancellationToken ct, RequestHandlerDelegate<TResponse> next)
    {
        List<ValidationFailure>? failures = null;

        foreach (var v in _validators)
        {
            var res = await v.ValidateAsync(new ValidationContext<TRequest>(request), ct).ConfigureAwait(false);
            if (!res.IsValid)
            {
                failures ??= new List<ValidationFailure>(res.Errors.Count);
                failures.AddRange(res.Errors);
            }
        }

        if (failures is { Count: > 0 })
            throw new ValidationException(failures);

        return await next().ConfigureAwait(false);
    }
}
