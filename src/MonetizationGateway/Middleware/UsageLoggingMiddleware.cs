using MonetizationGateway.Services;

namespace MonetizationGateway.Middleware;

/// <summary>Logs successful API usage (awaited after response) and increments monthly quota; logs errors on failure.</summary>
public class UsageLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<UsageLoggingMiddleware> _logger;

    public UsageLoggingMiddleware(RequestDelegate next, ILogger<UsageLoggingMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, GatewayRequestContext requestContext, IUsageTrackingService usageTracking)
    {
        var path = context.Request.Path.Value ?? "/";
        var method = context.Request.Method;

        await _next(context);

        if (!requestContext.IsAuthenticated || requestContext.CustomerId == null)
            return;

        var status = context.Response.StatusCode;
        if (status >= 200 && status < 300)
        {
            try
            {
                await usageTracking.LogUsageAsync(
                    requestContext.CustomerId.Value,
                    requestContext.UserId,
                    path,
                    method,
                    status,
                    context.RequestAborted);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Usage logging failed for CustomerId {CustomerId}, Endpoint {Endpoint}", requestContext.CustomerId.Value, path);
            }
        }
    }
}
