namespace Whycespace.Platform.ControlPlane.Dev;

using Microsoft.AspNetCore.Mvc;
using Whycespace.CommandSystem.Core.Models;
using Whycespace.RuntimeDispatcher.Dispatcher;

[ApiController]
[Route("dev/identity")]
public sealed class IdentityAuditController : ControllerBase
{
    private readonly IRuntimeDispatcher _dispatcher;

    public IdentityAuditController(IRuntimeDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("audit/record")]
    public async Task<IActionResult> RecordIdentityAudit(
        [FromBody] Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        var command = new CommandEnvelope(
            Guid.NewGuid(),
            "IdentityAudit",
            payload,
            DateTimeOffset.UtcNow);

        var result = await _dispatcher.DispatchAsync(command, cancellationToken);

        return result.Success
            ? Ok(new { result.Success, result.Output })
            : BadRequest(new { result.Success, result.ErrorMessage });
    }
}
