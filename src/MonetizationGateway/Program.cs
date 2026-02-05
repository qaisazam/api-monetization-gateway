using MonetizationGateway.Constants;
using MonetizationGateway.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMonetizationGateway(builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();
app.UseMonetizationGatewayPipeline();

app.MapGet(ApiConstants.Paths.Health, () => Results.Ok(new { status = "Healthy" })).AllowAnonymous();

app.MapGet("/internal/stub", () => Results.Ok(new { message = "OK", timestamp = DateTime.UtcNow }));

var internalBaseUrl = builder.Configuration["InternalApi:BaseUrl"]?.TrimEnd('/') ?? "http://localhost:5000";
app.MapFallback(async (HttpContext context, IHttpClientFactory factory) =>
{
    var client = factory.CreateClient();
    var path = context.Request.Path.Value ?? "/";
    var query = context.Request.QueryString.Value ?? "";
    var url = $"{internalBaseUrl}{path}{query}";
    var request = new HttpRequestMessage(new HttpMethod(context.Request.Method), url);
    foreach (var header in context.Request.Headers.Where(h => !string.Equals(h.Key, "Host", StringComparison.OrdinalIgnoreCase)))
        request.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
    if (context.Request.ContentLength > 0 && context.Request.Body.CanRead)
        request.Content = new StreamContent(context.Request.Body) { Headers = { { "Content-Type", context.Request.ContentType ?? "application/octet-stream" } } };
    var response = await client.SendAsync(request, context.RequestAborted);
    foreach (var header in response.Headers)
        context.Response.Headers[header.Key] = header.Value.ToArray();
    if (response.Content.Headers.ContentType != null)
        context.Response.ContentType = response.Content.Headers.ContentType.ToString();
    context.Response.StatusCode = (int)response.StatusCode;
    await response.Content.CopyToAsync(context.Response.Body);
});

app.Run();

/// <summary>Exposed for integration tests (WebApplicationFactory).</summary>
public partial class Program { }
