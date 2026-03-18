namespace Whycespace.Engines.T4A.Access.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Whycespace.Engines.T4A.Access.Applications.Property;
using Whycespace.Engines.T4A.Access.Contracts.Requests;

[ApiController]
[Route("api/property")]
public sealed class PropertyController : ControllerBase
{
    private readonly PropertyApplicationService _propertyService;

    public PropertyController(PropertyApplicationService propertyService)
    {
        _propertyService = propertyService;
    }

    [HttpPost("list")]
    public async Task<IActionResult> List([FromBody] ListPropertyRequest request)
    {
        var correlationId = GetCorrelationId();
        var response = await _propertyService.ListAsync(request, correlationId);
        return response.Success ? Ok(response) : BadRequest(response);
    }

    private string GetCorrelationId()
        => HttpContext.Items.TryGetValue("CorrelationId", out var id) && id is string s
            ? s : Guid.NewGuid().ToString();
}
