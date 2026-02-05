namespace MonetizationGateway.Constants;

/// <summary>Header names, error codes, and paths used across the API.</summary>
public static class ApiConstants
{
    public static class Headers
    {
        public const string ApiKey = "X-Api-Key";
        public const string UserId = "X-User-Id";
        public const string RetryAfter = "Retry-After";
        public const string RateLimitLimit = "X-RateLimit-Limit";
        public const string RateLimitRemaining = "X-RateLimit-Remaining";
        public const string RateLimitReset = "X-RateLimit-Reset";
    }

    public static class ErrorCodes
    {
        public const string InvalidApiKey = "INVALID_API_KEY";
        public const string MissingApiKey = "MISSING_API_KEY";
        public const string TierNotFound = "TIER_NOT_FOUND";
        public const string RateLimitExceeded = "RATE_LIMIT_EXCEEDED";
        public const string QuotaExceeded = "QUOTA_EXCEEDED";
        public const string InternalError = "INTERNAL_ERROR";
        public const string RateLimitUnavailable = "RATE_LIMIT_UNAVAILABLE";
        public const string ServiceUnavailable = "SERVICE_UNAVAILABLE";
    }

    public static class Paths
    {
        public const string Health = "/health";
    }
}
