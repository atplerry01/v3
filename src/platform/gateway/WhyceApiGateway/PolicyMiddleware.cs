namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Whycespace.Contracts.Runtime;

public sealed class PolicyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IPlatformDispatcher _dispatcher;

    public PolicyMiddleware(
        RequestDelegate next,
        IPlatformDispatcher dispatcher)
    {
        _next = next;
        _dispatcher = dispatcher;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var actorId = context.Request.Headers["X-Actor-Id"].FirstOrDefault();
        var domain = context.Request.Headers["X-Policy-Domain"].FirstOrDefault();

        if (string.IsNullOrEmpty(actorId) || string.IsNullOrEmpty(domain))
        {
            await _next(context);
            return;
        }

        var attributes = new Dictionary<string, string>();
        foreach (var header in context.Request.Headers)
        {
            if (header.Key.StartsWith("X-Policy-Attr-", StringComparison.OrdinalIgnoreCase))
            {
                var key = header.Key["X-Policy-Attr-".Length..].ToLowerInvariant();
                attributes[key] = header.Value.ToString();
            }
        }

        var result = await _dispatcher.DispatchAsync("policy.enforce", new Dictionary<string, object>
        {
            ["actorId"] = actorId,
            ["domain"] = domain,
            ["operation"] = context.Request.Path.Value ?? string.Empty,
            ["attributes"] = attributes
        });

        var allowed = result.Success && result.Data.TryGetValue("allowed", out var allowedObj) && allowedObj is true;

        if (!allowed)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";
            await context.Response.WriteAsJsonAsync(new
            {
                allowed = false,
                reason = result.Data.TryGetValue("reason", out var reason) ? reason : "Policy evaluation denied access",
                evaluatedAt = result.Data.TryGetValue("evaluatedAt", out var evaluatedAt) ? evaluatedAt : DateTimeOffset.UtcNow
            });
            return;
        }

        await _next(context);
    }
}
