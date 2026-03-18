namespace Whycespace.Engines.T4A.Access.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Whycespace.Engines.T4A.Access.Applications.Vault;
using Whycespace.Engines.T4A.Access.Contracts.Requests;

[ApiController]
[Route("api/vault")]
public sealed class VaultController : ControllerBase
{
    private readonly VaultApplicationService _vaultService;

    public VaultController(VaultApplicationService vaultService)
    {
        _vaultService = vaultService;
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateVaultRequest request)
    {
        var correlationId = GetCorrelationId();
        var response = await _vaultService.CreateAsync(request, correlationId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("transfer")]
    public async Task<IActionResult> Transfer([FromBody] TransferVaultRequest request)
    {
        var correlationId = GetCorrelationId();
        var response = await _vaultService.TransferAsync(request, correlationId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private string GetCorrelationId()
        => HttpContext.Items.TryGetValue("CorrelationId", out var id) && id is string s
            ? s : Guid.NewGuid().ToString();
}
