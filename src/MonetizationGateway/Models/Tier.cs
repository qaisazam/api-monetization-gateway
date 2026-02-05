namespace MonetizationGateway.Models;

public class Tier
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int MonthlyQuota { get; set; }
    public int RequestsPerSecond { get; set; }
    public decimal MonthlyPriceUsd { get; set; }

    public ICollection<Customer> Customers { get; set; } = new List<Customer>();
}
