namespace MonetizationGateway.Models;

public class MonthlyUsageSummary
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    public int Year { get; set; }
    public int Month { get; set; }
    public int TotalRequests { get; set; }
    /// <summary>JSON string: e.g. {"GET /api/data": 1500, "POST /api/submit": 200}</summary>
    public string EndpointBreakdown { get; set; } = "{}";
    public decimal AmountUsd { get; set; }

    public Customer Customer { get; set; } = null!;
}
