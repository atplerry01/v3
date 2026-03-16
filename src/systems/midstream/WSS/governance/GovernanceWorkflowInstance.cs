namespace Whycespace.Systems.Midstream.WSS.Governance;

public sealed record GovernanceWorkflowInstance(
    Guid InstanceId,
    Guid ProposalId,
    GovernanceWorkflowStep CurrentStep,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt);
