namespace Whycespace.Systems.Midstream.HEOS.Orchestration;

public sealed record HEOSContext(
    string SessionId,
    string TargetClusterId,
    string InitiatorId,
    IReadOnlyDictionary<string, object> Metadata
)
{
    public static HEOSContext Create(string targetClusterId, string initiatorId)
        => new(Guid.NewGuid().ToString(), targetClusterId, initiatorId, new Dictionary<string, object>());
}
