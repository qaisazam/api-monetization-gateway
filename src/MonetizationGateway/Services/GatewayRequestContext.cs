using MonetizationGateway.Models;

namespace MonetizationGateway.Services;

/// <summary>Scoped context for the current request: set by Auth and TierResolver, read by RateLimit and UsageLogging.</summary>
public class GatewayRequestContext
{
    /// <summary>Customer ID resolved from X-Api-Key (null if unauthenticated).</summary>
    public int? CustomerId { get; set; }

    /// <summary>Optional user ID from X-User-Id header.</summary>
    public string? UserId { get; set; }

    /// <summary>Resolved tier config (set by RateLimitMiddleware after tier resolution).</summary>
    public TierConfig? TierConfig { get; set; }

    /// <summary>True when CustomerId has been set by AuthMiddleware.</summary>
    public bool IsAuthenticated => CustomerId.HasValue;
}
