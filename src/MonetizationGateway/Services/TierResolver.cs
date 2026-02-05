using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MonetizationGateway.Configuration;
using MonetizationGateway.Data;
using MonetizationGateway.Models;

namespace MonetizationGateway.Services;

/// <summary>Resolves tier config for a customer from DB with in-memory cache.</summary>
public class TierResolver : ITierResolver
{
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;
    private readonly TierResolverOptions _options;
    private readonly ILogger<TierResolver> _logger;

    public TierResolver(
        AppDbContext db,
        IMemoryCache cache,
        IOptions<TierResolverOptions> options,
        ILogger<TierResolver> logger)
    {
        _db = db ?? throw new ArgumentNullException(nameof(db));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task<TierConfig?> GetTierConfigForCustomerAsync(int customerId, CancellationToken cancellationToken = default)
    {
        var cacheKey = $"tier:{customerId}";
        if (_cache.TryGetValue(cacheKey, out TierConfig? cached))
            return cached;

        var customer = await _db.Customers
            .AsNoTracking()
            .Include(c => c.Tier)
            .FirstOrDefaultAsync(c => c.Id == customerId, cancellationToken);
        var tier = customer?.Tier;

        if (tier == null)
        {
            _logger.LogWarning("Tier not found for CustomerId {CustomerId}", customerId);
            return null;
        }

        var config = new TierConfig
        {
            MonthlyQuota = tier.MonthlyQuota,
            RequestsPerSecond = tier.RequestsPerSecond,
            MonthlyPriceUsd = tier.MonthlyPriceUsd
        };
        var ttl = TimeSpan.FromMinutes(_options.CacheTtlMinutes);
        _cache.Set(cacheKey, config, ttl);
        return config;
    }
}
