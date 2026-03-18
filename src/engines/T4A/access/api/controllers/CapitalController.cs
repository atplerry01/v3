namespace Whycespace.Engines.T4A.Access.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Whycespace.Engines.T4A.Access.Applications.Capital;
using Whycespace.Engines.T4A.Access.Contracts.Requests;

[ApiController]
[Route("api/capital")]
public sealed class CapitalController : ControllerBase
{
    private readonly CapitalApplicationService _capitalService;

    public CapitalController(CapitalApplicationService capitalService)
    {
        _capitalService = capitalService;
    }

    [HttpPost("allocate")]
    public async Task<IActionResult> Allocate([FromBody] AllocateCapitalRequest request)
    {
        var correlationId = GetCorrelationId();
        var response = await _capitalService.AllocateAsync(request, correlationId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpPost("contribute")]
    public async Task<IActionResult> Contribute([FromBody] ContributeCapitalRequest request)
    {
        var correlationId = GetCorrelationId();
        var response = await _capitalService.ContributeAsync(request, correlationId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private string GetCorrelationId()
        => HttpContext.Items.TryGetValue("CorrelationId", out var id) && id is string s
            ? s : Guid.NewGuid().ToString();
}
