namespace Whycespace.Engines.T3I.WhycePolicy;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyDiffInput(
    PolicyDefinition PreviousPolicy,
    PolicyDefinition ProposedPolicy
);
