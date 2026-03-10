namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Whycespace.System.Upstream.WhycePolicy;

public sealed class PolicyMiddleware
{
    private readonly RequestDelegate _next;
    private readonly PolicyGovernor _governor;

    public PolicyMiddleware(RequestDelegate next, PolicyGovernor governor)
    {
        _next = next;
        _governor = governor;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        await _next(context);
    }
}
