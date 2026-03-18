namespace Whycespace.Systems.Midstream.Coordination;

public sealed class WorkflowToExecutionBridge
{
    private readonly List<BridgeEvent> _bridgeLog = new();

    public Task BridgeToExecutionAsync(string workflowId, IReadOnlyDictionary<string, object> context)
    {
        var bridgeEvent = new BridgeEvent(
            Guid.NewGuid().ToString(),
            "WorkflowToExecution",
            workflowId,
            context,
            DateTimeOffset.UtcNow);

        _bridgeLog.Add(bridgeEvent);
        return Task.CompletedTask;
    }

    public IReadOnlyList<BridgeEvent> GetBridgeLog() => _bridgeLog;
}

public sealed record BridgeEvent(
    string BridgeEventId,
    string BridgeType,
    string SourceId,
    IReadOnlyDictionary<string, object> Context,
    DateTimeOffset Timestamp
);
