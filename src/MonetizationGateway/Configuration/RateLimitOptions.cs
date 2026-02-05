namespace MonetizationGateway.Configuration;

/// <summary>Options for rate limiting (sliding window and quota check).</summary>
public class RateLimitOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Sliding window duration in seconds for per-second rate limit. Default: 1.</summary>
    public int SlidingWindowSeconds { get; set; } = 1;

    /// <summary>Whether to check monthly quota. Default: true.</summary>
    public bool EnableQuotaChecking { get; set; } = true;
}
