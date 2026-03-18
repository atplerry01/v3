namespace Whycespace.Platform.ControlPlane.HEOS;

using Microsoft.AspNetCore.Mvc;
using Whycespace.CommandSystem.Core.Models;
using Whycespace.RuntimeDispatcher.Dispatcher;

[ApiController]
[Route("dev/heos")]
public sealed class HEOSAssignmentController : ControllerBase
{
    private readonly IRuntimeDispatcher _dispatcher;

    public HEOSAssignmentController(IRuntimeDispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    [HttpPost("assignment")]
    public async Task<IActionResult> DispatchAssignment(
        [FromBody] Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        var command = new CommandEnvelope(
            Guid.NewGuid(),
            "HEOSAssignment",
            payload,
            DateTimeOffset.UtcNow);

        var result = await _dispatcher.DispatchAsync(command, cancellationToken);

        return result.Success
            ? Ok(new { result.Success, result.Output })
            : BadRequest(new { result.Success, result.ErrorMessage });
    }

    [HttpPost("workforce/schedule")]
    public async Task<IActionResult> DispatchWorkforceSchedule(
        [FromBody] Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        var command = new CommandEnvelope(
            Guid.NewGuid(),
            "WorkforceScheduling",
            payload,
            DateTimeOffset.UtcNow);

        var result = await _dispatcher.DispatchAsync(command, cancellationToken);

        return result.Success
            ? Ok(new { result.Success, result.Output })
            : BadRequest(new { result.Success, result.ErrorMessage });
    }

    [HttpPost("workforce/performance")]
    public async Task<IActionResult> DispatchWorkforcePerformance(
        [FromBody] Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        var command = new CommandEnvelope(
            Guid.NewGuid(),
            "WorkforcePerformance",
            payload,
            DateTimeOffset.UtcNow);

        var result = await _dispatcher.DispatchAsync(command, cancellationToken);

        return result.Success
            ? Ok(new { result.Success, result.Output })
            : BadRequest(new { result.Success, result.ErrorMessage });
    }

    [HttpPost("workforce/incentive")]
    public async Task<IActionResult> DispatchWorkforceIncentive(
        [FromBody] Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        var command = new CommandEnvelope(
            Guid.NewGuid(),
            "WorkforceIncentive",
            payload,
            DateTimeOffset.UtcNow);

        var result = await _dispatcher.DispatchAsync(command, cancellationToken);

        return result.Success
            ? Ok(new { result.Success, result.Output })
            : BadRequest(new { result.Success, result.ErrorMessage });
    }

    [HttpPost("workforce/compliance")]
    public async Task<IActionResult> DispatchWorkforceCompliance(
        [FromBody] Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        var command = new CommandEnvelope(
            Guid.NewGuid(),
            "WorkforceCompliance",
            payload,
            DateTimeOffset.UtcNow);

        var result = await _dispatcher.DispatchAsync(command, cancellationToken);

        return result.Success
            ? Ok(new { result.Success, result.Output })
            : BadRequest(new { result.Success, result.ErrorMessage });
    }

    [HttpPost("workforce/lifecycle")]
    public async Task<IActionResult> DispatchWorkforceLifecycle(
        [FromBody] Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        var command = new CommandEnvelope(
            Guid.NewGuid(),
            "WorkforceLifecycle",
            payload,
            DateTimeOffset.UtcNow);

        var result = await _dispatcher.DispatchAsync(command, cancellationToken);

        return result.Success
            ? Ok(new { result.Success, result.Output })
            : BadRequest(new { result.Success, result.ErrorMessage });
    }

    [HttpPost("workforce/audit")]
    public async Task<IActionResult> DispatchWorkforceAudit(
        [FromBody] Dictionary<string, object> payload,
        CancellationToken cancellationToken)
    {
        var command = new CommandEnvelope(
            Guid.NewGuid(),
            "WorkforceAudit",
            payload,
            DateTimeOffset.UtcNow);

        var result = await _dispatcher.DispatchAsync(command, cancellationToken);

        return result.Success
            ? Ok(new { result.Success, result.Output })
            : BadRequest(new { result.Success, result.ErrorMessage });
    }
}
