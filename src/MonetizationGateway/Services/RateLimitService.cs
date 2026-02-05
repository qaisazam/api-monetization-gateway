using Microsoft.Extensions.Options;
using MonetizationGateway.Configuration;
using MonetizationGateway.Models;
using StackExchange.Redis;

namespace MonetizationGateway.Services;

/// <summary>Checks and consumes per-second rate limit and (optionally) monthly quota in Redis.</summary>
public class RateLimitService : IRateLimitService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly RateLimitOptions _options;
    private readonly ILogger<RateLimitService> _logger;
    private const string RateLimitKeyPrefix = "ratelimit:";
    private const string QuotaKeyPrefix = "quota:";

    public RateLimitService(
        IConnectionMultiplexer redis,
        IOptions<RateLimitOptions> options,
        ILogger<RateLimitService> logger)
    {
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<RateLimitResult> CheckAndConsumeReqSecAsync(int customerId, TierConfig tier, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;
        var windowSeconds = _options.SlidingWindowSeconds;

        var rateLimitKey = $"{RateLimitKeyPrefix}{customerId}:second";
        var quotaKey = $"{QuotaKeyPrefix}{customerId}:{year}:{month}";

        // 1) Check req/s (sliding window)
        var windowStart = now.AddSeconds(-windowSeconds);
        var reqCount = await GetSlidingWindowCountAsync(db, rateLimitKey, windowStart, cancellationToken);
        if (reqCount >= tier.RequestsPerSecond)
        {
            _logger.LogWarning(
                "Rate limit exceeded for CustomerId {CustomerId}: Limit={Limit}, Current={Current}",
                customerId, tier.RequestsPerSecond, reqCount);
            return new RateLimitResult(false, 1, tier.RequestsPerSecond, 0, now.AddSeconds(1), false);
        }

        // 2) Check monthly quota (if enabled)
        int quotaCount = 0;
        if (_options.EnableQuotaChecking)
        {
            var quotaVal = await db.StringGetAsync(quotaKey);
            quotaCount = quotaVal.HasValue ? (int)quotaVal : 0;
            if (quotaCount >= tier.MonthlyQuota)
            {
                _logger.LogWarning(
                    "Monthly quota exceeded for CustomerId {CustomerId}: Limit={Limit}, Current={Current}",
                    customerId, tier.MonthlyQuota, quotaCount);
                var endOfMonth = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);
                return new RateLimitResult(false, (int)(endOfMonth - now).TotalSeconds, tier.MonthlyQuota, 0, endOfMonth, true);
            }
        }

        // 3) Consume req/s slot only (monthly quota incremented after successful response)
        await AddToSlidingWindowAsync(db, rateLimitKey, now, windowSeconds, cancellationToken);

        var endOfMonthReset = new DateTime(year, month, DateTime.DaysInMonth(year, month), 23, 59, 59, DateTimeKind.Utc);
        var remaining = _options.EnableQuotaChecking ? tier.MonthlyQuota - quotaCount - 1 : tier.MonthlyQuota;
        return new RateLimitResult(true, null, tier.MonthlyQuota, remaining, endOfMonthReset, false);
    }

    /// <inheritdoc />
    public async Task IncrementMonthlyQuotaAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var db = _redis.GetDatabase();
        var now = DateTime.UtcNow;
        var quotaKey = $"{QuotaKeyPrefix}{customerId}:{now.Year}:{now.Month}";
        await db.StringIncrementAsync(quotaKey);
        var ttl = await db.KeyTimeToLiveAsync(quotaKey);
        if (!ttl.HasValue)
        {
            var endOfMonth = new DateTime(now.Year, now.Month, DateTime.DaysInMonth(now.Year, now.Month), 23, 59, 59, DateTimeKind.Utc);
            await db.KeyExpireAsync(quotaKey, endOfMonth - now);
        }
    }

    private static async Task<long> GetSlidingWindowCountAsync(IDatabase db, string key, DateTime windowStart, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        await db.SortedSetRemoveRangeByScoreAsync(key, 0, windowStart.Ticks);
        return await db.SortedSetLengthAsync(key);
    }

    private static async Task AddToSlidingWindowAsync(IDatabase db, string key, DateTime timestamp, int windowSeconds, CancellationToken ct)
    {
        await db.SortedSetAddAsync(key, timestamp.Ticks, timestamp.Ticks);
        await db.KeyExpireAsync(key, TimeSpan.FromSeconds(windowSeconds + 1));
    }
}
