namespace Whycespace.System.Upstream.WhycePolicy.Registry;

using Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyVersionRecord(
    string PolicyId,
    int Version,
    PolicyDefinition PolicyDefinition,
    DateTime CreatedAt,
    string CreatedBy
);
