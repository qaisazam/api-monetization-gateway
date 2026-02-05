using Microsoft.EntityFrameworkCore;
using MonetizationGateway.Configuration;
using MonetizationGateway.Data;
using MonetizationGateway.Jobs;
using MonetizationGateway.Services;
using StackExchange.Redis;

namespace MonetizationGateway.Extensions;

/// <summary>DI registration for the monetization gateway (DbContext, Redis, options, services, job, HttpClient).</summary>
public static class ServiceCollectionExtensions
{
    /// <summary>Adds all gateway services, options, and hosted job. Call once during app configuration.</summary>
    public static IServiceCollection AddMonetizationGateway(this IServiceCollection services, IConfiguration configuration)
    {
        // DbContext
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        // Redis
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));
        var redisConfig = configuration["Redis:Configuration"] ?? "localhost:6379";
        services.AddSingleton<IConnectionMultiplexer>(_ => ConnectionMultiplexer.Connect(redisConfig));

        // Memory cache
        services.AddMemoryCache();

        // Options
        services.Configure<RateLimitOptions>(configuration.GetSection(RateLimitOptions.SectionName));
        services.Configure<TierResolverOptions>(configuration.GetSection(TierResolverOptions.SectionName));
        services.Configure<MonthlyJobOptions>(configuration.GetSection(MonthlyJobOptions.SectionName));
        services.Configure<InternalApiOptions>(configuration.GetSection(InternalApiOptions.SectionName));

        // Request context and services
        services.AddScoped<GatewayRequestContext>();
        services.AddScoped<ITierResolver, TierResolver>();
        services.AddScoped<IRateLimitService, RateLimitService>();
        services.AddScoped<IUsageTrackingService, UsageTrackingService>();

        // Background job
        services.AddHostedService<MonthlyUsageSummaryJob>();

        services.AddHttpClient();

        return services;
    }
}
