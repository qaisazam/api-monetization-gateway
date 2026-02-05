namespace MonetizationGateway.Models;

public class Customer
{
    public int Id { get; set; }
    public string ExternalId { get; set; } = string.Empty;
    public int TierId { get; set; }
    /// <summary>SHA256 hash of the API key; never store the plain key.</summary>
    public string ApiKeyHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }

    public Tier Tier { get; set; } = null!;
    public ICollection<ApiUsageLog> ApiUsageLogs { get; set; } = new List<ApiUsageLog>();
    public ICollection<MonthlyUsageSummary> MonthlyUsageSummaries { get; set; } = new List<MonthlyUsageSummary>();
}
