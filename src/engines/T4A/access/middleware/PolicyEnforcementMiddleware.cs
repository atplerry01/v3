namespace Whycespace.Engines.T4A.Access.Middleware;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Whycespace.Contracts.Runtime;

public sealed class PolicyEnforcementMiddleware
{
    private readonly RequestDelegate _next;

    public PolicyEnforcementMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPlatformDispatcher dispatcher)
    {
        var path = context.Request.Path.Value ?? "";

        // Skip policy enforcement for health checks and dev endpoints
        if (path.StartsWith("/health") || path.StartsWith("/dev"))
        {
            await _next(context);
            return;
        }

        var userId = context.Items.TryGetValue("UserId", out var uid) ? uid as string : null;
        if (string.IsNullOrEmpty(userId))
        {
            // No authenticated user — let auth middleware handle this
            await _next(context);
            return;
        }

        var result = await dispatcher.DispatchAsync("policy.enforce", new Dictionary<string, object>
        {
            ["subjectId"] = userId,
            ["resource"] = path,
            ["action"] = context.Request.Method
        });

        if (!result.Success)
        {
            context.Response.StatusCode = 403;
            context.Response.ContentType = "application/json";

            var correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id as string : "unknown";
            var error = new
            {
                success = false,
                error = result.Error ?? "Policy enforcement denied access",
                correlationId,
                timestamp = DateTimeOffset.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
            return;
        }

        await _next(context);
    }
}
