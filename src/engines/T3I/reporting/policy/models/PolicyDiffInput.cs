namespace Whycespace.Engines.T3I.Reporting.Policy.Models;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyDiffInput(
    PolicyDefinition PreviousPolicy,
    PolicyDefinition ProposedPolicy
);
