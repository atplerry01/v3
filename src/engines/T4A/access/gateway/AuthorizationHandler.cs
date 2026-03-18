namespace Whycespace.Engines.T4A.Access.Gateway;

public sealed class AuthorizationHandler
{
    private static readonly IReadOnlyDictionary<string, string[]> AreaRoles =
        new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["economic"] = ["admin", "investor", "operator"],
            ["property"] = ["admin", "operator", "tenant"],
            ["identity"] = ["admin", "operator"],
            ["workforce"] = ["admin", "operator"],
            ["analytics"] = ["admin", "investor", "operator", "analyst"],
            ["monitoring"] = ["admin", "operator"],
            ["reports"] = ["admin", "investor", "operator", "auditor"]
        };

    public AuthorizationResult Authorize(string? area, IReadOnlyList<string> userRoles)
    {
        if (string.IsNullOrEmpty(area))
            return AuthorizationResult.Fail("Unknown route area");

        if (!AreaRoles.TryGetValue(area, out var allowedRoles))
            return AuthorizationResult.Fail($"No access policy defined for area: {area}");

        var hasRole = userRoles.Any(role =>
            allowedRoles.Any(allowed => string.Equals(role, allowed, StringComparison.OrdinalIgnoreCase)));

        return hasRole
            ? AuthorizationResult.Ok()
            : AuthorizationResult.Fail($"Insufficient permissions for area: {area}");
    }
}

public sealed record AuthorizationResult(bool Success, string? Error)
{
    public static AuthorizationResult Ok() => new(true, null);
    public static AuthorizationResult Fail(string error) => new(false, error);
}
