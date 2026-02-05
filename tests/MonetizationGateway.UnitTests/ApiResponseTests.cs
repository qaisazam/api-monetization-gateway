using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using MonetizationGateway.Constants;
using MonetizationGateway.Responses;
using Xunit;

namespace MonetizationGateway.UnitTests;

/// <summary>Tests for centralized ApiResponse helpers (consistent JSON shape and status/headers).</summary>
public class ApiResponseTests
{
    private static HttpContext CreateContextWithWritableBody()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();
        return context;
    }

    private static async Task<string> ReadResponseBodyAsync(HttpContext context)
    {
        context.Response.Body.Position = 0;
        using var reader = new StreamReader(context.Response.Body);
        return await reader.ReadToEndAsync();
    }

    [Fact]
    public async Task WriteUnauthorizedAsync_WhenCalled_Sets401_AndBodyWithErrorAndCode()
    {
        // Arrange
        var context = CreateContextWithWritableBody();
        var message = "Missing or invalid API key.";
        var code = ApiConstants.ErrorCodes.MissingApiKey;

        // Act
        await ApiResponse.WriteUnauthorizedAsync(context, message, code);

        // Assert
        context.Response.StatusCode.Should().Be(401);
        context.Response.ContentType.Should().Contain("application/json");
        var body = await ReadResponseBodyAsync(context);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.GetProperty("error").GetString().Should().Be(message);
        json.GetProperty("code").GetString().Should().Be(code);
    }

    [Fact]
    public async Task WriteForbiddenAsync_WhenCalled_Sets403_AndBodyWithErrorAndCode()
    {
        // Arrange
        var context = CreateContextWithWritableBody();
        var message = "Tier not found.";
        var code = ApiConstants.ErrorCodes.TierNotFound;

        // Act
        await ApiResponse.WriteForbiddenAsync(context, message, code);

        // Assert
        context.Response.StatusCode.Should().Be(403);
        context.Response.ContentType.Should().Contain("application/json");
        var body = await ReadResponseBodyAsync(context);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.GetProperty("error").GetString().Should().Be(message);
        json.GetProperty("code").GetString().Should().Be(code);
    }

    [Fact]
    public async Task WriteTooManyRequestsAsync_WhenCalled_Sets429_HeadersAndBody()
    {
        // Arrange
        var context = CreateContextWithWritableBody();
        var error = "Too many requests.";
        var limit = 10;
        var remaining = 0;
        var resetAt = new DateTime(2025, 2, 28, 23, 59, 59, DateTimeKind.Utc);
        var retryAfterSeconds = 1;

        // Act
        await ApiResponse.WriteTooManyRequestsAsync(context, error, limit, remaining, resetAt, retryAfterSeconds);

        // Assert
        context.Response.StatusCode.Should().Be(429);
        context.Response.ContentType.Should().Contain("application/json");
        context.Response.Headers[ApiConstants.Headers.RetryAfter].ToString().Should().Be("1");
        context.Response.Headers[ApiConstants.Headers.RateLimitLimit].ToString().Should().Be("10");
        context.Response.Headers[ApiConstants.Headers.RateLimitRemaining].ToString().Should().Be("0");
        context.Response.Headers[ApiConstants.Headers.RateLimitReset].ToString().Should().NotBeNullOrEmpty();

        var body = await ReadResponseBodyAsync(context);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.GetProperty("error").GetString().Should().Be(error);
        json.GetProperty("limit").GetInt32().Should().Be(limit);
        json.GetProperty("remaining").GetInt32().Should().Be(remaining);
        json.TryGetProperty("retryAfter", out var ra).Should().BeTrue();
        ra.GetInt32().Should().Be(retryAfterSeconds);
        json.TryGetProperty("resetAt", out _).Should().BeTrue();
    }

    [Fact]
    public async Task WriteInternalErrorAsync_WhenCalled_Sets500_AndBodyWithCode()
    {
        // Arrange
        var context = CreateContextWithWritableBody();

        // Act
        await ApiResponse.WriteInternalErrorAsync(context, "An error occurred.", ApiConstants.ErrorCodes.InternalError);

        // Assert
        context.Response.StatusCode.Should().Be(500);
        var body = await ReadResponseBodyAsync(context);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.GetProperty("code").GetString().Should().Be(ApiConstants.ErrorCodes.InternalError);
    }

    [Fact]
    public async Task WriteServiceUnavailableAsync_WhenCalled_Sets503_AndBodyWithCode()
    {
        // Arrange
        var context = CreateContextWithWritableBody();

        // Act
        await ApiResponse.WriteServiceUnavailableAsync(context, "Rate limit service unavailable.", ApiConstants.ErrorCodes.RateLimitUnavailable);

        // Assert
        context.Response.StatusCode.Should().Be(503);
        var body = await ReadResponseBodyAsync(context);
        var json = JsonSerializer.Deserialize<JsonElement>(body);
        json.GetProperty("code").GetString().Should().Be(ApiConstants.ErrorCodes.RateLimitUnavailable);
    }
}
