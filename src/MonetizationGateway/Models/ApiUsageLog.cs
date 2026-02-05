namespace MonetizationGateway.Models;

public class ApiUsageLog
{
    public int Id { get; set; }
    public int CustomerId { get; set; }
    /// <summary>Optional user context from headers/claims (e.g. X-User-Id); null if not provided.</summary>
    public string? UserId { get; set; }
    public string Endpoint { get; set; } = string.Empty;
    public string Method { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public int ResponseStatus { get; set; }

    public Customer Customer { get; set; } = null!;
}
