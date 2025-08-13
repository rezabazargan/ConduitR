using ConduitR.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace ConduitR.AspNetCore;

public static class Extensions
{
    /// <summary>Adds ConduitR ProblemDetails options.</summary>
    public static IServiceCollection AddConduitProblemDetails(this IServiceCollection services, Action<ConduitProblemDetailsOptions>? configure = null)
    {
        if (configure is null) services.AddOptions<ConduitProblemDetailsOptions>();
        else services.Configure(configure);
        return services;
    }

    /// <summary>Enables the middleware that writes RFC7807 ProblemDetails for exceptions.</summary>
    public static IApplicationBuilder UseConduitProblemDetails(this IApplicationBuilder app)
        => app.UseMiddleware<ConduitProblemDetailsMiddleware>();

    /// <summary>Maps a POST endpoint that binds <typeparamref name="TRequest"/> from the body and sends it via <see cref="IMediator"/>.</summary>
    public static RouteHandlerBuilder MapMediatorPost<TRequest, TResponse>(this IEndpointRouteBuilder endpoints, string pattern)
        where TRequest : IRequest<TResponse>
    {
        return endpoints.MapPost(pattern, async (TRequest request, IMediator mediator, HttpContext _, CancellationToken ct)
            => Results.Ok(await mediator.Send(request, ct)));
    }
}
