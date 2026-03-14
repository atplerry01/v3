namespace Whycespace.EventReplay.Governance.Models;

public sealed record ReplayRequest(
    Guid EventId,
    string SourceTopic,
    string Payload,
    int ReplayCount
);
