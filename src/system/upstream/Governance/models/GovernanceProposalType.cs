namespace Whycespace.System.Upstream.Governance.Models;

public sealed record GovernanceProposalType(
    string TypeId,
    string Name,
    string Description,
    bool IsActive = true);

public enum GovernanceProposalTypeAction
{
    Registered,
    Deactivated,
    Validated
}
