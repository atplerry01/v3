namespace Whycespace.Engines.T4A.Access.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Whycespace.Engines.T4A.Access.Applications.Identity;
using Whycespace.Engines.T4A.Access.Contracts.Requests;

[ApiController]
[Route("api/identity")]
public sealed class IdentityController : ControllerBase
{
    private readonly IdentityApplicationService _identityService;

    public IdentityController(IdentityApplicationService identityService)
    {
        _identityService = identityService;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterIdentityRequest request)
    {
        var correlationId = GetCorrelationId();
        var response = await _identityService.RegisterAsync(request, correlationId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("policy/evaluate")]
    public async Task<IActionResult> EvaluatePolicy([FromBody] EvaluatePolicyRequest request)
    {
        var correlationId = GetCorrelationId();
        var response = await _identityService.EvaluatePolicyAsync(request, correlationId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private string GetCorrelationId()
        => HttpContext.Items.TryGetValue("CorrelationId", out var id) && id is string s
            ? s : Guid.NewGuid().ToString();
}
