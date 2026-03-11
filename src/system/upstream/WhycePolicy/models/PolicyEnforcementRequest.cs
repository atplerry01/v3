namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyEnforcementRequest(
    string ActorId,
    string Domain,
    string Operation,
    IReadOnlyDictionary<string, string> Attributes
);
