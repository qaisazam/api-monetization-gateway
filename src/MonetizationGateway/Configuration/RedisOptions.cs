namespace MonetizationGateway.Configuration;

/// <summary>Options for Redis connection (rate limiting store).</summary>
public class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>Redis connection string (e.g. localhost:6379).</summary>
    public string Configuration { get; set; } = "localhost:6379";
}
