namespace Whycespace.Engines.T0U.Governance.Dispute.Raising;

using Whycespace.Systems.Upstream.Governance.Models;

public sealed record RaiseGovernanceDisputeCommand(
    Guid CommandId,
    Guid ProposalId,
    DisputeType DisputeType,
    Guid RaisedByGuardianId,
    string DisputeReason,
    DateTime Timestamp);
