namespace Whycespace.Engines.T4A.Access.Middleware;

using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var correlationId = context.Items.TryGetValue("CorrelationId", out var id) ? id as string : "unknown";
        var method = context.Request.Method;
        var path = context.Request.Path;

        _logger.LogInformation("[{CorrelationId}] {Method} {Path} — started", correlationId, method, path);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            _logger.LogInformation(
                "[{CorrelationId}] {Method} {Path} — {StatusCode} in {ElapsedMs}ms",
                correlationId, method, path, context.Response.StatusCode, stopwatch.ElapsedMilliseconds);
        }
    }
}
