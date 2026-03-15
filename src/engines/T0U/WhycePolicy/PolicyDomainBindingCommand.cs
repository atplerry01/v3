namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyDomainBindingCommand(
    string PolicyId,
    PolicyDomainType DomainType,
    string DomainIdentifier,
    string BoundBy,
    string Reason
);
