namespace Whycespace.Engines.T0U.Governance.Results;

using Whycespace.System.Upstream.Governance.Models;

public sealed record GovernanceDisputeResult(
    bool Success,
    Guid DisputeId,
    Guid ProposalId,
    DisputeType DisputeType,
    DisputeStatus DisputeStatus,
    string Message,
    DateTime ExecutedAt);
