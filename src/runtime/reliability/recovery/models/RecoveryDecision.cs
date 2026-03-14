namespace Whycespace.Reliability.Recovery.Models;

public sealed record RecoveryDecision(
    bool AllowReplay,
    bool Quarantine,
    string Reason
);
