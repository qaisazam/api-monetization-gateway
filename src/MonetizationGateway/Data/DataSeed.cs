using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MonetizationGateway.Models;

namespace MonetizationGateway.Data;

/// <summary>Runtime seed: ensures tiers and test customers exist at startup.</summary>
public static class DataSeed
{
    /// <summary>API keys for seeded customers. Free = sk_mgw_7f3a9b2c4e1d8f6a, Pro = sk_mgw_a8c2e5f1b9d4e7a3.</summary>
    public static class CustomerKeys
    {
        public const string Free = "sk_mgw_7f3a9b2c4e1d8f6a";
        public const string Pro = "sk_mgw_a8c2e5f1b9d4e7a3";
    }

    public static async Task EnsureSeedAsync(AppDbContext db, CancellationToken cancellationToken = default)
    {
        await EnsureTiersAsync(db, cancellationToken);
        await EnsureCustomersAsync(db, cancellationToken);
    }

    private static async Task EnsureTiersAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var hasFree = await db.Tiers.AnyAsync(t => t.Name == "Free", cancellationToken);
        var hasPro = await db.Tiers.AnyAsync(t => t.Name == "Pro", cancellationToken);

        if (!hasFree)
            db.Tiers.Add(new Tier { Name = "Free", MonthlyQuota = 1000, RequestsPerSecond = 2, MonthlyPriceUsd = 0 });
        if (!hasPro)
            db.Tiers.Add(new Tier { Name = "Pro", MonthlyQuota = 100_000, RequestsPerSecond = 10, MonthlyPriceUsd = 50 });

        if (!hasFree || !hasPro)
            await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureCustomersAsync(AppDbContext db, CancellationToken cancellationToken)
    {
        var freeTierId = await db.Tiers.Where(t => t.Name == "Free").Select(t => t.Id).FirstOrDefaultAsync(cancellationToken);
        var proTierId = await db.Tiers.Where(t => t.Name == "Pro").Select(t => t.Id).FirstOrDefaultAsync(cancellationToken);
        if (freeTierId == 0 || proTierId == 0)
            return;

        var freeHash = ApiKeyHash(CustomerKeys.Free);
        var proHash = ApiKeyHash(CustomerKeys.Pro);

        var existing = await db.Customers
            .Where(c => c.ApiKeyHash == freeHash || c.ApiKeyHash == proHash)
            .Select(c => c.ApiKeyHash)
            .ToListAsync(cancellationToken);

        if (existing.Count >= 2)
            return;

        var seedDate = DateTime.UtcNow;
        if (!existing.Contains(freeHash))
        {
            db.Customers.Add(new Customer
            {
                ExternalId = "seed-free-tier",
                TierId = freeTierId,
                ApiKeyHash = freeHash,
                CreatedAt = seedDate
            });
        }

        if (!existing.Contains(proHash))
        {
            db.Customers.Add(new Customer
            {
                ExternalId = "seed-pro-tier",
                TierId = proTierId,
                ApiKeyHash = proHash,
                CreatedAt = seedDate
            });
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static string ApiKeyHash(string apiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
