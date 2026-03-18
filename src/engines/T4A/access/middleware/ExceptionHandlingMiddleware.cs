namespace Whycespace.Engines.T4A.Access.Middleware;

using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            var correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id as string : "unknown";
            _logger.LogError(ex, "[{CorrelationId}] Unhandled exception on {Method} {Path}",
                correlationId, context.Request.Method, context.Request.Path);

            context.Response.StatusCode = 500;
            context.Response.ContentType = "application/json";

            var error = new
            {
                success = false,
                error = "An internal error occurred",
                correlationId,
                timestamp = DateTimeOffset.UtcNow
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(error));
        }
    }
}
