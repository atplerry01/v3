using Whycespace.Systems.Downstream.Coordination.Evidence;
using Whycespace.Systems.Downstream.Coordination.Trace;
using Whycespace.Systems.Downstream.Coordination.Simulation;

namespace Whycespace.Systems.Downstream.Coordination;

public sealed class DownstreamCoordinator : ISimulationAware
{
    private readonly ClusterExecutionRouter _clusterRouter;
    private readonly ExecutionToSpvBridge _spvBridge;
    private readonly WorkToCapitalFlowMapper _capitalMapper;
    private readonly EvidenceRecorder _evidenceRecorder;
    private readonly TraceCorrelationService _traceService;

    public DownstreamCoordinator(
        ClusterExecutionRouter clusterRouter,
        ExecutionToSpvBridge spvBridge,
        WorkToCapitalFlowMapper capitalMapper,
        EvidenceRecorder evidenceRecorder,
        TraceCorrelationService traceService)
    {
        _clusterRouter = clusterRouter;
        _spvBridge = spvBridge;
        _capitalMapper = capitalMapper;
        _evidenceRecorder = evidenceRecorder;
        _traceService = traceService;
    }

    public DownstreamExecutionResult Execute(DownstreamExecutionRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        var trace = _traceService.StartTrace("DownstreamExecution");

        var clusterStep = trace.BeginStep("ClusterResolution");
        var clusterResolved = _clusterRouter.ResolveCluster(request.ClusterId, request.SubClusterId);
        if (!clusterResolved)
        {
            clusterStep.Fail("Cluster resolution failed");
            _traceService.CompleteTrace(trace.TraceId, false);
            _evidenceRecorder.Record("DownstreamExecution", request.InitiatorIdentityId.ToString(), request.ClusterId, "Execute", "Failed:ClusterResolution", trace.TraceId);
            return DownstreamExecutionResult.Failed("Cluster resolution failed.");
        }
        clusterStep.Complete();

        var spvStep = trace.BeginStep("SpvMapping");
        var spvMapped = _spvBridge.MapToSpv(request.ClusterId, request.ExecutionType);
        if (spvMapped is null)
        {
            spvStep.Fail("SPV mapping failed");
            _traceService.CompleteTrace(trace.TraceId, false);
            _evidenceRecorder.Record("DownstreamExecution", request.InitiatorIdentityId.ToString(), request.ClusterId, "Execute", "Failed:SpvMapping", trace.TraceId);
            return DownstreamExecutionResult.Failed("SPV mapping failed.");
        }
        spvStep.Complete();

        var flowStep = trace.BeginStep("CapitalFlowRecording");
        _capitalMapper.RecordFlow(request.ClusterId, spvMapped.Value, request.Amount);
        flowStep.Complete();

        _traceService.CompleteTrace(trace.TraceId, true);
        _evidenceRecorder.Record("DownstreamExecution", request.InitiatorIdentityId.ToString(), request.ClusterId, "Execute", "Success", trace.TraceId);

        return DownstreamExecutionResult.Success(request.ClusterId, spvMapped.Value);
    }

    public Task<SimulationResult> SimulateAsync(SimulationContext context)
    {
        var steps = new List<string> { "ClusterResolution", "SpvMapping", "CapitalFlowRecording" };
        var policies = new List<string> { "ClusterActivation", "SpvAccess" };
        var events = new List<string> { "DownstreamExecutionCompletedEvent" };

        var result = SimulationResult.Success("DownstreamExecution", steps, policies, events);
        return Task.FromResult(result);
    }
}

public sealed record DownstreamExecutionRequest(
    string ClusterId,
    string SubClusterId,
    string ExecutionType,
    decimal Amount,
    Guid InitiatorIdentityId,
    DateTimeOffset Timestamp
);

public sealed record DownstreamExecutionResult(
    bool IsSuccess,
    string? ClusterId,
    Guid? SpvId,
    string? ErrorMessage
)
{
    public static DownstreamExecutionResult Success(string clusterId, Guid spvId) =>
        new(true, clusterId, spvId, null);

    public static DownstreamExecutionResult Failed(string error) =>
        new(false, null, null, error);
}
