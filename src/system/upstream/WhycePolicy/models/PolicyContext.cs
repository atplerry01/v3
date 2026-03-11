namespace Whycespace.System.Upstream.WhycePolicy.Models;

public sealed record PolicyContext(
    Guid ContextId,
    Guid ActorId,
    string TargetDomain,
    IReadOnlyDictionary<string, string> Attributes,
    DateTime Timestamp
);
