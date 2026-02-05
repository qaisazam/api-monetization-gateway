using MonetizationGateway.Models;

namespace MonetizationGateway.Services;

/// <summary>Resolves tier configuration for a customer from the database (with optional cache).</summary>
public interface ITierResolver
{
    /// <summary>Resolve tier config for the given customer (from DB/cache). Returns null if customer not found.</summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Tier config or null if customer/tier not found.</returns>
    Task<TierConfig?> GetTierConfigForCustomerAsync(int customerId, CancellationToken cancellationToken = default);
}
