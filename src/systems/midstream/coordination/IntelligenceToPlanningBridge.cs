namespace Whycespace.Systems.Midstream.Coordination;

public sealed class IntelligenceToPlanningBridge
{
    private readonly List<BridgeEvent> _bridgeLog = new();

    public Task BridgeToPlanningAsync(string insightId, string clusterId)
    {
        var context = new Dictionary<string, object>
        {
            ["insightId"] = insightId,
            ["clusterId"] = clusterId
        };

        var bridgeEvent = new BridgeEvent(
            Guid.NewGuid().ToString(),
            "IntelligenceToPlanning",
            insightId,
            context,
            DateTimeOffset.UtcNow);

        _bridgeLog.Add(bridgeEvent);
        return Task.CompletedTask;
    }

    public IReadOnlyList<BridgeEvent> GetBridgeLog() => _bridgeLog;
}
