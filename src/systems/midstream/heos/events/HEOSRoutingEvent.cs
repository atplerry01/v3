namespace Whycespace.Systems.Midstream.HEOS.Events;

public sealed record HEOSRoutingEvent(
    Guid EventId,
    string SignalType,
    string SourceClusterId,
    string TargetClusterId,
    string RoutingDecision,
    DateTimeOffset Timestamp
)
{
    public static HEOSRoutingEvent Create(string signalType, string sourceClusterId, string targetClusterId, string routingDecision)
        => new(Guid.NewGuid(), signalType, sourceClusterId, targetClusterId, routingDecision, DateTimeOffset.UtcNow);
}
