using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ConduitR.AspNetCore;

/// <summary>Options to control ConduitR ProblemDetails behavior.</summary>
public sealed class ConduitProblemDetailsOptions
{
    /// <summary>Map exception type to HTTP status code.</summary>
    public Dictionary<Type, int> StatusCodeMap { get; } = new()
    {
        { typeof(ArgumentException), StatusCodes.Status400BadRequest },
        { typeof(UnauthorizedAccessException), StatusCodes.Status401Unauthorized },
        // default fallback handled in middleware (500)
    };

    /// <summary>Customize the ProblemDetails before writing.</summary>
    public Action<HttpContext, Exception, ProblemDetails>? Enrich { get; set; }
}
