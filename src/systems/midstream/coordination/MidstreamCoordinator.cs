namespace Whycespace.Systems.Midstream.Coordination;

public sealed class MidstreamCoordinator
{
    private readonly WorkflowToExecutionBridge _workflowBridge;
    private readonly IntelligenceToPlanningBridge _intelligenceBridge;
    private readonly SystemRoutingManager _routingManager;

    public MidstreamCoordinator(
        WorkflowToExecutionBridge workflowBridge,
        IntelligenceToPlanningBridge intelligenceBridge,
        SystemRoutingManager routingManager)
    {
        _workflowBridge = workflowBridge;
        _intelligenceBridge = intelligenceBridge;
        _routingManager = routingManager;
    }

    public async Task CoordinateWorkflowExecutionAsync(string workflowId, IReadOnlyDictionary<string, object> context)
    {
        await _workflowBridge.BridgeToExecutionAsync(workflowId, context);
    }

    public async Task CoordinateIntelligencePlanningAsync(string insightId, string clusterId)
    {
        await _intelligenceBridge.BridgeToPlanningAsync(insightId, clusterId);
    }

    public void RegisterRoute(string systemName, string targetEndpoint)
    {
        _routingManager.RegisterRoute(systemName, targetEndpoint);
    }
}
