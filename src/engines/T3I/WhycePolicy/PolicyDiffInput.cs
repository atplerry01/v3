namespace Whycespace.Engines.T3I.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyDiffInput(
    PolicyDefinition PreviousPolicy,
    PolicyDefinition ProposedPolicy
);
