namespace MonetizationGateway.Services;

/// <summary>Logs API usage to the database and increments monthly quota in Redis.</summary>
public interface IUsageTrackingService
{
    /// <summary>Log a successful API call and increment monthly quota in Redis.</summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="userId">Optional user identifier (e.g. from X-User-Id header).</param>
    /// <param name="endpoint">Request path/endpoint.</param>
    /// <param name="method">HTTP method.</param>
    /// <param name="responseStatus">Response status code.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task LogUsageAsync(int customerId, string? userId, string endpoint, string method, int responseStatus, CancellationToken cancellationToken = default);
}
