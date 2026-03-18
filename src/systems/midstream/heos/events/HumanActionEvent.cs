namespace Whycespace.Systems.Midstream.HEOS.Events;

public sealed record HumanActionEvent(
    Guid EventId,
    string ActionType,
    string ActorId,
    string ClusterId,
    IReadOnlyDictionary<string, object> Payload,
    DateTimeOffset Timestamp
)
{
    public static HumanActionEvent Create(string actionType, string actorId, string clusterId, IReadOnlyDictionary<string, object>? payload = null)
        => new(Guid.NewGuid(), actionType, actorId, clusterId, payload ?? new Dictionary<string, object>(), DateTimeOffset.UtcNow);
}
