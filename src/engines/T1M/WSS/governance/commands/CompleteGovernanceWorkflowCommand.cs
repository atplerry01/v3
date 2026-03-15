namespace Whycespace.Engines.T1M.WSS.Governance.Commands;

public sealed record CompleteGovernanceWorkflowCommand(
    Guid CommandId,
    Guid ProposalId,
    Guid CompletedBy,
    DateTime Timestamp);
