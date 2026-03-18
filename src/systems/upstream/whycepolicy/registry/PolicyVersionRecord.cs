namespace Whycespace.Systems.Upstream.WhycePolicy.Registry;

using Whycespace.Systems.Upstream.WhycePolicy.Models;

public sealed record PolicyVersionRecord(
    string PolicyId,
    int Version,
    PolicyDefinition PolicyDefinition,
    DateTime CreatedAt,
    string CreatedBy
);
