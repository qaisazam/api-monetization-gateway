using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MonetizationGateway.Constants;
using MonetizationGateway.Data;
using Xunit;

namespace MonetizationGateway.IntegrationTests;

/// <summary>Integration tests for the gateway pipeline: auth, rate limit, usage logging, and API response contract (status, body, headers).</summary>
public class GatewayIntegrationTests : IClassFixture<MonetizationGatewayAppFactory>, IAsyncLifetime
{
    private readonly MonetizationGatewayAppFactory _factory;
    private readonly HttpClient _client;

    public GatewayIntegrationTests(MonetizationGatewayAppFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.SeedTestCustomerAsync("test-key");

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Health_WhenCalled_Returns200_WithoutApiKey()
    {
        _client.DefaultRequestHeaders.Clear();

        var response = await _client.GetAsync(ApiConstants.Paths.Health);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Request_WithoutApiKey_Returns401_WithMISSING_API_KEY_Code()
    {
        _client.DefaultRequestHeaders.Clear();

        var response = await _client.GetAsync("/internal/stub");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/json");
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.GetProperty("error").GetString().Should().NotBeNullOrEmpty();
        json.GetProperty("code").GetString().Should().Be(ApiConstants.ErrorCodes.MissingApiKey);
    }

    [Fact]
    public async Task Request_WithInvalidApiKey_Returns401_WithINVALID_API_KEY_Code()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add(ApiConstants.Headers.ApiKey, "invalid-key");

        var response = await _client.GetAsync("/internal/stub");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType?.MediaType.Should().Contain("application/json");
        var body = await response.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.GetProperty("error").GetString().Should().Be("Invalid API key.");
        json.GetProperty("code").GetString().Should().Be(ApiConstants.ErrorCodes.InvalidApiKey);
    }

    [Fact]
    public async Task Request_WithValidApiKey_Returns200_AndLogsUsage()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add(ApiConstants.Headers.ApiKey, "test-key");

        var response = await _client.GetAsync("/internal/stub");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logs = await db.ApiUsageLogs.Where(l => l.Endpoint == "/internal/stub").ToListAsync();
        logs.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Request_WhenOverRateLimit_Returns429_WithContractHeadersAndBody()
    {
        // Free tier allows 2 req/s; send 3 in quick succession so one gets 429
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add(ApiConstants.Headers.ApiKey, "test-key");
        var tasks = Enumerable.Range(0, 3).Select(_ => _client.GetAsync("/internal/stub")).ToArray();
        var responses = await Task.WhenAll(tasks);
        var rateLimited = responses.FirstOrDefault(r => r.StatusCode == HttpStatusCode.TooManyRequests);
        rateLimited.Should().NotBeNull("one of the requests should be rate limited (429)");

        // Headers (ApiResponse + ApiConstants)
        rateLimited!.Headers.TryGetValues(ApiConstants.Headers.RetryAfter, out var retryAfter).Should().BeTrue();
        retryAfter.Should().NotBeEmpty();
        rateLimited.Headers.TryGetValues(ApiConstants.Headers.RateLimitReset, out var reset).Should().BeTrue();
        reset.Should().NotBeEmpty();
        rateLimited.Headers.GetValues(ApiConstants.Headers.RateLimitLimit).FirstOrDefault().Should().NotBeNullOrEmpty();
        rateLimited.Headers.GetValues(ApiConstants.Headers.RateLimitRemaining).FirstOrDefault().Should().Be("0");

        // Body: error, limit, remaining, resetAt, retryAfter (ApiResponse contract)
        var body = await rateLimited.Content.ReadAsStringAsync();
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.TryGetProperty("error", out _).Should().BeTrue();
        json.TryGetProperty("limit", out _).Should().BeTrue();
        json.TryGetProperty("remaining", out _).Should().BeTrue();
        json.TryGetProperty("resetAt", out _).Should().BeTrue();
        json.TryGetProperty("retryAfter", out _).Should().BeTrue();
    }
}
