namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyEvidenceRecord(
    string EvidenceId,
    string PolicyId,
    string ActorId,
    string Domain,
    string Operation,
    bool Allowed,
    string Reason,
    DateTime RecordedAt
);
