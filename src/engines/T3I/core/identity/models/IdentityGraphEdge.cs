namespace Whycespace.Engines.T3I.Core.Identity.Models;

public sealed record IdentityGraphEdge(
    Guid SourceNodeId,
    Guid TargetNodeId,
    string RelationshipType,
    double Strength,
    DateTime CreatedAt);
