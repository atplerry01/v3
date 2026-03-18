namespace Whycespace.Engines.T4A.Access.Middleware;

using System.Text.Json;
using Microsoft.AspNetCore.Http;

public sealed class ValidationMiddleware
{
    private readonly RequestDelegate _next;

    public ValidationMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (IsWriteMethod(context.Request.Method))
        {
            var contentType = context.Request.ContentType;
            if (contentType is null || !contentType.Contains("application/json", StringComparison.OrdinalIgnoreCase))
            {
                context.Response.StatusCode = 415;
                context.Response.ContentType = "application/json";

                var correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id as string : "unknown";
                var error = new
                {
                    success = false,
                    error = "Content-Type must be application/json",
                    correlationId,
                    timestamp = DateTimeOffset.UtcNow
                };

                await context.Response.WriteAsync(JsonSerializer.Serialize(error));
                return;
            }
        }

        await _next(context);
    }

    private static bool IsWriteMethod(string method)
        => method is "POST" or "PUT" or "PATCH";
}
