namespace Whycespace.EventReplay.Governance.Models;

public sealed record ReplayDecision(
    bool AllowReplay,
    bool Quarantine,
    string Reason
);
