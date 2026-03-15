namespace Whycespace.Engines.T1M.WSS.Governance.Results;

public sealed record GovernanceWorkflowResult(
    bool Success,
    Guid ProposalId,
    string CurrentStep,
    string NextStep,
    string Message,
    DateTime ExecutedAt);
