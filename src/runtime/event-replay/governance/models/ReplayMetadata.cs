namespace Whycespace.EventReplay.Governance.Models;

public sealed record ReplayMetadata(
    Guid EventId,
    int ReplayCount,
    DateTime FirstReplay,
    DateTime LastReplay
);
