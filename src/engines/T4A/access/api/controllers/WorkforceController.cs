namespace Whycespace.Engines.T4A.Access.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Whycespace.Engines.T4A.Access.Applications.Workforce;

[ApiController]
[Route("api/workforce")]
public sealed class WorkforceController : ControllerBase
{
    private readonly WorkforceApplicationService _workforceService;

    public WorkforceController(WorkforceApplicationService workforceService)
    {
        _workforceService = workforceService;
    }

    [HttpPost("assign")]
    public async Task<IActionResult> Assign([FromBody] WorkforceAssignRequest request)
    {
        var correlationId = GetCorrelationId();
        var response = await _workforceService.AssignAsync(request.WorkerId, request.TaskId, correlationId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    [HttpGet("compliance/{workerId}")]
    public async Task<IActionResult> CheckCompliance(string workerId)
    {
        var correlationId = GetCorrelationId();
        var response = await _workforceService.CheckComplianceAsync(workerId, correlationId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private string GetCorrelationId()
        => HttpContext.Items.TryGetValue("CorrelationId", out var id) && id is string s
            ? s : Guid.NewGuid().ToString();
}

public sealed record WorkforceAssignRequest(string WorkerId, string TaskId);
