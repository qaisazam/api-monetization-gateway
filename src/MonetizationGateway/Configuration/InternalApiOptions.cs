namespace MonetizationGateway.Configuration;

/// <summary>Options for the internal API (proxy target).</summary>
public class InternalApiOptions
{
    public const string SectionName = "InternalApi";

    /// <summary>Base URL of the internal API to proxy requests to.</summary>
    public string BaseUrl { get; set; } = "http://localhost:5000";
}
