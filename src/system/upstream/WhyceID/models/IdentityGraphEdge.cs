namespace Whycespace.System.WhyceID.Models;

public sealed record IdentityGraphEdge(
    Guid EdgeId,
    Guid SourceIdentityId,
    Guid TargetEntityId,
    string Relationship,
    DateTime CreatedAt
);
