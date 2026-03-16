namespace Whycespace.Platform.ControlPlane.OperatorConsole;

using Microsoft.AspNetCore.Mvc;
using Whycespace.EngineRuntime.Registry;
using Whycespace.Runtime.Observability;
using Whycespace.Runtime.Reliability;
using Whycespace.Systems.Downstream.Clusters;

[ApiController]
[Route("api/operator")]
public sealed class OperatorController : ControllerBase
{
    private readonly EngineRegistry _engineRegistry;
    private readonly RuntimeObserver _observer;
    private readonly DeadLetterQueue _dlq;
    private readonly ClusterRegistry _clusterRegistry;

    public OperatorController(
        EngineRegistry engineRegistry,
        RuntimeObserver observer,
        DeadLetterQueue dlq,
        ClusterRegistry clusterRegistry)
    {
        _engineRegistry = engineRegistry;
        _observer = observer;
        _dlq = dlq;
        _clusterRegistry = clusterRegistry;
    }

    [HttpGet("engines")]
    public IActionResult GetEngines() => Ok(_engineRegistry.ListEngines());

    [HttpGet("invocations")]
    public IActionResult GetInvocations() => Ok(_observer.GetLogs());

    [HttpGet("deadletters")]
    public IActionResult GetDeadLetters() => Ok(_dlq.GetEntries());

    [HttpGet("clusters")]
    public IActionResult GetClusters() => Ok(_clusterRegistry.GetAllClusters());
}
