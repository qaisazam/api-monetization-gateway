using MonetizationGateway.Models;

namespace MonetizationGateway.Services;

/// <summary>Checks and consumes per-second rate limit and monthly quota in Redis.</summary>
public interface IRateLimitService
{
    /// <summary>Check rate limit (req/s + monthly quota). Consumes req/s slot if allowed; monthly quota is incremented only after successful response via IncrementMonthlyQuotaAsync.</summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="tier">Tier configuration (limits).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result indicating allowed/denied, limit/remaining, reset time, and whether quota was exceeded.</returns>
    /// <exception cref="StackExchange.Redis.RedisConnectionException">Redis is unavailable.</exception>
    /// <exception cref="StackExchange.Redis.RedisException">Redis operation failed.</exception>
    Task<RateLimitResult> CheckAndConsumeReqSecAsync(int customerId, TierConfig tier, CancellationToken cancellationToken = default);

    /// <summary>Increment monthly quota counter in Redis (call only after successful request).</summary>
    /// <param name="customerId">Customer identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <exception cref="StackExchange.Redis.RedisConnectionException">Redis is unavailable.</exception>
    Task IncrementMonthlyQuotaAsync(int customerId, CancellationToken cancellationToken = default);
}

/// <summary>Result of a rate limit check: allowed/denied, limit/remaining counts, reset time, and quota-exceeded flag.</summary>
/// <param name="Allowed">Whether the request is allowed.</param>
/// <param name="RetryAfterSeconds">Suggested retry-after in seconds (for 429 responses).</param>
/// <param name="Limit">Configured limit (req/s or monthly quota depending on context).</param>
/// <param name="Remaining">Remaining allowance.</param>
/// <param name="ResetAt">When the limit resets (per-second or end of month).</param>
/// <param name="IsQuotaExceeded">True if denied due to monthly quota exceeded.</param>
public record RateLimitResult(bool Allowed, int? RetryAfterSeconds, int Limit, int Remaining, DateTime? ResetAt, bool IsQuotaExceeded);
