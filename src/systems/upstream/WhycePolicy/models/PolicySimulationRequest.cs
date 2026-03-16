namespace Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicySimulationRequest(
    string Domain,
    string ActorId,
    IReadOnlyDictionary<string, string> Attributes
);
