using MonetizationGateway.Constants;
using MonetizationGateway.Responses;
using MonetizationGateway.Services;

namespace MonetizationGateway.Middleware;

/// <summary>Enforces per-second and monthly quota rate limits; returns 403 if tier not found, 429 if over limit.</summary>
public class RateLimitMiddleware
{
    private readonly RequestDelegate _next;

    public RateLimitMiddleware(RequestDelegate next) => _next = next ?? throw new ArgumentNullException(nameof(next));

    public async Task InvokeAsync(HttpContext context, GatewayRequestContext requestContext, IRateLimitService rateLimit, ITierResolver tierResolver)
    {
        if (!requestContext.IsAuthenticated)
        {
            await _next(context);
            return;
        }

        var tier = await tierResolver.GetTierConfigForCustomerAsync(requestContext.CustomerId!.Value, context.RequestAborted);
        if (tier == null)
        {
            await ApiResponse.WriteForbiddenAsync(context, "Tier not found.", ApiConstants.ErrorCodes.TierNotFound);
            return;
        }

        requestContext.TierConfig = tier;
        var result = await rateLimit.CheckAndConsumeReqSecAsync(requestContext.CustomerId.Value, tier, context.RequestAborted);

        if (!result.Allowed)
        {
            await ApiResponse.WriteTooManyRequestsAsync(
                context,
                result.IsQuotaExceeded ? "Monthly quota exceeded." : "Too many requests.",
                result.Limit,
                0,
                result.ResetAt,
                result.RetryAfterSeconds);
            return;
        }

        context.Response.Headers[ApiConstants.Headers.RateLimitLimit] = result.Limit.ToString();
        context.Response.Headers[ApiConstants.Headers.RateLimitRemaining] = result.Remaining.ToString();
        if (result.ResetAt.HasValue)
            context.Response.Headers[ApiConstants.Headers.RateLimitReset] = new DateTimeOffset(result.ResetAt.Value).ToUnixTimeSeconds().ToString();

        await _next(context);
    }
}
