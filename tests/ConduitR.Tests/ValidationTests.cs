using System.Reflection;
using ConduitR;
using ConduitR.Abstractions;
using ConduitR.DependencyInjection;
using ConduitR.Validation.FluentValidation;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

public class ValidationTests
{
    [Fact]
    public async Task AddConduitValidation_throws_when_request_is_invalid()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddConduitValidation(Assembly.GetExecutingAssembly());

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        await Assert.ThrowsAsync<ValidationException>(() => mediator.Send(new ValidatedRequest(string.Empty)).AsTask());
    }

    [Fact]
    public async Task AddConduitValidation_allows_valid_request()
    {
        var services = new ServiceCollection();
        services.AddConduit(cfg => cfg.AddHandlersFromAssemblies(Assembly.GetExecutingAssembly()));
        services.AddConduitValidation(Assembly.GetExecutingAssembly());

        var mediator = services.BuildServiceProvider().GetRequiredService<IMediator>();

        var result = await mediator.Send(new ValidatedRequest("valid")).AsTask();

        Assert.Equal("valid", result);
    }

    public sealed record ValidatedRequest(string Name) : IRequest<string>;

    public sealed class ValidatedRequestValidator : AbstractValidator<ValidatedRequest>
    {
        public ValidatedRequestValidator()
        {
            RuleFor(x => x.Name).NotEmpty();
        }
    }

    public sealed class ValidatedRequestHandler : IRequestHandler<ValidatedRequest, string>
    {
        public ValueTask<string> Handle(ValidatedRequest request, CancellationToken cancellationToken)
            => ValueTask.FromResult(request.Name);
    }
}
