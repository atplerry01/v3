namespace Whycespace.Platform.Gateway.WhyceApiGateway;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Diagnostics.HealthChecks;

[ApiController]
[Route("health")]
public sealed class HealthController : ControllerBase
{
    private readonly HealthCheckService _healthCheckService;

    public HealthController(HealthCheckService healthCheckService)
    {
        _healthCheckService = healthCheckService;
    }

    [HttpGet("kafka")]
    public async Task<IActionResult> Kafka()
    {
        var result = await _healthCheckService.CheckHealthAsync(
            r => r.Tags.Contains("kafka") || r.Name == "kafka");
        return result.Status == HealthStatus.Healthy ? Ok("Healthy") : StatusCode(503, "Unhealthy");
    }

    [HttpGet("postgres")]
    public async Task<IActionResult> Postgres()
    {
        var result = await _healthCheckService.CheckHealthAsync(
            r => r.Tags.Contains("postgres") || r.Name == "postgres");
        return result.Status == HealthStatus.Healthy ? Ok("Healthy") : StatusCode(503, "Unhealthy");
    }

    [HttpGet("redis")]
    public async Task<IActionResult> Redis()
    {
        var result = await _healthCheckService.CheckHealthAsync(
            r => r.Tags.Contains("redis") || r.Name == "redis");
        return result.Status == HealthStatus.Healthy ? Ok("Healthy") : StatusCode(503, "Unhealthy");
    }
}
