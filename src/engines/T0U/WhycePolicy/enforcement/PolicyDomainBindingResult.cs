namespace Whycespace.Engines.T0U.WhycePolicy.Enforcement;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyDomainBindingResult(
    string PolicyId,
    PolicyDomainType DomainType,
    string DomainIdentifier,
    bool BindingSuccessful,
    DateTime BoundAt,
    string BindingReason
);
