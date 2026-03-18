namespace Whycespace.Engines.T4A.Access.Gateway;

using Microsoft.AspNetCore.Http;

public sealed class AuthenticationHandler
{
    private const string BearerPrefix = "Bearer ";

    public AuthenticationResult Authenticate(HttpContext context)
    {
        var authHeader = context.Request.Headers.Authorization.ToString();

        if (string.IsNullOrEmpty(authHeader))
            return AuthenticationResult.Fail("Missing Authorization header");

        if (!authHeader.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
            return AuthenticationResult.Fail("Invalid authorization scheme — Bearer expected");

        var token = authHeader[BearerPrefix.Length..].Trim();
        if (string.IsNullOrEmpty(token))
            return AuthenticationResult.Fail("Empty bearer token");

        // Token validation delegated to WhyceID via dispatcher
        // This handler only extracts and validates format
        return AuthenticationResult.Ok(token);
    }
}

public sealed record AuthenticationResult(bool Success, string? Token, string? Error)
{
    public static AuthenticationResult Ok(string token) => new(true, token, null);
    public static AuthenticationResult Fail(string error) => new(false, null, error);
}
