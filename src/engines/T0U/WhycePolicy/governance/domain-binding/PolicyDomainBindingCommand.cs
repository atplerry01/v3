namespace Whycespace.Engines.T0U.WhycePolicy.Governance.DomainBinding;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyDomainBindingCommand(
    string PolicyId,
    PolicyDomainType DomainType,
    string DomainIdentifier,
    string BoundBy,
    string Reason
);
