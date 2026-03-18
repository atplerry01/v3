namespace Whycespace.Engines.T4A.Access.Gateway;

using Microsoft.AspNetCore.Http;

public sealed class RequestRouter
{
    private static readonly IReadOnlyDictionary<string, string> RouteMap =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["/api/capital"] = "economic",
            ["/api/vault"] = "economic",
            ["/api/property"] = "property",
            ["/api/identity"] = "identity",
            ["/api/workforce"] = "workforce",
            ["/api/analytics"] = "analytics",
            ["/api/monitoring"] = "monitoring",
            ["/api/reports"] = "reports"
        };

    public string? ResolveArea(PathString path)
    {
        var pathValue = path.Value ?? "";
        foreach (var kvp in RouteMap)
        {
            if (pathValue.StartsWith(kvp.Key, StringComparison.OrdinalIgnoreCase))
                return kvp.Value;
        }
        return null;
    }

    public bool IsReadEndpoint(PathString path, string method)
        => string.Equals(method, "GET", StringComparison.OrdinalIgnoreCase)
           || (path.Value?.Contains("/analytics/") ?? false)
           || (path.Value?.Contains("/monitoring/") ?? false)
           || (path.Value?.Contains("/reports/") ?? false);
}
