namespace Whycespace.Engines.T1M.WSS.Governance.Commands;

public sealed record AdvanceGovernanceWorkflowCommand(
    Guid CommandId,
    Guid ProposalId,
    string CurrentStep,
    string NextStep,
    Guid TriggeredBy,
    DateTime Timestamp);
