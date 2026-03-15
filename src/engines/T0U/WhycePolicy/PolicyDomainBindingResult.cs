namespace Whycespace.Engines.T0U.WhycePolicy;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyDomainBindingResult(
    string PolicyId,
    PolicyDomainType DomainType,
    string DomainIdentifier,
    bool BindingSuccessful,
    DateTime BoundAt,
    string BindingReason
);
