using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MonetizationGateway.Configuration;
using MonetizationGateway.Data;
using MonetizationGateway.Models;

namespace MonetizationGateway.Jobs;

/// <summary>Background job that aggregates monthly usage from logs into MonthlyUsageSummary records.</summary>
public class MonthlyUsageSummaryJob : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<MonthlyUsageSummaryJob> _logger;
    private readonly MonthlyJobOptions _options;

    public MonthlyUsageSummaryJob(IServiceProvider services, ILogger<MonthlyUsageSummaryJob> logger, IOptions<MonthlyJobOptions> options)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var interval = TimeSpan.FromHours(_options.SummaryJobIntervalHours);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunSummaryAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Monthly summary job failed");
            }

            await Task.Delay(interval, stoppingToken);
        }
    }

    private async Task RunSummaryAsync(CancellationToken ct)
    {
        await using var scope = _services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var now = DateTime.UtcNow;
        var year = now.Year;
        var month = now.Month;

        var customers = await db.Customers.Include(c => c.Tier).ToListAsync(ct);
        foreach (var customer in customers)
        {
            var logs = await db.ApiUsageLogs
                .Where(l => l.CustomerId == customer.Id && l.Timestamp.Year == year && l.Timestamp.Month == month)
                .ToListAsync(ct);

            if (logs.Count == 0)
                continue;

            var totalRequests = logs.Count;
            var breakdown = logs
                .GroupBy(l => $"{l.Method} {l.Endpoint}")
                .ToDictionary(g => g.Key, g => g.Count());
            var endpointBreakdownJson = JsonSerializer.Serialize(breakdown);
            var amountUsd = customer.Tier.MonthlyPriceUsd;

            var existing = await db.MonthlyUsageSummaries
                .FirstOrDefaultAsync(s => s.CustomerId == customer.Id && s.Year == year && s.Month == month, ct);

            if (existing != null)
            {
                existing.TotalRequests = totalRequests;
                existing.EndpointBreakdown = endpointBreakdownJson;
                existing.AmountUsd = amountUsd;
            }
            else
            {
                db.MonthlyUsageSummaries.Add(new MonthlyUsageSummary
                {
                    CustomerId = customer.Id,
                    Year = year,
                    Month = month,
                    TotalRequests = totalRequests,
                    EndpointBreakdown = endpointBreakdownJson,
                    AmountUsd = amountUsd
                });
            }
        }

        await db.SaveChangesAsync(ct);
        _logger.LogInformation("Monthly summary job completed for {Year}-{Month}", year, month);
    }
}
