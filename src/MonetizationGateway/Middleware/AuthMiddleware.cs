using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using MonetizationGateway.Constants;
using MonetizationGateway.Data;
using MonetizationGateway.Responses;
using MonetizationGateway.Services;

namespace MonetizationGateway.Middleware;

/// <summary>Authenticates requests via X-Api-Key and populates GatewayRequestContext with CustomerId and UserId.</summary>
public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<AuthMiddleware> _logger;

    public AuthMiddleware(RequestDelegate next, ILogger<AuthMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context, AppDbContext db, GatewayRequestContext requestContext)
    {
        if (context.Request.Path.StartsWithSegments(ApiConstants.Paths.Health, StringComparison.OrdinalIgnoreCase))
        {
            await _next(context);
            return;
        }
        var apiKey = context.Request.Headers[ApiConstants.Headers.ApiKey].FirstOrDefault();
        if (string.IsNullOrEmpty(apiKey))
        {
            _logger.LogWarning("Request rejected: missing API key");
            await ApiResponse.WriteUnauthorizedAsync(context, "Missing or invalid API key.", ApiConstants.ErrorCodes.MissingApiKey);
            return;
        }

        var hash = ComputeSha256Hash(apiKey);
        var customer = await db.Customers.AsNoTracking().FirstOrDefaultAsync(c => c.ApiKeyHash == hash, context.RequestAborted);
        if (customer == null)
        {
            _logger.LogWarning("Request rejected: invalid API key");
            await ApiResponse.WriteUnauthorizedAsync(context, "Invalid API key.", ApiConstants.ErrorCodes.InvalidApiKey);
            return;
        }

        requestContext.CustomerId = customer.Id;
        requestContext.UserId = context.Request.Headers[ApiConstants.Headers.UserId].FirstOrDefault();
        await _next(context);
    }

    private static string ComputeSha256Hash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

}
