namespace Whycespace.System.Upstream.WhycePolicy.Models;

public enum PolicyStatus { Active = 0, Inactive = 1, Superseded = 2 }

public sealed record PolicyRecord(
    string PolicyId,
    int Version,
    PolicyDefinition PolicyDefinition,
    PolicyStatus Status,
    DateTime RegisteredAt
);
