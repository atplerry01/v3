namespace Whycespace.Systems.Upstream.Governance.Models;

public sealed record GovernanceRole(
    string RoleId,
    string Name,
    string Description,
    IReadOnlyList<string> Permissions);
