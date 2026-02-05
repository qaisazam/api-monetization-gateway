namespace MonetizationGateway.Models;

/// <summary>Configuration passed to rate limiter (MonthlyQuota, RequestsPerSecond, MonthlyPriceUsd).</summary>
public class TierConfig
{
    public int MonthlyQuota { get; set; }
    public int RequestsPerSecond { get; set; }
    public decimal MonthlyPriceUsd { get; set; }
}
