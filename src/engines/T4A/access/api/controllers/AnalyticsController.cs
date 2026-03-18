namespace Whycespace.Engines.T4A.Access.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T4A.Access.Contracts.Responses;

[ApiController]
[Route("api/analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly IPlatformDispatcher _dispatcher;

    public AnalyticsController(IPlatformDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("vault/{vaultId}/balance")]
    public async Task<IActionResult> GetVaultBalance(string vaultId)
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("analytics.vault.balance",
            new Dictionary<string, object> { ["vaultId"] = vaultId });

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : NotFound(ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Vault not found", correlationId));
    }

    [HttpGet("vault/{vaultId}/cashflow")]
    public async Task<IActionResult> GetVaultCashflow(string vaultId)
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("analytics.vault.cashflow",
            new Dictionary<string, object> { ["vaultId"] = vaultId });

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : NotFound(ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Cashflow data not found", correlationId));
    }

    [HttpGet("vault/{vaultId}/profit")]
    public async Task<IActionResult> GetVaultProfit(string vaultId)
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("analytics.vault.profit",
            new Dictionary<string, object> { ["vaultId"] = vaultId });

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : NotFound(ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Profit data not found", correlationId));
    }

    private string GetCorrelationId()
        => HttpContext.Items.TryGetValue("CorrelationId", out var id) && id is string s
            ? s : Guid.NewGuid().ToString();
}
