using Microsoft.EntityFrameworkCore;
using MonetizationGateway.Models;

namespace MonetizationGateway.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Tier> Tiers => Set<Tier>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<ApiUsageLog> ApiUsageLogs => Set<ApiUsageLog>();
    public DbSet<MonthlyUsageSummary> MonthlyUsageSummaries => Set<MonthlyUsageSummary>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tier>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(64);
            e.Property(x => x.MonthlyPriceUsd).HasPrecision(18, 2);
        });

        modelBuilder.Entity<Customer>(e =>
        {
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ApiKeyHash).IsUnique();
            e.Property(x => x.ExternalId).HasMaxLength(128);
            e.Property(x => x.ApiKeyHash).HasMaxLength(64);
            e.HasOne(x => x.Tier).WithMany(t => t.Customers).HasForeignKey(x => x.TierId);
        });

        modelBuilder.Entity<ApiUsageLog>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId).HasMaxLength(256);
            e.Property(x => x.Endpoint).HasMaxLength(512);
            e.Property(x => x.Method).HasMaxLength(16);
            e.HasOne(x => x.Customer).WithMany(c => c.ApiUsageLogs).HasForeignKey(x => x.CustomerId);
        });

        modelBuilder.Entity<MonthlyUsageSummary>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.AmountUsd).HasPrecision(18, 2);
            e.Property(x => x.EndpointBreakdown).HasMaxLength(4000);
            e.HasIndex(x => new { x.CustomerId, x.Year, x.Month }).IsUnique();
            e.HasOne(x => x.Customer).WithMany(c => c.MonthlyUsageSummaries).HasForeignKey(x => x.CustomerId);
        });
    }
}
