namespace MonetizationGateway.Configuration;

/// <summary>Options for tier resolution (cache TTL).</summary>
public class TierResolverOptions
{
    public const string SectionName = "TierResolver";

    /// <summary>Cache TTL in minutes for resolved tier config. Default: 2.</summary>
    public int CacheTtlMinutes { get; set; } = 2;
}
