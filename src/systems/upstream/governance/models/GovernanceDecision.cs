namespace Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceDecision(
    string ProposalId,
    DecisionOutcome Outcome,
    int Approve,
    int Reject,
    int Abstain,
    bool QuorumMet);

public enum DecisionOutcome
{
    Approved,
    Rejected,
    NoQuorum,
    Escalated
}

public enum DecisionRule
{
    SimpleMajority,
    SuperMajority,
    ConstitutionalMajority,
    EmergencyOverride
}
