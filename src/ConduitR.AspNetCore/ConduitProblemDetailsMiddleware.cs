using System.Net.Mime;
using System.Reflection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ConduitR.AspNetCore;

/// <summary>Middleware that converts exceptions to RFC 7807 ProblemDetails JSON.</summary>
public sealed class ConduitProblemDetailsMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ConduitProblemDetailsMiddleware> _logger;
    private readonly ConduitProblemDetailsOptions _options;

    public ConduitProblemDetailsMiddleware(RequestDelegate next, ILogger<ConduitProblemDetailsMiddleware> logger, IOptions<ConduitProblemDetailsOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    public async Task Invoke(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await WriteProblemAsync(context, ex);
        }
    }

    private async Task WriteProblemAsync(HttpContext ctx, Exception ex)
    {
        var problem = new ProblemDetails
        {
            Title = GetTitle(ex),
            Detail = ex.Message,
            Type = ex.GetType().FullName,
            Instance = ctx.Request.Path
        };

        int status = ResolveStatusCode(ex);
        problem.Status = status;
        ctx.Response.StatusCode = status;
        ctx.Response.ContentType = MediaTypeNames.Application.Json;

        // Include validation details if it's a FluentValidation.ValidationException (without taking a hard dependency).
        if (ex.GetType().FullName == "FluentValidation.ValidationException")
        {
            var errorsProp = ex.GetType().GetProperty("Errors", BindingFlags.Public | BindingFlags.Instance);
            if (errorsProp?.GetValue(ex) is System.Collections.IEnumerable errors)
            {
                var list = new List<object>();
                foreach (var err in errors)
                {
                    var t = err.GetType();
                    var property = t.GetProperty("PropertyName")?.GetValue(err)?.ToString();
                    var message  = t.GetProperty("ErrorMessage")?.GetValue(err)?.ToString();
                    var code     = t.GetProperty("ErrorCode")?.GetValue(err)?.ToString();
                    list.Add(new { property, message, code });
                }
                problem.Extensions["errors"] = list;
            }
        }

        _options.Enrich?.Invoke(ctx, ex, problem);

        // Don't log expected client errors (4xx) as errors
        if (status >= 500)
        {
            _logger.LogError(ex, "Unhandled exception converting to ProblemDetails");
        }
        else
        {
            _logger.LogInformation(ex, "Handled exception -> ProblemDetails {StatusCode}", status);
        }

        await ctx.Response.WriteAsJsonAsync(problem);
    }

    private static string GetTitle(Exception ex) => ex switch
    {
        ArgumentException => "Bad request",
        _ => "An error occurred"
    };

    private int ResolveStatusCode(Exception ex)
    {
        // Map by exact type or base types present in map
        var type = ex.GetType();
        while (type is not null)
        {
            if (_options.StatusCodeMap.TryGetValue(type, out var status))
                return status;
            type = type.BaseType;
        }

        // Special-case: FluentValidation.ValidationException → 400
        if (ex.GetType().FullName == "FluentValidation.ValidationException")
            return StatusCodes.Status400BadRequest;

        return StatusCodes.Status500InternalServerError;
    }
}
