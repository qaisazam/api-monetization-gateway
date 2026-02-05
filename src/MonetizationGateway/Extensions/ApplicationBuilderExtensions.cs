using MonetizationGateway.Middleware;

namespace MonetizationGateway.Extensions;

/// <summary>Pipeline extension for the monetization gateway middleware (exception handling, auth, rate limit, usage logging).</summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>Adds gateway middleware in order: ExceptionHandling → Auth → RateLimit → UsageLogging.</summary>
    public static IApplicationBuilder UseMonetizationGatewayPipeline(this IApplicationBuilder app)
    {
        app.UseMiddleware<ExceptionHandlingMiddleware>();
        app.UseMiddleware<AuthMiddleware>();
        app.UseMiddleware<RateLimitMiddleware>();
        app.UseMiddleware<UsageLoggingMiddleware>();
        return app;
    }
}
