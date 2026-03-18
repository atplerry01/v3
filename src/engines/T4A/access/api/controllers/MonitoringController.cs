namespace Whycespace.Engines.T4A.Access.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T4A.Access.Contracts.Responses;

[ApiController]
[Route("api/monitoring")]
public sealed class MonitoringController : ControllerBase
{
    private readonly IPlatformDispatcher _dispatcher;

    public MonitoringController(IPlatformDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("chain/health")]
    public async Task<IActionResult> GetChainHealth()
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("monitoring.chain.health",
            new Dictionary<string, object>());

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : StatusCode(503, ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Health check failed", correlationId));
    }

    [HttpGet("capital/validation/{operationId}")]
    public async Task<IActionResult> GetCapitalValidation(string operationId)
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("monitoring.capital.validation",
            new Dictionary<string, object> { ["operationId"] = operationId });

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : NotFound(ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Validation not found", correlationId));
    }

    [HttpGet("policy/anomalies")]
    public async Task<IActionResult> GetPolicyAnomalies()
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("monitoring.policy.anomalies",
            new Dictionary<string, object>());

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : BadRequest(ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Failed to retrieve anomalies", correlationId));
    }

    private string GetCorrelationId()
        => HttpContext.Items.TryGetValue("CorrelationId", out var id) && id is string s
            ? s : Guid.NewGuid().ToString();
}
