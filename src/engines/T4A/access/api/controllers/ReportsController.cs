namespace Whycespace.Engines.T4A.Access.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Whycespace.Contracts.Runtime;
using Whycespace.Engines.T4A.Access.Contracts.Responses;

[ApiController]
[Route("api/reports")]
public sealed class ReportsController : ControllerBase
{
    private readonly IPlatformDispatcher _dispatcher;

    public ReportsController(IPlatformDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpGet("chain/audit/{chainId}")]
    public async Task<IActionResult> GetChainAudit(string chainId)
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("reports.chain.audit",
            new Dictionary<string, object> { ["chainId"] = chainId });

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : NotFound(ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Audit not found", correlationId));
    }

    [HttpGet("governance/audit")]
    public async Task<IActionResult> GetGovernanceAudit()
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("reports.governance.audit",
            new Dictionary<string, object>());

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : BadRequest(ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Audit retrieval failed", correlationId));
    }

    [HttpGet("capital/reconciliation/{vaultId}")]
    public async Task<IActionResult> GetCapitalReconciliation(string vaultId)
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("reports.capital.reconciliation",
            new Dictionary<string, object> { ["vaultId"] = vaultId });

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : NotFound(ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Reconciliation not found", correlationId));
    }

    [HttpGet("policy/audit/{policyId}")]
    public async Task<IActionResult> GetPolicyAudit(string policyId)
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("reports.policy.audit",
            new Dictionary<string, object> { ["policyId"] = policyId });

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : NotFound(ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Policy audit not found", correlationId));
    }

    [HttpGet("identity/audit/{identityId}")]
    public async Task<IActionResult> GetIdentityAudit(string identityId)
    {
        var correlationId = GetCorrelationId();
        var result = await _dispatcher.DispatchAsync("reports.identity.audit",
            new Dictionary<string, object> { ["identityId"] = identityId });

        return result.Success
            ? Ok(ApiResponse<IReadOnlyDictionary<string, object>>.Ok(result.Data, correlationId))
            : NotFound(ApiResponse<IReadOnlyDictionary<string, object>>.Fail(
                result.Error ?? "Identity audit not found", correlationId));
    }

    private string GetCorrelationId()
        => HttpContext.Items.TryGetValue("CorrelationId", out var id) && id is string s
            ? s : Guid.NewGuid().ToString();
}
