using MonetizationGateway.Data;
using MonetizationGateway.Models;

namespace MonetizationGateway.Services;

/// <summary>Logs API usage to the database and increments monthly quota in Redis.</summary>
public class UsageTrackingService : IUsageTrackingService
{
    private readonly AppDbContext _db;
    private readonly IRateLimitService _rateLimit;
    private readonly ILogger<UsageTrackingService> _logger;

    public UsageTrackingService(AppDbContext db, IRateLimitService rateLimit, ILogger<UsageTrackingService> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _rateLimit = rateLimit ?? throw new ArgumentNullException(nameof(rateLimit));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task LogUsageAsync(int customerId, string? userId, string endpoint, string method, int responseStatus, CancellationToken cancellationToken = default)
    {
        try
        {
            var log = new ApiUsageLog
            {
                CustomerId = customerId,
                UserId = userId,
                Endpoint = endpoint,
                Method = method,
                Timestamp = DateTime.UtcNow,
                ResponseStatus = responseStatus
            };
            _db.ApiUsageLogs.Add(log);
            await _db.SaveChangesAsync(cancellationToken);
            await _rateLimit.IncrementMonthlyQuotaAsync(customerId, cancellationToken);
            _logger.LogDebug("Usage logged for CustomerId {CustomerId}, Endpoint {Endpoint}", customerId, endpoint);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to log usage for CustomerId {CustomerId}, Endpoint {Endpoint}", customerId, endpoint);
            throw;
        }
    }
}
