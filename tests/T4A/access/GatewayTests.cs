using Whycespace.Engines.T4A.Access.Gateway;
using Microsoft.AspNetCore.Http;

namespace Whycespace.T4AAccess.Tests;

public sealed class GatewayTests
{
    [Theory]
    [InlineData("/api/capital/allocate", "economic")]
    [InlineData("/api/vault/create", "economic")]
    [InlineData("/api/property/list", "property")]
    [InlineData("/api/identity/register", "identity")]
    [InlineData("/api/workforce/assign", "workforce")]
    [InlineData("/api/analytics/vault/balance", "analytics")]
    [InlineData("/api/monitoring/chain/health", "monitoring")]
    [InlineData("/api/reports/governance/audit", "reports")]
    public void RequestRouter_ResolvesAreaCorrectly(string path, string expectedArea)
    {
        var router = new RequestRouter();
        var area = router.ResolveArea(new PathString(path));
        Assert.Equal(expectedArea, area);
    }

    [Fact]
    public void RequestRouter_ReturnsNullForUnknownPath()
    {
        var router = new RequestRouter();
        Assert.Null(router.ResolveArea(new PathString("/unknown/path")));
    }

    [Fact]
    public void AuthenticationHandler_FailsWithoutHeader()
    {
        var handler = new AuthenticationHandler();
        var context = new DefaultHttpContext();
        var result = handler.Authenticate(context);
        Assert.False(result.Success);
    }

    [Fact]
    public void AuthenticationHandler_SucceedsWithBearerToken()
    {
        var handler = new AuthenticationHandler();
        var context = new DefaultHttpContext();
        context.Request.Headers.Authorization = "Bearer test-token-123";
        var result = handler.Authenticate(context);

        Assert.True(result.Success);
        Assert.Equal("test-token-123", result.Token);
    }

    [Fact]
    public void AuthorizationHandler_AllowsValidRole()
    {
        var handler = new AuthorizationHandler();
        var result = handler.Authorize("economic", new[] { "admin" });
        Assert.True(result.Success);
    }

    [Fact]
    public void AuthorizationHandler_DeniesInvalidRole()
    {
        var handler = new AuthorizationHandler();
        var result = handler.Authorize("monitoring", new[] { "tenant" });
        Assert.False(result.Success);
    }

    [Fact]
    public void RateLimiter_AllowsWithinLimit()
    {
        var limiter = new RateLimiter(maxRequestsPerWindow: 5);
        var result = limiter.Check("client-1");
        Assert.True(result.IsAllowed);
        Assert.Equal(1, result.CurrentCount);
    }

    [Fact]
    public void RateLimiter_BlocksWhenLimitExceeded()
    {
        var limiter = new RateLimiter(maxRequestsPerWindow: 3);
        limiter.Check("client-1");
        limiter.Check("client-1");
        limiter.Check("client-1");
        var result = limiter.Check("client-1");

        Assert.False(result.IsAllowed);
        Assert.NotNull(result.RetryAfter);
    }
}
