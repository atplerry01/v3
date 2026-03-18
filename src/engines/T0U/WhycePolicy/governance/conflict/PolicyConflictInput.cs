namespace Whycespace.Engines.T0U.WhycePolicy.Governance.Conflict;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyConflictInput(
    IReadOnlyList<PolicyDefinition> Policies
);
