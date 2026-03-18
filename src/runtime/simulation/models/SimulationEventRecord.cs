namespace Whycespace.SimulationRuntime.Models;

public sealed record SimulationEventRecord(
    Guid EventId,
    string EventType,
    IReadOnlyDictionary<string, object> Payload,
    DateTimeOffset Timestamp,
    string WouldPublishToTopic
);
