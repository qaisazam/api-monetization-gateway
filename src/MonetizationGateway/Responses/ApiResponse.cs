using System.Text.Json;
using MonetizationGateway.Constants;

namespace MonetizationGateway.Responses;

/// <summary>Centralized API error and rate-limit response helpers (consistent JSON shape and headers).</summary>
public static class ApiResponse
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    /// <summary>Writes 401 with error and code (MISSING_API_KEY or INVALID_API_KEY).</summary>
    public static Task WriteUnauthorizedAsync(HttpContext context, string message, string code)
    {
        context.Response.StatusCode = 401;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message, code }, JsonOptions));
    }

    /// <summary>Writes 403 with error and code TIER_NOT_FOUND.</summary>
    public static Task WriteForbiddenAsync(HttpContext context, string message, string code)
    {
        context.Response.StatusCode = 403;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message, code }, JsonOptions));
    }

    /// <summary>Writes 429 with body (error, limit, remaining, resetAt, retryAfter) and headers Retry-After, X-RateLimit-Limit, X-RateLimit-Remaining, X-RateLimit-Reset (Unix seconds).</summary>
    public static Task WriteTooManyRequestsAsync(
        HttpContext context,
        string error,
        int limit,
        int remaining,
        DateTime? resetAt,
        int? retryAfterSeconds)
    {
        context.Response.StatusCode = 429;
        context.Response.ContentType = "application/json";
        if (retryAfterSeconds.HasValue)
            context.Response.Headers[ApiConstants.Headers.RetryAfter] = retryAfterSeconds.Value.ToString();
        context.Response.Headers[ApiConstants.Headers.RateLimitLimit] = limit.ToString();
        context.Response.Headers[ApiConstants.Headers.RateLimitRemaining] = remaining.ToString();
        if (resetAt.HasValue)
            context.Response.Headers[ApiConstants.Headers.RateLimitReset] = new DateTimeOffset(resetAt.Value).ToUnixTimeSeconds().ToString();

        var body = JsonSerializer.Serialize(new
        {
            error,
            limit,
            remaining,
            resetAt = resetAt?.ToUniversalTime(),
            retryAfter = retryAfterSeconds
        }, JsonOptions);
        return context.Response.WriteAsync(body);
    }

    /// <summary>Writes 500 with error and code INTERNAL_ERROR.</summary>
    public static Task WriteInternalErrorAsync(HttpContext context, string message, string code)
    {
        context.Response.StatusCode = 500;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message, code }, JsonOptions));
    }

    /// <summary>Writes 503 with error and code (e.g. RATE_LIMIT_UNAVAILABLE or SERVICE_UNAVAILABLE).</summary>
    public static Task WriteServiceUnavailableAsync(HttpContext context, string message, string code)
    {
        context.Response.StatusCode = 503;
        context.Response.ContentType = "application/json";
        return context.Response.WriteAsync(JsonSerializer.Serialize(new { error = message, code }, JsonOptions));
    }
}
