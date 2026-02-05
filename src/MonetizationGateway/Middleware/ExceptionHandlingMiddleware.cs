using Microsoft.EntityFrameworkCore;
using MonetizationGateway.Constants;
using MonetizationGateway.Responses;
using StackExchange.Redis;

namespace MonetizationGateway.Middleware;

/// <summary>Catches unhandled exceptions and returns consistent JSON errors; differentiates Redis/DB for 503.</summary>
public class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            if (context.Response.HasStarted)
                return;

            if (ex is RedisConnectionException or RedisException)
            {
                await ApiResponse.WriteServiceUnavailableAsync(context, "Rate limit service unavailable.", ApiConstants.ErrorCodes.RateLimitUnavailable);
                return;
            }
            if (ex is DbUpdateException)
            {
                await ApiResponse.WriteServiceUnavailableAsync(context, "Service temporarily unavailable.", ApiConstants.ErrorCodes.ServiceUnavailable);
                return;
            }

            await ApiResponse.WriteInternalErrorAsync(context, "An error occurred.", ApiConstants.ErrorCodes.InternalError);
        }
    }
}
