namespace Whycespace.Engines.T1M.WSS.Governance.Commands;

public sealed record StartGovernanceWorkflowCommand(
    Guid CommandId,
    Guid ProposalId,
    Guid StartedByGuardianId,
    DateTime Timestamp);
