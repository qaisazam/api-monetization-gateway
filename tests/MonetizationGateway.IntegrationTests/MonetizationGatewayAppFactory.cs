using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MonetizationGateway.Data;
using MonetizationGateway.Models;

namespace MonetizationGateway.IntegrationTests;

public class MonetizationGatewayAppFactory : WebApplicationFactory<Program>
{
    public static string Sha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Server=(localdb)\\mssqllocaldb;Database=MonetizationGateway_Test;Trusted_Connection=true;TrustServerCertificate=true;",
                ["Redis:Configuration"] = "localhost:6379",
                ["InternalApi:BaseUrl"] = "http://localhost:5000"
            });
        });
        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (descriptor != null) services.Remove(descriptor);
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=MonetizationGateway_Test;Trusted_Connection=true;TrustServerCertificate=true;"));
        });
    }

    public async Task SeedTestCustomerAsync(string apiKey = "test-key")
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await db.Database.EnsureCreatedAsync();
        if (await db.Customers.AnyAsync(c => c.ApiKeyHash == Sha256Hash(apiKey)))
            return;
        db.Customers.Add(new Customer
        {
            ExternalId = "test-customer",
            TierId = 1,
            ApiKeyHash = Sha256Hash(apiKey),
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
