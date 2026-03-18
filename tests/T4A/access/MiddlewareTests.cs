using Microsoft.AspNetCore.Http;
using Whycespace.Engines.T4A.Access.Middleware;

namespace Whycespace.T4AAccess.Tests;

public sealed class MiddlewareTests
{
    [Fact]
    public async Task CorrelationIdMiddleware_GeneratesIdWhenMissing()
    {
        var context = new DefaultHttpContext();
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.True(context.Items.ContainsKey("CorrelationId"));
        Assert.NotNull(context.Items["CorrelationId"]);
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-Id"));
    }

    [Fact]
    public async Task CorrelationIdMiddleware_PreservesExistingId()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers["X-Correlation-Id"] = "existing-id";
        var middleware = new CorrelationIdMiddleware(_ => Task.CompletedTask);

        await middleware.InvokeAsync(context);

        Assert.Equal("existing-id", context.Items["CorrelationId"]);
        Assert.Equal("existing-id", context.Response.Headers["X-Correlation-Id"].ToString());
    }

    [Fact]
    public async Task ValidationMiddleware_RejectsNonJsonPost()
    {
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "text/plain";
        context.Response.Body = new MemoryStream();

        var middleware = new ValidationMiddleware(_ => Task.CompletedTask);
        await middleware.InvokeAsync(context);

        Assert.Equal(415, context.Response.StatusCode);
    }

    [Fact]
    public async Task ValidationMiddleware_AllowsJsonPost()
    {
        var invoked = false;
        var context = new DefaultHttpContext();
        context.Request.Method = "POST";
        context.Request.ContentType = "application/json";

        var middleware = new ValidationMiddleware(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
    }

    [Fact]
    public async Task ValidationMiddleware_AllowsGetWithoutContentType()
    {
        var invoked = false;
        var context = new DefaultHttpContext();
        context.Request.Method = "GET";

        var middleware = new ValidationMiddleware(_ =>
        {
            invoked = true;
            return Task.CompletedTask;
        });

        await middleware.InvokeAsync(context);

        Assert.True(invoked);
    }
}
